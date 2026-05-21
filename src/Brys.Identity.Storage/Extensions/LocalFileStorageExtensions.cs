using Brys.Identity.Storage.Stores;
using Duende.IdentityServer;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Brys.Identity.Storage.Extensions;

public static class LocalFileStorageExtensions
{
    public static IIdentityServerBuilder AddLocalFileStorage(
        this IIdentityServerBuilder builder,
        Action<LocalFileStorageOptions>? configure = null)
    {
        builder.Services.Configure<LocalFileStorageOptions>(opts =>
        {
            configure?.Invoke(opts);
        });

        builder.Services.AddSingleton<IStorageProvider, LocalFileStorageProvider>();
        builder.Services.AddTransient<IPersistedGrantStore, FilePersistedGrantStore>();
        builder.Services.AddTransient<IDeviceFlowStore, FileDeviceFlowStore>();
        builder.Services.AddSingleton<ISigningKeyStore, FileSigningKeyStore>();

        return builder;
    }
}
