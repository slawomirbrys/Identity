namespace Brys.Identity.Storage;

public interface IStorageProvider
{
    Task<string?> ReadAsync(string container, string key, CancellationToken cancellationToken = default);
    Task WriteAsync(string container, string key, string content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListKeysAsync(string container, CancellationToken cancellationToken = default);
}
