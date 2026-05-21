using System.Text.Json;
using Brys.Identity.Storage;
using Brys.Identity.Storage.Stores;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Moq;

namespace Brys.Identity.Storage.Tests.Stores;

public class FilePersistedGrantStoreTests
{
    private Mock<IStorageProvider> _storageMock = null!;
    private FilePersistedGrantStore _store = null!;

    [Before(Test)]
    public void Setup()
    {
        _storageMock = new Mock<IStorageProvider>();
        _store = new FilePersistedGrantStore(_storageMock.Object);
    }

    [Test]
    public async Task StoreAsync_WritesSerializedGrantToStorage()
    {
        var grant = BuildGrant("grant-key", "authorization_code", "user1", "client1");

        await _store.StoreAsync(grant);

        _storageMock.Verify(
            s => s.WriteAsync(
                "persisted-grants",
                It.IsAny<string>(),
                It.Is<string>(json => json.Contains("authorization_code")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task StoreAsync_EncodesKeyAsFileName()
    {
        var grant = BuildGrant("my/special+key", "code", "user1", "client1");

        await _store.StoreAsync(grant);

        _storageMock.Verify(
            s => s.WriteAsync(
                "persisted-grants",
                It.Is<string>(k => !k.Contains('/') && !k.Contains('+')),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAsync_WhenGrantExists_ReturnsDeserializedGrant()
    {
        var grant = BuildGrant("grant-key", "authorization_code", "user1", "client1");
        _storageMock
            .Setup(s => s.ReadAsync("persisted-grants", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(grant));

        var result = await _store.GetAsync("grant-key");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Key).IsEqualTo("grant-key");
        await Assert.That(result.ClientId).IsEqualTo("client1");
        await Assert.That(result.SubjectId).IsEqualTo("user1");
    }

    [Test]
    public async Task GetAsync_WhenGrantNotFound_ReturnsNull()
    {
        _storageMock
            .Setup(s => s.ReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _store.GetAsync("missing-key");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task RemoveAsync_CallsDeleteOnStorage()
    {
        await _store.RemoveAsync("grant-key");

        _storageMock.Verify(
            s => s.DeleteAsync("persisted-grants", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAllAsync_ReturnsOnlyGrantsMatchingSubjectIdFilter()
    {
        var grant1 = BuildGrant("k1", "code", "user1", "client1");
        var grant2 = BuildGrant("k2", "code", "user2", "client1");

        _storageMock
            .Setup(s => s.ListKeysAsync("persisted-grants", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["file1", "file2"]);

        _storageMock
            .SetupSequence(s => s.ReadAsync("persisted-grants", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(grant1))
            .ReturnsAsync(JsonSerializer.Serialize(grant2));

        var results = (await _store.GetAllAsync(new PersistedGrantFilter { SubjectId = "user1" })).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].SubjectId).IsEqualTo("user1");
    }

    [Test]
    public async Task GetAllAsync_WithNoFilterCriteria_ReturnsAllGrants()
    {
        var grant1 = BuildGrant("k1", "code", "user1", "client1");
        var grant2 = BuildGrant("k2", "refresh_token", "user2", "client2");

        _storageMock
            .Setup(s => s.ListKeysAsync("persisted-grants", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["file1", "file2"]);

        _storageMock
            .SetupSequence(s => s.ReadAsync("persisted-grants", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(grant1))
            .ReturnsAsync(JsonSerializer.Serialize(grant2));

        var results = (await _store.GetAllAsync(new PersistedGrantFilter())).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveAllAsync_DeletesGrantsMatchingFilter()
    {
        var grant1 = BuildGrant("k1", "code", "user1", "client1");
        var grant2 = BuildGrant("k2", "code", "user2", "client1");

        _storageMock
            .Setup(s => s.ListKeysAsync("persisted-grants", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["file1", "file2"]);

        _storageMock
            .SetupSequence(s => s.ReadAsync("persisted-grants", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(grant1))
            .ReturnsAsync(JsonSerializer.Serialize(grant2));

        await _store.RemoveAllAsync(new PersistedGrantFilter { SubjectId = "user1" });

        _storageMock.Verify(
            s => s.DeleteAsync("persisted-grants", "file1", It.IsAny<CancellationToken>()),
            Times.Once);
        _storageMock.Verify(
            s => s.DeleteAsync("persisted-grants", "file2", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static PersistedGrant BuildGrant(string key, string type, string subjectId, string clientId) => new()
    {
        Key = key,
        Type = type,
        SubjectId = subjectId,
        ClientId = clientId,
        CreationTime = DateTime.UtcNow,
        Data = "{}"
    };
}
