using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

public sealed record MenuInput(string Category, string Id, string Name, string Description, double Price, bool IsAvailable);
public sealed record MenuUpdate(double? Price, bool? IsAvailable);

/// <summary>Public menu browsing and staff-only modifications.</summary>
[ApiController, Route("api/menu")]
public sealed class MenuController(IHttpClientFactory clients) : ControllerBase
{
    private HttpClient Client => clients.CreateClient("functions");
    private static async Task<IActionResult> Forward(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return new ContentResult { Content = body, ContentType = contentType, StatusCode = (int)response.StatusCode };
    }
    /// <summary>List all menu items.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Get() => await Forward(await Client.GetAsync("api/menu"));
    /// <summary>Filter menu items by category.</summary>
    [HttpGet("category/{category}"), AllowAnonymous]
    public async Task<IActionResult> Category(string category) => await Forward(await Client.GetAsync("api/menu/category/" + Uri.EscapeDataString(category)));
    /// <summary>Create a menu item (Staff/Admin).</summary>
    [HttpPost, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(MenuInput input) => await Forward(await Client.PostAsJsonAsync("api/menu", input));
    /// <summary>Update menu pricing or availability (Staff/Admin).</summary>
    [HttpPut("{category}/{id}"), Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(string category, string id, MenuUpdate input) =>
        await Forward(await Client.PutAsJsonAsync($"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", input));
    /// <summary>Delete a menu item (Staff/Admin).</summary>
    [HttpDelete("{category}/{id}"), Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(string category, string id) =>
        await Forward(await Client.DeleteAsync($"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}"));
}
