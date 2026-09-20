using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentinel.Data;
using Sentinel.Services;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Runs authorization integration tests without a local SQL Server, startup
/// migrations, or production logging sinks. Tests seed only the records they
/// need, which makes an HTTP status an authorization result rather than an
/// infrastructure side effect.
/// </summary>
public sealed class SentinelWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();

            var databaseName = $"SentinelAuthorizationTests-{Guid.NewGuid():N}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            // Keep the test host self-contained. The desktop default key-ring
            // location is not available to the sandboxed test runner, and
            // antiforgery plus any production cookie challenge must still use
            // a functioning Data Protection provider during integration tests.
            var keyRingDirectory = Directory.CreateDirectory(Path.Combine(
                Path.GetTempPath(),
                "SentinelTests",
                "DataProtection",
                Guid.NewGuid().ToString("N")));
            services.AddDataProtection()
                .SetApplicationName("Sentinel.Authorization.Tests")
                .PersistKeysToFileSystem(keyRingDirectory);

            // Authorization tests must prove the controller/policy boundary,
            // not depend on an external geocoding provider being reachable.
            services.RemoveAll<ILocationLookupService>();
            services.AddSingleton<ILocationLookupService, TestLocationLookupService>();

            // One deterministic test scheme serves every request. Individual
            // clients declare their synthetic user/permissions in headers, so
            // test cases cannot leak a previous client's permission set.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });

            // Route-inventory tests intentionally challenge every protected
            // mutation endpoint. Disable the production global limiter only
            // in this in-memory test host so a rate-limit response cannot
            // mask an authentication failure later in the enumeration.
            services.PostConfigure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = null;
            });

            services.RemoveAll<IAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, TestPermissionHandler>();
        });
    }

    private sealed class TestLocationLookupService : ILocationLookupService
    {
        public Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address) =>
            Task.FromResult<(double? Latitude, double? Longitude)>((null, null));

        public Task<List<AddressLookupResult>> SearchAddressesAsync(
            string query,
            int limit = 5,
            double? biasLatitude = null,
            double? biasLongitude = null) =>
            Task.FromResult(new List<AddressLookupResult>());

        public Task<List<PlaceLookupResult>> SearchPlacesAsync(
            string query,
            int limit = 5,
            double? biasLatitude = null,
            double? biasLongitude = null) =>
            Task.FromResult(new List<PlaceLookupResult>());
    }
}
