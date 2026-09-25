using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Sentinel.Extensions;

/// <summary>
/// Applies deployment configuration before Sentinel services are registered.
/// Keep this small and deterministic: service registration and the request
/// pipeline rely on these values being established first.
/// </summary>
public static class SentinelConfigurationExtensions
{
    public static bool ConfigureSentinelDeployment(this WebApplicationBuilder builder)
    {
        var useForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:UseForwardedHeaders");
        if (useForwardedHeaders)
        {
            // Docker publishes only Caddy. The private Sentinel service therefore
            // accepts forwarded scheme/client information from that internal proxy.
            // Do not enable this setting when Kestrel is published directly.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        ApplyEnvironmentOverride(builder.Configuration, "ConnectionStrings__DefaultConnection", "ConnectionStrings:DefaultConnection");
        ApplyEnvironmentOverride(builder.Configuration, "Geocoding__ApiKey", "Geocoding:ApiKey");
        ApplyEnvironmentOverride(builder.Configuration, "Geocoding__Email", "Geocoding:Email");

        // Keep the default request budget aligned with the protected-attachment limit.
        // Jurisdiction shapefile endpoints explicitly opt into their larger 100 MB limit.
        var configuredProtectedUploadBytes = builder.Configuration.GetValue<long?>("FileStorage:MaxUploadBytes");
        var defaultMultipartRequestLimit = configuredProtectedUploadBytes is > 0
            ? configuredProtectedUploadBytes.Value
            : 26_214_400L;

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = defaultMultipartRequestLimit;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
        });

        builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = defaultMultipartRequestLimit;
            options.ValueLengthLimit = 1_048_576; // 1 MB per non-file form value
            options.MultipartHeadersLengthLimit = 16384;
            options.BufferBodyLengthLimit = defaultMultipartRequestLimit;
        });

        return useForwardedHeaders;
    }

    private static void ApplyEnvironmentOverride(IConfiguration configuration, string environmentVariable, string configurationKey)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            configuration[configurationKey] = value;
        }
    }
}
