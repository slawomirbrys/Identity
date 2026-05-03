using System.Text;
using System.Text.Json;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace Brys.Identity.Storage.Stores;

public sealed class FilePersistedGrantStore : IPersistedGrantStore
{
    private const string Container = "persisted-grants";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IStorageProvider _storage;

    public FilePersistedGrantStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    public async Task StoreAsync(PersistedGrant grant)
    {
        var json = JsonSerializer.Serialize(grant, JsonOptions);
        await _storage.WriteAsync(Container, EncodeKey(grant.Key), json);
    }

    public async Task<PersistedGrant?> GetAsync(string key)
    {
        var json = await _storage.ReadAsync(Container, EncodeKey(key));
        return json is null ? null : JsonSerializer.Deserialize<PersistedGrant>(json, JsonOptions);
    }

    public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        var fileKeys = await _storage.ListKeysAsync(Container);
        var grants = new List<PersistedGrant>();

        foreach (var fileKey in fileKeys)
        {
            var json = await _storage.ReadAsync(Container, fileKey);
            if (json is null) continue;

            var grant = JsonSerializer.Deserialize<PersistedGrant>(json, JsonOptions);
            if (grant is not null && Matches(grant, filter))
                grants.Add(grant);
        }

        return grants;
    }

    public async Task RemoveAsync(string key)
    {
        await _storage.DeleteAsync(Container, EncodeKey(key));
    }

    public async Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        var fileKeys = await _storage.ListKeysAsync(Container);

        foreach (var fileKey in fileKeys)
        {
            var json = await _storage.ReadAsync(Container, fileKey);
            if (json is null) continue;

            var grant = JsonSerializer.Deserialize<PersistedGrant>(json, JsonOptions);
            if (grant is not null && Matches(grant, filter))
                await _storage.DeleteAsync(Container, fileKey);
        }
    }

    private static bool Matches(PersistedGrant grant, PersistedGrantFilter filter)
    {
        if (filter.SubjectId is not null && grant.SubjectId != filter.SubjectId) return false;
        if (filter.SessionId is not null && grant.SessionId != filter.SessionId) return false;
        if (filter.ClientId is not null && grant.ClientId != filter.ClientId) return false;
        if (filter.Type is not null && grant.Type != filter.Type) return false;
        return true;
    }

    internal static string EncodeKey(string key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
