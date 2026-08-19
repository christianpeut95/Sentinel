using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class UserSessionInvalidationService : IUserSessionInvalidationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserSessionInvalidationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task InvalidateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
    }

    public async Task InvalidateUsersInRoleAsync(string roleId)
    {
        var userIds = await _context.UserRoles
            .Where(userRole => userRole.RoleId == roleId)
            .Select(userRole => userRole.UserId)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            await InvalidateUserAsync(userId);
        }
    }
}
