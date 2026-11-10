using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.Gateway;

public sealed class UserStore(TableServiceClient storage)
{
    private readonly PasswordHasher<UserProfile> hasher = new();
    private TableClient Users => storage.GetTableClient("Users");

    public async Task<UserProfile?> FindAsync(string email)
    {
        var table = Users;
        await table.CreateIfNotExistsAsync();
        // PartitionKey is the role, so query the three allowed role partitions.
        foreach (var role in new[] { "Admin", "Staff", "Customer" })
        {
            var result = await table.GetEntityIfExistsAsync<UserProfile>(role, email);
            if (result.HasValue) return result.Value;
        }
        return null;
    }

    public async Task<bool> RegisterAsync(string email, string fullName, string password)
    {
        var table = Users;
        await table.CreateIfNotExistsAsync();
        if (await FindAsync(email) is not null) return false;
        // Public registration never grants Staff/Admin privileges.
        var user = new UserProfile { PartitionKey = "Customer", RowKey = email, FullName = fullName, CreatedDate = DateTimeOffset.UtcNow };
        user.PasswordHash = hasher.HashPassword(user, password);
        try { await table.AddEntityAsync(user); return true; }
        catch (RequestFailedException ex) when (ex.Status == 409) { return false; }
    }

    public async Task<UserProfile?> VerifyAsync(string email, string password)
    {
        var user = await FindAsync(email);
        if (user is null) return null;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await Users.UpdateEntityAsync(user, user.ETag, TableUpdateMode.Replace);
        }
        return user;
    }
}

public sealed class UserProfile : ITableEntity
{
    public string PartitionKey { get; set; } = "";
    public string RowKey { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
