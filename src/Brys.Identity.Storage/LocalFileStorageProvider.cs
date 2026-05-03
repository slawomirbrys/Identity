using Microsoft.Extensions.Options;

namespace Brys.Identity.Storage;

public sealed class LocalFileStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public LocalFileStorageProvider(IOptions<LocalFileStorageOptions> options)
    {
        _basePath = options.Value.BasePath;
    }

    public async Task<string?> ReadAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(container, key);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task WriteAsync(string container, string key, string content, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.Combine(_basePath, container));
            await File.WriteAllTextAsync(GetPath(container, key), content, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(container, key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListKeysAsync(string container, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_basePath, container);
        if (!Directory.Exists(dir))
            return Task.FromResult(Enumerable.Empty<string>());

        var keys = Directory.GetFiles(dir)
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Cast<string>();

        return Task.FromResult(keys);
    }

    private string GetPath(string container, string key) =>
        Path.Combine(_basePath, container, key);
}
