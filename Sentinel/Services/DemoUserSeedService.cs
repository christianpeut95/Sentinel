using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Models;

namespace Sentinel.Services;

public static class DemoUserSeedService
{
    private static readonly (string Email, string Password, string FirstName, string LastName, string Role, bool IsInterviewWorker)[] DemoUsers =
    [
        ("manager@sentinel-demo.com", "Demo123!@#Manager", "Emma", "Thompson", "Surveillance Manager", false),
        ("officer@sentinel-demo.com", "Demo123!@#Officer", "Isabella", "Chen", "Surveillance Officer", false),
        ("tracer@sentinel-demo.com", "Demo123!@#Tracer", "Emma", "Rodriguez", "Contact Tracer", true),
        ("supervisor@sentinel-demo.com", "Demo123!@#Supervisor", "James", "Wilson", "Surveillance Manager", false),
        ("stiofficer@sentinel-demo.com", "Demo123!@#STI", "Megge", "Taylor", "Surveillance Officer", false),
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue<bool>("Demo:EnableDemoUsers"))
            return;

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        logger.LogInformation("Demo mode enabled — seeding demo users...");

        foreach (var (email, password, firstName, lastName, role, isInterviewWorker) in DemoUsers)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                logger.LogDebug("Demo user {Email} already exists, skipping", email);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                IsInterviewWorker = isInterviewWorker,
                AvailableForAutoAssignment = isInterviewWorker,
                CurrentTaskCapacity = isInterviewWorker ? 20 : 10,
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Created demo user {Email} with role {Role}", email, role);
            }
            else
            {
                logger.LogWarning("Failed to create demo user {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        logger.LogInformation("Demo user seeding complete");
    }
}
