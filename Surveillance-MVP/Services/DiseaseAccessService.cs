using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Services
{
    public class DiseaseAccessService : IDiseaseAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DiseaseAccessService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> CanAccessDiseaseAsync(string userId, Guid diseaseId)
        {
            // 1. Check if disease exists and get its access level with parent info
            var disease = await _context.Diseases
                .Include(d => d.ParentDisease)
                .FirstOrDefaultAsync(d => d.Id == diseaseId);
                
            if (disease == null)
                return false;

            // 2. Check the entire parent hierarchy for restrictions
            // Start with the current disease and walk up the parent chain
            var diseaseToCheck = disease;
            var diseaseIdsToCheck = new List<Guid>();
            
            while (diseaseToCheck != null)
            {
                diseaseIdsToCheck.Add(diseaseToCheck.Id);
                
                // If any disease in the hierarchy is restricted, we need to verify access
                if (diseaseToCheck.AccessLevel == DiseaseAccessLevel.Restricted)
                {
                    // Check if user has access to this specific restricted disease
                    var hasAccess = await HasAccessToSpecificDiseaseAsync(userId, diseaseToCheck.Id);
                    if (!hasAccess)
                        return false; // Blocked by restricted parent
                }
                
                // Move to parent
                if (diseaseToCheck.ParentDiseaseId.HasValue)
                {
                    diseaseToCheck = await _context.Diseases
                        .Include(d => d.ParentDisease)
                        .FirstOrDefaultAsync(d => d.Id == diseaseToCheck.ParentDiseaseId.Value);
                }
                else
                {
                    diseaseToCheck = null;
                }
            }

            // 3. If we got here, either all diseases in hierarchy are public, 
            // or user has access to all restricted ones
            return true;
        }

        private async Task<bool> HasAccessToSpecificDiseaseAsync(string userId, Guid diseaseId)
        {
            // 1. Check user-specific access (overrides role access)
            var userAccess = await _context.UserDiseaseAccess
                .FirstOrDefaultAsync(uda =>
                    uda.UserId == userId &&
                    uda.DiseaseId == diseaseId &&
                    uda.IsAllowed &&
                    (uda.ExpiresAt == null || uda.ExpiresAt > DateTime.UtcNow));

            if (userAccess != null)
                return true;

            // 2. Check role-based access
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            
            foreach (var roleName in userRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                    continue;

                var roleAccess = await _context.RoleDiseaseAccess
                    .FirstOrDefaultAsync(rda =>
                        rda.RoleId == role.Id &&
                        rda.DiseaseId == diseaseId &&
                        rda.IsAllowed);

                if (roleAccess != null)
                    return true;
            }

            // 3. No access found
            return false;
        }

        public async Task<List<Guid>> GetAccessibleDiseaseIdsAsync(string userId)
        {
            // Get all diseases with their parent information
            var allDiseases = await _context.Diseases
                .Include(d => d.ParentDisease)
                .ToListAsync();

            var accessibleDiseaseIds = new HashSet<Guid>();

            // Check each disease individually using the hierarchy-aware method
            foreach (var disease in allDiseases)
            {
                if (await CanAccessDiseaseAsync(userId, disease.Id))
                {
                    accessibleDiseaseIds.Add(disease.Id);
                }
            }

            return accessibleDiseaseIds.ToList();
        }

        public async Task GrantDiseaseAccessToRoleAsync(string roleId, Guid diseaseId, string grantedByUserId, bool applyToChildren = false)
        {
            var existingAccess = await _context.RoleDiseaseAccess
                .FirstOrDefaultAsync(rda => rda.RoleId == roleId && rda.DiseaseId == diseaseId);

            if (existingAccess != null)
            {
                existingAccess.IsAllowed = true;
                existingAccess.CreatedAt = DateTime.UtcNow;
                existingAccess.CreatedByUserId = grantedByUserId;
                existingAccess.ApplyToChildren = applyToChildren;
                existingAccess.InheritedFromDiseaseId = null; // Direct grant, not inherited
            }
            else
            {
                _context.RoleDiseaseAccess.Add(new RoleDiseaseAccess
                {
                    RoleId = roleId,
                    DiseaseId = diseaseId,
                    IsAllowed = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = grantedByUserId,
                    ApplyToChildren = applyToChildren
                });
            }

            await _context.SaveChangesAsync();

            // If applyToChildren is true, create inherited access for all child diseases
            if (applyToChildren)
            {
                await ApplyRoleAccessToChildrenAsync(roleId, diseaseId, grantedByUserId);
            }
        }

        private async Task ApplyRoleAccessToChildrenAsync(string roleId, Guid parentDiseaseId, string grantedByUserId)
        {
            var childDiseaseIds = await GetAllChildDiseaseIdsAsync(parentDiseaseId);
            
            foreach (var childDiseaseId in childDiseaseIds)
            {
                var existingChildAccess = await _context.RoleDiseaseAccess
                    .FirstOrDefaultAsync(rda => rda.RoleId == roleId && rda.DiseaseId == childDiseaseId);

                if (existingChildAccess != null)
                {
                    // Update to mark as inherited
                    existingChildAccess.IsAllowed = true;
                    existingChildAccess.InheritedFromDiseaseId = parentDiseaseId;
                    existingChildAccess.CreatedAt = DateTime.UtcNow;
                    existingChildAccess.CreatedByUserId = grantedByUserId;
                }
                else
                {
                    // Create new inherited access
                    _context.RoleDiseaseAccess.Add(new RoleDiseaseAccess
                    {
                        RoleId = roleId,
                        DiseaseId = childDiseaseId,
                        IsAllowed = true,
                        InheritedFromDiseaseId = parentDiseaseId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = grantedByUserId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokeDiseaseAccessFromRoleAsync(string roleId, Guid diseaseId, bool revokeFromChildren = false)
        {
            var access = await _context.RoleDiseaseAccess
                .FirstOrDefaultAsync(rda => rda.RoleId == roleId && rda.DiseaseId == diseaseId);

            if (access != null)
            {
                _context.RoleDiseaseAccess.Remove(access);
                
                // If revoking from children, also remove inherited access
                if (revokeFromChildren || access.ApplyToChildren)
                {
                    await RevokeRoleAccessFromChildrenAsync(roleId, diseaseId);
                }
                
                await _context.SaveChangesAsync();
            }
        }

        private async Task RevokeRoleAccessFromChildrenAsync(string roleId, Guid parentDiseaseId)
        {
            var inheritedAccess = await _context.RoleDiseaseAccess
                .Where(rda => rda.RoleId == roleId && rda.InheritedFromDiseaseId == parentDiseaseId)
                .ToListAsync();

            _context.RoleDiseaseAccess.RemoveRange(inheritedAccess);
            await _context.SaveChangesAsync();
        }

        public async Task GrantDiseaseAccessToUserAsync(string userId, Guid diseaseId, string grantedByUserId, DateTime? expiresAt = null, string? reason = null, bool applyToChildren = false)
        {
            var existingAccess = await _context.UserDiseaseAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DiseaseId == diseaseId);

            if (existingAccess != null)
            {
                existingAccess.IsAllowed = true;
                existingAccess.CreatedAt = DateTime.UtcNow;
                existingAccess.ExpiresAt = expiresAt;
                existingAccess.GrantedByUserId = grantedByUserId;
                existingAccess.Reason = reason;
                existingAccess.ApplyToChildren = applyToChildren;
                existingAccess.InheritedFromDiseaseId = null; // Direct grant, not inherited
            }
            else
            {
                _context.UserDiseaseAccess.Add(new UserDiseaseAccess
                {
                    UserId = userId,
                    DiseaseId = diseaseId,
                    IsAllowed = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    GrantedByUserId = grantedByUserId,
                    Reason = reason,
                    ApplyToChildren = applyToChildren
                });
            }

            await _context.SaveChangesAsync();

            // If applyToChildren is true, create inherited access for all child diseases
            if (applyToChildren)
            {
                await ApplyUserAccessToChildrenAsync(userId, diseaseId, grantedByUserId, expiresAt, reason);
            }
        }

        private async Task ApplyUserAccessToChildrenAsync(string userId, Guid parentDiseaseId, string grantedByUserId, DateTime? expiresAt, string? reason)
        {
            var childDiseaseIds = await GetAllChildDiseaseIdsAsync(parentDiseaseId);
            
            foreach (var childDiseaseId in childDiseaseIds)
            {
                var existingChildAccess = await _context.UserDiseaseAccess
                    .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DiseaseId == childDiseaseId);

                if (existingChildAccess != null)
                {
                    // Update to mark as inherited
                    existingChildAccess.IsAllowed = true;
                    existingChildAccess.InheritedFromDiseaseId = parentDiseaseId;
                    existingChildAccess.CreatedAt = DateTime.UtcNow;
                    existingChildAccess.ExpiresAt = expiresAt;
                    existingChildAccess.GrantedByUserId = grantedByUserId;
                    existingChildAccess.Reason = reason;
                }
                else
                {
                    // Create new inherited access
                    _context.UserDiseaseAccess.Add(new UserDiseaseAccess
                    {
                        UserId = userId,
                        DiseaseId = childDiseaseId,
                        IsAllowed = true,
                        InheritedFromDiseaseId = parentDiseaseId,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        GrantedByUserId = grantedByUserId,
                        Reason = reason
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokeDiseaseAccessFromUserAsync(string userId, Guid diseaseId, bool revokeFromChildren = false)
        {
            var access = await _context.UserDiseaseAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DiseaseId == diseaseId);

            if (access != null)
            {
                _context.UserDiseaseAccess.Remove(access);
                
                // If revoking from children, also remove inherited access
                if (revokeFromChildren || access.ApplyToChildren)
                {
                    await RevokeUserAccessFromChildrenAsync(userId, diseaseId);
                }
                
                await _context.SaveChangesAsync();
            }
        }

        private async Task RevokeUserAccessFromChildrenAsync(string userId, Guid parentDiseaseId)
        {
            var inheritedAccess = await _context.UserDiseaseAccess
                .Where(uda => uda.UserId == userId && uda.InheritedFromDiseaseId == parentDiseaseId)
                .ToListAsync();

            _context.UserDiseaseAccess.RemoveRange(inheritedAccess);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetRolesWithDiseaseAccessAsync(Guid diseaseId)
        {
            return await _context.RoleDiseaseAccess
                .Where(rda => rda.DiseaseId == diseaseId && rda.IsAllowed)
                .Include(rda => rda.Role)
                .Select(rda => rda.Role!.Name!)
                .ToListAsync();
        }

        public async Task<List<string>> GetUsersWithDiseaseAccessAsync(Guid diseaseId)
        {
            return await _context.UserDiseaseAccess
                .Where(uda => uda.DiseaseId == diseaseId && uda.IsAllowed &&
                             (uda.ExpiresAt == null || uda.ExpiresAt > DateTime.UtcNow))
                .Select(uda => uda.UserId)
                .ToListAsync();
        }

        public async Task RemoveExpiredAccessAsync()
        {
            var expiredAccess = await _context.UserDiseaseAccess
                .Where(uda => uda.ExpiresAt != null && uda.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            _context.UserDiseaseAccess.RemoveRange(expiredAccess);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasInheritedAccessAsync(string roleId, Guid diseaseId)
        {
            var access = await _context.RoleDiseaseAccess
                .FirstOrDefaultAsync(rda => rda.RoleId == roleId && rda.DiseaseId == diseaseId);

            return access?.InheritedFromDiseaseId != null;
        }

        public async Task<bool> HasInheritedUserAccessAsync(string userId, Guid diseaseId)
        {
            var access = await _context.UserDiseaseAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DiseaseId == diseaseId);

            return access?.InheritedFromDiseaseId != null;
        }

        public async Task<List<Guid>> GetAllChildDiseaseIdsAsync(Guid parentDiseaseId)
        {
            var childIds = new List<Guid>();
            
            // Get immediate children
            var children = await _context.Diseases
                .Where(d => d.ParentDiseaseId == parentDiseaseId)
                .Select(d => d.Id)
                .ToListAsync();

            childIds.AddRange(children);

            // Recursively get descendants
            foreach (var childId in children)
            {
                var descendants = await GetAllChildDiseaseIdsAsync(childId);
                childIds.AddRange(descendants);
            }

            return childIds;
        }
    }
}
