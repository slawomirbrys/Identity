using System.Text.Json;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace Brys.Identity.Storage.Stores;

public sealed class FileSigningKeyStore : ISigningKeyStore
{
    private const string Container = "signing-keys";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IStorageProvider _storage;

    public FileSigningKeyStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    public async Task<IEnumerable<SerializedKey>> LoadKeysAsync()
    {
        var fileKeys = await _storage.ListKeysAsync(Container);
        var result = new List<SerializedKey>();

        foreach (var fileKey in fileKeys)
        {
            var json = await _storage.ReadAsync(Container, fileKey);
            if (json is null) continue;

            var key = JsonSerializer.Deserialize<SerializedKey>(json, JsonOptions);
            if (key is not null)
                result.Add(key);
        }

        return result;
    }

    public async Task StoreKeyAsync(SerializedKey key)
    {
        var json = JsonSerializer.Serialize(key, JsonOptions);
        await _storage.WriteAsync(Container, key.Id, json);
    }

    public async Task DeleteKeyAsync(string id)
    {
        await _storage.DeleteAsync(Container, id);
    }
}
