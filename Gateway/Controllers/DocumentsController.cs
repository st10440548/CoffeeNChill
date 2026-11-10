using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>Staff operational files backed by Azure Files.</summary>
[ApiController, Route("api/documents"), Authorize(Roles = "Admin,Staff")]
public sealed class DocumentsController(IHttpClientFactory clients) : ControllerBase
{
    private HttpClient Client => clients.CreateClient("functions");
    private static async Task<IActionResult> Forward(HttpResponseMessage response) =>
        new ContentResult { Content = await response.Content.ReadAsStringAsync(), ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", StatusCode = (int)response.StatusCode };

    /// <summary>List staff files.</summary>
    [HttpGet]
    public async Task<IActionResult> List() => await Forward(await Client.GetAsync("api/documents"));

    /// <summary>Upload a staff document (maximum 10 MiB).</summary>
    [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 10 * 1024 * 1024) return BadRequest("File size must be 1 byte to 10 MiB.");
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", file.FileName);
        return await Forward(await Client.PostAsync("api/documents/upload", form));
    }

    /// <summary>Download a staff document.</summary>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Download(string fileName)
    {
        using var response = await Client.GetAsync("api/documents/download/" + Uri.EscapeDataString(fileName));
        if (!response.IsSuccessStatusCode) return await Forward(response);
        return File(await response.Content.ReadAsByteArrayAsync(), "application/octet-stream", fileName);
    }

    /// <summary>Delete a staff document (not supported by the existing Functions backend).</summary>
    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName) => StatusCode(501, "Document deletion is pending backend implementation.");
}
