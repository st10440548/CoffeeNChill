using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
namespace CoffeeNChill;
public sealed class DocumentFunctions(Storage storage)
{
    static IActionResult Missing()=>new ObjectResult("Azure Files is not emulated by Azurite. Set AzureFilesConnectionString to a real Azure Storage account connection string for staff-docs."){StatusCode=503};
    [Function("UploadStaffDocument")]
    public async Task<IActionResult> Upload([HttpTrigger(AuthorizationLevel.Anonymous,"post",Route="documents/upload")] HttpRequest req)
    {
        var share=storage.Documents;if(share is null)return Missing();
        if(!req.HasFormContentType)return new BadRequestObjectResult("Use multipart/form-data with a file field.");
        var form=await req.ReadFormAsync();var f=form.Files.GetFile("file");if(f is null||f.Length==0)return new BadRequestObjectResult("Attach a nonempty file.");
        if(f.Length>10*1024*1024)return new ObjectResult("Maximum file size is 10 MiB."){StatusCode=413};
        var name=f.FileName;if(!Storage.ValidKey(name)||name.Contains(':')||name.IndexOfAny(new[]{'*','"','<','>','|'})>=0)return new BadRequestObjectResult("Invalid filename.");
        await share.CreateIfNotExistsAsync();var client=share.GetRootDirectoryClient().GetFileClient(name);
        await client.CreateAsync(f.Length);await using var stream=f.OpenReadStream();long offset=0;var buffer=new byte[1024*1024];int read;
        while((read=await stream.ReadAsync(buffer))>0){using var chunk=new MemoryStream(buffer,0,read,false);await client.UploadRangeAsync(new HttpRange(offset,read),chunk);offset+=read;}
        return new CreatedResult($"/api/documents/download/{Uri.EscapeDataString(name)}",new{FileName=name,Size=f.Length});
    }
    [Function("ListStaffDocuments")]
    public async Task<IActionResult> List([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="documents")] HttpRequest req)
    {
        var share=storage.Documents;if(share is null)return Missing();await share.CreateIfNotExistsAsync();var files=new List<object>();
        await foreach(var item in share.GetRootDirectoryClient().GetFilesAndDirectoriesAsync())if(!item.IsDirectory){var props=(await share.GetRootDirectoryClient().GetFileClient(item.Name).GetPropertiesAsync()).Value;files.Add(new{FileName=item.Name,Size=props.ContentLength,LastModified=props.LastModified});}
        return new OkObjectResult(files);
    }
    [Function("DownloadStaffDocument")]
    public async Task<IActionResult> Download([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="documents/download/{fileName}")] HttpRequest req,string fileName)
    {
        var share=storage.Documents;if(share is null)return Missing();if(!Storage.ValidKey(fileName))return new BadRequestResult();
        try{var download=(await share.GetRootDirectoryClient().GetFileClient(fileName).DownloadAsync()).Value;return new FileStreamResult(download.Content,"application/octet-stream"){FileDownloadName=fileName};}catch(RequestFailedException ex)when(ex.Status==404){return new NotFoundResult();}
    }
}
