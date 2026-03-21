using Microsoft.AspNetCore.Authentication;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Authorization;

/// <summary>
/// Transforms user claims on each request to include permissions from the database
/// This allows Razor views to use User.HasClaim() to check permissions
/// </summary>
public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IPermissionService _permissionService;

    public PermissionClaimsTransformation(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only add permission claims if user is authenticated
        if (!principal.Identity?.IsAuthenticated ?? true)
        {
            return principal;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        // Check if permission claims are already added (avoid duplicate processing)
        if (principal.HasClaim(c => c.Type == "Permission"))
        {
            return principal;
        }

        // Get user's permissions from database
        var permissions = await _permissionService.GetUserPermissionsAsync(userId);

        // Create a new identity with permission claims
        var claimsIdentity = new ClaimsIdentity();
        
        foreach (var permission in permissions)
        {
            // Add permission as claim: Type="Permission", Value="Patient.Delete"
            claimsIdentity.AddClaim(new Claim("Permission", permission.Name));
        }

        // Add the claims to the principal
        principal.AddIdentity(claimsIdentity);

        return principal;
    }
}
