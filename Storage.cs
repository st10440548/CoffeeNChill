using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
namespace CoffeeNChill;
public sealed class Storage
{
    readonly string connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
    public TableClient Menu => new(connection, "MenuItems");
    public TableClient Orders => new(connection, "Orders");
    public QueueClient Queue => new(connection, "order-processing-queue", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    public ShareClient? Documents => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureFilesConnectionString")) ? null : new ShareClient(Environment.GetEnvironmentVariable("AzureFilesConnectionString"), "staff-docs");
    public static bool ValidKey(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(c => c is '/' or '\\' or '#' or '?' || char.IsControl(c));
}
