using System.Text.Json;
using Brys.Identity.Storage;
using Brys.Identity.Storage.Stores;
using Duende.IdentityServer.Models;
using Moq;

namespace Brys.Identity.Storage.Tests.Stores;

public class FileDeviceFlowStoreTests
{
    private Mock<IStorageProvider> _storageMock = null!;
    private FileDeviceFlowStore _store = null!;

    [Before(Test)]
    public void Setup()
    {
        _storageMock = new Mock<IStorageProvider>();
        _store = new FileDeviceFlowStore(_storageMock.Object);
    }

    [Test]
    public async Task StoreDeviceAuthorizationAsync_WritesCodeAndUserIndex()
    {
        var deviceCode = BuildDeviceCode("client1");

        await _store.StoreDeviceAuthorizationAsync("device123", "user123", deviceCode);

        _storageMock.Verify(
            s => s.WriteAsync("device-codes", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _storageMock.Verify(
            s => s.WriteAsync("device-code-user-index", It.IsAny<string>(), "device123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FindByUserCodeAsync_WhenIndexMissing_ReturnsNull()
    {
        _storageMock
            .Setup(s => s.ReadAsync("device-code-user-index", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _store.FindByUserCodeAsync("user123");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindByDeviceCodeAsync_WhenCodeMissing_ReturnsNull()
    {
        _storageMock
            .Setup(s => s.ReadAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _store.FindByDeviceCodeAsync("device123");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindByDeviceCodeAsync_WhenCodeExists_ReturnsDeserializedDeviceCode()
    {
        var json = BuildDeviceCodeEntryJson("user123", "client1");
        _storageMock
            .Setup(s => s.ReadAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _store.FindByDeviceCodeAsync("device123");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ClientId).IsEqualTo("client1");
        await Assert.That(result.IsOpenId).IsTrue();
    }

    [Test]
    public async Task UpdateByUserCodeAsync_WhenIndexMissing_DoesNotWrite()
    {
        _storageMock
            .Setup(s => s.ReadAsync("device-code-user-index", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await _store.UpdateByUserCodeAsync("user123", BuildDeviceCode("client1"));

        _storageMock.Verify(
            s => s.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateByUserCodeAsync_WhenIndexExists_UpdatesDeviceCode()
    {
        _storageMock
            .Setup(s => s.ReadAsync("device-code-user-index", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("device123");

        await _store.UpdateByUserCodeAsync("user123", BuildDeviceCode("client1"));

        _storageMock.Verify(
            s => s.WriteAsync("device-codes", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveByDeviceCodeAsync_DeletesBothCodeAndUserIndex()
    {
        var json = BuildDeviceCodeEntryJson("user123", "client1");
        _storageMock
            .Setup(s => s.ReadAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        await _store.RemoveByDeviceCodeAsync("device123");

        _storageMock.Verify(
            s => s.DeleteAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _storageMock.Verify(
            s => s.DeleteAsync("device-code-user-index", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveByDeviceCodeAsync_WhenCodeMissing_StillDeletesCodeEntry()
    {
        _storageMock
            .Setup(s => s.ReadAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await _store.RemoveByDeviceCodeAsync("device123");

        _storageMock.Verify(
            s => s.DeleteAsync("device-codes", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _storageMock.Verify(
            s => s.DeleteAsync("device-code-user-index", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DeviceCode BuildDeviceCode(string clientId) => new()
    {
        ClientId = clientId,
        RequestedScopes = ["openid", "profile"],
        IsOpenId = true,
        CreationTime = DateTime.UtcNow,
        Lifetime = 300
    };

    private static string BuildDeviceCodeEntryJson(string userCode, string clientId) =>
        JsonSerializer.Serialize(new
        {
            UserCode = userCode,
            ClientId = clientId,
            RequestedScopes = new[] { "openid" },
            AuthorizedScopes = (string[]?)null,
            IsAuthorized = false,
            IsOpenId = true,
            CreationTime = DateTime.UtcNow,
            Lifetime = 300,
            Description = (string?)null,
            SessionId = (string?)null,
            SubjectClaims = (object[]?)null
        });
}
