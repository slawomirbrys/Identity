using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace Brys.Identity.Storage.Stores;

public sealed class FileDeviceFlowStore : IDeviceFlowStore
{
    private const string CodesContainer = "device-codes";
    private const string UserIndexContainer = "device-code-user-index";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IStorageProvider _storage;

    public FileDeviceFlowStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    public async Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
    {
        var entry = ToEntry(userCode, data);
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await _storage.WriteAsync(CodesContainer, EncodeKey(deviceCode), json);
        await _storage.WriteAsync(UserIndexContainer, EncodeKey(userCode), deviceCode);
    }

    public async Task<DeviceCode?> FindByUserCodeAsync(string userCode)
    {
        var deviceCode = await _storage.ReadAsync(UserIndexContainer, EncodeKey(userCode));
        return deviceCode is null ? null : await FindByDeviceCodeAsync(deviceCode);
    }

    public async Task<DeviceCode?> FindByDeviceCodeAsync(string deviceCode)
    {
        var json = await _storage.ReadAsync(CodesContainer, EncodeKey(deviceCode));
        if (json is null) return null;

        var entry = JsonSerializer.Deserialize<DeviceCodeEntry>(json, JsonOptions);
        return entry is null ? null : FromEntry(entry);
    }

    public async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
    {
        var deviceCode = await _storage.ReadAsync(UserIndexContainer, EncodeKey(userCode));
        if (deviceCode is null) return;

        var entry = ToEntry(userCode, data);
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await _storage.WriteAsync(CodesContainer, EncodeKey(deviceCode), json);
    }

    public async Task RemoveByDeviceCodeAsync(string deviceCode)
    {
        var json = await _storage.ReadAsync(CodesContainer, EncodeKey(deviceCode));
        if (json is not null)
        {
            var entry = JsonSerializer.Deserialize<DeviceCodeEntry>(json, JsonOptions);
            if (entry is not null)
                await _storage.DeleteAsync(UserIndexContainer, EncodeKey(entry.UserCode));
        }

        await _storage.DeleteAsync(CodesContainer, EncodeKey(deviceCode));
    }

    private static DeviceCodeEntry ToEntry(string userCode, DeviceCode data) => new(
        userCode,
        data.ClientId,
        data.RequestedScopes.ToArray(),
        data.AuthorizedScopes?.ToArray(),
        data.IsAuthorized,
        data.IsOpenId,
        data.CreationTime,
        data.Lifetime,
        data.Description,
        data.SessionId,
        data.Subject?.Claims.Select(c => new SerializedClaim(c.Type, c.Value, c.ValueType, c.Issuer)).ToArray()
    );

    private static DeviceCode FromEntry(DeviceCodeEntry entry)
    {
        var deviceCode = new DeviceCode
        {
            ClientId = entry.ClientId,
            RequestedScopes = entry.RequestedScopes,
            AuthorizedScopes = entry.AuthorizedScopes ?? [],
            IsAuthorized = entry.IsAuthorized,
            IsOpenId = entry.IsOpenId,
            CreationTime = entry.CreationTime,
            Lifetime = entry.Lifetime,
            Description = entry.Description,
            SessionId = entry.SessionId
        };

        if (entry.SubjectClaims is not null)
        {
            var claims = entry.SubjectClaims.Select(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer));
            deviceCode.Subject = new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        return deviceCode;
    }

    private static string EncodeKey(string key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private sealed record DeviceCodeEntry(
        string UserCode,
        string ClientId,
        string[] RequestedScopes,
        string[]? AuthorizedScopes,
        bool IsAuthorized,
        bool IsOpenId,
        DateTime CreationTime,
        int Lifetime,
        string? Description,
        string? SessionId,
        SerializedClaim[]? SubjectClaims
    );

    private sealed record SerializedClaim(string Type, string Value, string ValueType, string Issuer);
}
