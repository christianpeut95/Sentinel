using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentinel.Data;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Hosts the production Identity cookie pipeline against an isolated in-memory
/// database. Unlike <see cref="SentinelWebApplicationFactory"/>, it deliberately
/// does not replace authentication with a synthetic test scheme.
/// </summary>
public sealed class IdentityCookieWebApplicationFactory : WebApplicationFactory<Program>
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

            var databaseName = $"SentinelIdentityCookieTests-{Guid.NewGuid():N}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            // Exercise the production Identity/antiforgery pipeline with an
            // isolated writable key ring instead of the desktop profile's
            // default key location, which is unavailable to test runners.
            var keyRingDirectory = Directory.CreateDirectory(Path.Combine(
                Path.GetTempPath(),
                "SentinelTests",
                "DataProtection",
                Guid.NewGuid().ToString("N")));
            services.AddDataProtection()
                .SetApplicationName("Sentinel.IdentityCookie.Tests")
                .PersistKeysToFileSystem(keyRingDirectory);

            // Make the test prove Sentinel's explicit request-time session
            // check, rather than accidentally passing through Identity's
            // separately configured immediate stamp validator.
            services.PostConfigure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.FromDays(1));
        });
    }
}
