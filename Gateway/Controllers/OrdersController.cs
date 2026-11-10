using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

public sealed record OrderRequest(string OrderId, string CustomerName, string[] SelectedItemSKUs, double TotalPrice, DateTimeOffset OrderTimestamp);

/// <summary>Public order submission and status retrieval.</summary>
[ApiController, Route("api/orders")]
public sealed class OrdersController(IHttpClientFactory clients) : ControllerBase
{
    private HttpClient Client => clients.CreateClient("functions");
    private static async Task<IActionResult> Forward(HttpResponseMessage response) =>
        new ContentResult { Content = await response.Content.ReadAsStringAsync(), ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", StatusCode = (int)response.StatusCode };
    /// <summary>Submit an order asynchronously; returns 202 when queued.</summary>
    [HttpPost("queue"), AllowAnonymous]
    public async Task<IActionResult> Queue(OrderRequest input) => await Forward(await Client.PostAsJsonAsync("api/orders/queue", input));
    /// <summary>Get the status of an existing order.</summary>
    [HttpGet("{orderDate}/{orderId}"), AllowAnonymous]
    public async Task<IActionResult> Get(string orderDate, string orderId) =>
        await Forward(await Client.GetAsync($"api/orders/{Uri.EscapeDataString(orderDate)}/{Uri.EscapeDataString(orderId)}"));
}
