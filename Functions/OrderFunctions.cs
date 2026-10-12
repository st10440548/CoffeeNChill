using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace CoffeeNChill;
public sealed record OrderInput(string OrderId,string CustomerName,string[] SelectedItemSKUs,double TotalPrice,DateTimeOffset OrderTimestamp);
public sealed class OrderFunctions(Storage storage,ILogger<OrderFunctions> logger)
{
    static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true};
    static bool Valid(OrderInput? o) => o is not null && Storage.ValidKey(o.OrderId) && !string.IsNullOrWhiteSpace(o.CustomerName) && o.SelectedItemSKUs is {Length:>0} && o.SelectedItemSKUs.All(Storage.ValidKey) && double.IsFinite(o.TotalPrice) && o.TotalPrice>=0 && o.OrderTimestamp!=default;
    static string Date(OrderInput o)=>o.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd");
    [Function("QueueOrder")]
    public async Task<IActionResult> Queue([HttpTrigger(AuthorizationLevel.Anonymous,"post",Route="orders/queue")] HttpRequest req)
    {
        OrderInput? o;try{o=await JsonSerializer.DeserializeAsync<OrderInput>(req.Body,Json);}catch(JsonException){return new BadRequestObjectResult("Invalid JSON.");}
        if(!Valid(o))return new BadRequestObjectResult("Provide valid OrderId, CustomerName, SelectedItemSKUs, TotalPrice and OrderTimestamp.");
        var q=storage.Queue;await q.CreateIfNotExistsAsync();await q.SendMessageAsync(JsonSerializer.Serialize(o));
        return new AcceptedResult($"/api/orders/{Date(o!)}/{Uri.EscapeDataString(o!.OrderId)}",new{ o.OrderId,OrderDate=Date(o),Status="Queued" });
    }
    [Function("ProcessOrderQueue")]
    public async Task Process([QueueTrigger("order-processing-queue",Connection="AzureWebJobsStorage")] string message,CancellationToken cancellationToken)
    {
        var o=JsonSerializer.Deserialize<OrderInput>(message,Json);if(!Valid(o))throw new InvalidOperationException("Invalid order message; retries then poison queue.");
        var t=storage.Orders;await t.CreateIfNotExistsAsync(cancellationToken);var date=Date(o!);
        var e=new TableEntity(date,o!.OrderId){["CustomerName"]=o.CustomerName,["SelectedItemSKUs"]=JsonSerializer.Serialize(o.SelectedItemSKUs),["TotalPrice"]=o.TotalPrice,["OrderTimestamp"]=o.OrderTimestamp.ToUniversalTime(),["Status"]="Received",["ReceivedAt"]=DateTimeOffset.UtcNow};
        try{await t.AddEntityAsync(e,cancellationToken);}catch(RequestFailedException ex) when(ex.Status==409)
        {
            e=(await t.GetEntityAsync<TableEntity>(date,o.OrderId,cancellationToken:cancellationToken)).Value;
            if(e.GetString("CustomerName")!=o.CustomerName || e.GetString("SelectedItemSKUs")!=JsonSerializer.Serialize(o.SelectedItemSKUs) || e.GetDouble("TotalPrice")!=o.TotalPrice || e.GetDateTimeOffset("OrderTimestamp")!=o.OrderTimestamp.ToUniversalTime()) throw new InvalidOperationException("Order ID reused with different details.");
        }
        e=(await t.GetEntityAsync<TableEntity>(date,o.OrderId,cancellationToken:cancellationToken)).Value;
        var states=new[]{"Received","Preparing","Ready","Collected"};
        var index=Array.IndexOf(states,e.GetString("Status"));if(index<0)throw new InvalidOperationException("Unknown stored status.");
        logger.LogInformation("Order {OrderId}: {Status}",o.OrderId,states[index]);
        var delay=int.TryParse(Environment.GetEnvironmentVariable("PreparationDelaySeconds"),out var parsed)?Math.Clamp(parsed,0,30):3;
        for(var i=index+1;i<states.Length;i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay),cancellationToken);
            e["Status"]=states[i];e[states[i]+"At"]=DateTimeOffset.UtcNow;
            // Optimistic concurrency prevents another worker from regressing the state.
            await t.UpdateEntityAsync(e,e.ETag,TableUpdateMode.Replace,cancellationToken);
            e=(await t.GetEntityAsync<TableEntity>(date,o.OrderId,cancellationToken:cancellationToken)).Value;
            logger.LogInformation("Order {OrderId}: {Status}",o.OrderId,e.GetString("Status"));
        }
    }
    [Function("GetOrderStatus")]
    public async Task<IActionResult> Get([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="orders/{orderDate}/{orderId}")] HttpRequest req,string orderDate,string orderId)
    {
        if(!Storage.ValidKey(orderDate)||!Storage.ValidKey(orderId))return new BadRequestResult();
        var t=storage.Orders;await t.CreateIfNotExistsAsync();var found=await t.GetEntityIfExistsAsync<TableEntity>(orderDate,orderId);if(!found.HasValue)return new NotFoundObjectResult("Not processed yet, or order does not exist.");
        var e=found.Value;return new OkObjectResult(new{OrderId=e.RowKey,OrderDate=e.PartitionKey,CustomerName=e.GetString("CustomerName"),SelectedItemSKUs=JsonSerializer.Deserialize<string[]>(e.GetString("SelectedItemSKUs")!),TotalPrice=e.GetDouble("TotalPrice"),Status=e.GetString("Status"),ReceivedAt=e.GetDateTimeOffset("ReceivedAt"),PreparingAt=e.GetDateTimeOffset("PreparingAt"),ReadyAt=e.GetDateTimeOffset("ReadyAt"),CollectedAt=e.GetDateTimeOffset("CollectedAt")});
    }
}
