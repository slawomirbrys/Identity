using Brys.Identity.Storage;
using Microsoft.Extensions.Options;

namespace Brys.Identity.Storage.Tests;

public class LocalFileStorageProviderTests
{
    private string _tempDir = string.Empty;
    private LocalFileStorageProvider _provider = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _provider = new LocalFileStorageProvider(Options.Create(new LocalFileStorageOptions { BasePath = _tempDir }));
    }

    [After(Test)]
    public void Cleanup()
    {
        _provider.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task ReadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var result = await _provider.ReadAsync("test", "missing-key");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReadAsync_WhenFileExists_ReturnsContent()
    {
        await _provider.WriteAsync("test", "key1", "hello world");

        var result = await _provider.ReadAsync("test", "key1");

        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task WriteAsync_CreatesFileWithCorrectContent()
    {
        await _provider.WriteAsync("container", "mykey", "test-content");

        var filePath = Path.Combine(_tempDir, "container", "mykey");
        await Assert.That(File.Exists(filePath)).IsTrue();
        await Assert.That(await File.ReadAllTextAsync(filePath)).IsEqualTo("test-content");
    }

    [Test]
    public async Task WriteAsync_CreatesContainerDirectoryIfMissing()
    {
        await _provider.WriteAsync("new-container", "key", "value");

        await Assert.That(Directory.Exists(Path.Combine(_tempDir, "new-container"))).IsTrue();
    }

    [Test]
    public async Task DeleteAsync_RemovesExistingFile()
    {
        await _provider.WriteAsync("test", "key1", "content");
        await _provider.DeleteAsync("test", "key1");

        var result = await _provider.ReadAsync("test", "key1");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        await _provider.DeleteAsync("test", "nonexistent-key");
    }

    [Test]
    public async Task ListKeysAsync_WhenContainerDoesNotExist_ReturnsEmpty()
    {
        var keys = await _provider.ListKeysAsync("nonexistent-container");

        await Assert.That(keys.Any()).IsFalse();
    }

    [Test]
    public async Task ListKeysAsync_ReturnsAllKeysInContainer()
    {
        await _provider.WriteAsync("test", "key1", "a");
        await _provider.WriteAsync("test", "key2", "b");
        await _provider.WriteAsync("test", "key3", "c");

        var keys = (await _provider.ListKeysAsync("test")).ToList();

        await Assert.That(keys.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ListKeysAsync_DoesNotReturnKeysFromOtherContainers()
    {
        await _provider.WriteAsync("container-a", "key1", "a");
        await _provider.WriteAsync("container-b", "key2", "b");

        var keys = (await _provider.ListKeysAsync("container-a")).ToList();

        await Assert.That(keys.Count).IsEqualTo(1);
        await Assert.That(keys[0]).IsEqualTo("key1");
    }

    [Test]
    public async Task WriteAsync_OverwritesExistingContent()
    {
        await _provider.WriteAsync("test", "key1", "original");
        await _provider.WriteAsync("test", "key1", "updated");

        var result = await _provider.ReadAsync("test", "key1");
        await Assert.That(result).IsEqualTo("updated");
    }
}
