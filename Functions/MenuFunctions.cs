using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
namespace CoffeeNChill;
public sealed class MenuFunctions(Storage storage)
{
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public record MenuInput(string Category, string Id, string Name, string Description, double Price, bool IsAvailable);
    public record MenuUpdate(double? Price, bool? IsAvailable);
    static object View(TableEntity e) => new { Category=e.PartitionKey, Id=e.RowKey, Name=e.GetString("Name"), Description=e.GetString("Description"), Price=e.GetDouble("Price"), IsAvailable=e.GetBoolean("IsAvailable") };
    [Function("CreateMenuItem")]
    public async Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous,"post",Route="menu")] HttpRequest req)
    {
        MenuInput? m; try { m=await JsonSerializer.DeserializeAsync<MenuInput>(req.Body,Json); } catch(JsonException) { return new BadRequestObjectResult("Invalid JSON."); }
        if(m is null || !Storage.ValidKey(m.Category) || !Storage.ValidKey(m.Id) || string.IsNullOrWhiteSpace(m.Name) || !double.IsFinite(m.Price) || m.Price<0) return new BadRequestObjectResult("Provide Category, Id, Name and a nonnegative Price.");
        var table=storage.Menu; await table.CreateIfNotExistsAsync();
        var e=new TableEntity(m.Category,m.Id) { ["Name"]=m.Name,["Description"]=m.Description??"",["Price"]=m.Price,["IsAvailable"]=m.IsAvailable };
        try { await table.AddEntityAsync(e); } catch(RequestFailedException ex) when(ex.Status==409) { return new ConflictObjectResult("Item already exists."); }
        return new CreatedResult($"/api/menu/{Uri.EscapeDataString(m.Category)}/{Uri.EscapeDataString(m.Id)}", View(e));
    }
    [Function("GetAllMenuItems")]
    public Task<IActionResult> All([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="menu")] HttpRequest req) => List(null);
    [Function("GetMenuItemsByCategory")]
    public Task<IActionResult> Category([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="menu/category/{category}")] HttpRequest req,string category) => List(category);
    async Task<IActionResult> List(string? category)
    {
        var t=storage.Menu;await t.CreateIfNotExistsAsync();var items=new List<object>();
        await foreach(var e in t.QueryAsync<TableEntity>(filter:category is null ? null : TableClient.CreateQueryFilter($"PartitionKey eq {category}"))) items.Add(View(e));
        return new OkObjectResult(items);
    }
    [Function("UpdateMenuItem")]
    public async Task<IActionResult> Update([HttpTrigger(AuthorizationLevel.Anonymous,"put",Route="menu/{category}/{id}")] HttpRequest req,string category,string id)
    {
        if(!Storage.ValidKey(category)||!Storage.ValidKey(id)) return new BadRequestObjectResult("Invalid keys.");
        MenuUpdate? m;try {m=await JsonSerializer.DeserializeAsync<MenuUpdate>(req.Body,Json);}catch(JsonException){return new BadRequestObjectResult("Invalid JSON.");}
        if(m is null || (m.Price is null && m.IsAvailable is null) || (m.Price.HasValue && (!double.IsFinite(m.Price.Value)||m.Price.Value<0))) return new BadRequestObjectResult("Provide Price and/or IsAvailable.");
        var t=storage.Menu;await t.CreateIfNotExistsAsync();var existing=await t.GetEntityIfExistsAsync<TableEntity>(category,id);
        if(!existing.HasValue)return new NotFoundResult();var e=existing.Value;
        if(m.Price.HasValue)e["Price"]=m.Price.Value;if(m.IsAvailable.HasValue)e["IsAvailable"]=m.IsAvailable.Value;
        try{await t.UpdateEntityAsync(e,e.ETag,TableUpdateMode.Replace);}catch(RequestFailedException ex) when(ex.Status==412){return new ConflictObjectResult("Item changed; retry.");}
        return new OkObjectResult(View(e));
    }
    [Function("DeleteMenuItem")]
    public async Task<IActionResult> Delete([HttpTrigger(AuthorizationLevel.Anonymous,"delete",Route="menu/{category}/{id}")] HttpRequest req,string category,string id)
    {
        if(!Storage.ValidKey(category)||!Storage.ValidKey(id))return new BadRequestResult();
        var t=storage.Menu;await t.CreateIfNotExistsAsync();var e=await t.GetEntityIfExistsAsync<TableEntity>(category,id);if(!e.HasValue)return new NotFoundResult();
        try{await t.DeleteEntityAsync(category,id,e.Value.ETag);}catch(RequestFailedException ex) when(ex.Status==412){return new ConflictResult();}
        return new NoContentResult();
    }
}
