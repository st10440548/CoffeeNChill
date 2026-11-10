using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.Gateway;

public sealed class UserStore(TableServiceClient storage)
{
    private readonly PasswordHasher<UserProfile> hasher = new();
    private TableClient Users => storage.GetTableClient("Users");
    private TableClient EmailIndex => storage.GetTableClient("UserEmailIndex");

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

    public async Task<bool> RegisterAsync(string email, string fullName, string password) =>
        await CreateAsync(email, fullName, password, "Customer");

    public async Task<bool> CreateAsync(string email, string fullName, string password, string role)
    {
        if (role is not ("Customer" or "Staff" or "Admin")) throw new ArgumentException("Invalid role.");
        var table = Users;
        await table.CreateIfNotExistsAsync();
        var index = EmailIndex;
        await index.CreateIfNotExistsAsync();
        if (await FindAsync(email) is not null) return false;
        // Unique email reservation blocks concurrent registrations across role partitions.
        var reservation = new TableEntity("Email", email) { ["Role"] = role };
        try { await index.AddEntityAsync(reservation); }
        catch (RequestFailedException ex) when (ex.Status == 409) { return false; }
        var user = new UserProfile { PartitionKey = role, RowKey = email, FullName = fullName, CreatedDate = DateTimeOffset.UtcNow };
        user.PasswordHash = hasher.HashPassword(user, password);
        try { await table.AddEntityAsync(user); return true; }
        catch
        {
            // Preserve the reservation on failure to avoid cross-role duplicate accounts.
            throw;
        }
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
