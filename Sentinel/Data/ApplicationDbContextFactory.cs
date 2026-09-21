using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Data;

/// <summary>
/// Provides EF Core tooling with a context without starting Sentinel's hosted
/// services. This keeps migration creation independent of a running local SQL
/// Server instance.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectDirectory = File.Exists(Path.Combine(currentDirectory, "appsettings.json"))
            ? currentDirectory
            : Path.Combine(currentDirectory, "Sentinel");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
