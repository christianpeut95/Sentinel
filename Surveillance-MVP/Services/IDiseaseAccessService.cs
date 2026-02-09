namespace Surveillance_MVP.Services
{
    public interface IDiseaseAccessService
    {
        /// <summary>
        /// Checks if a user can access a specific disease
        /// </summary>
        Task<bool> CanAccessDiseaseAsync(string userId, Guid diseaseId);
        
        /// <summary>
        /// Gets all disease IDs that a user can access
        /// </summary>
        Task<List<Guid>> GetAccessibleDiseaseIdsAsync(string userId);
        
        /// <summary>
        /// Grants disease access to a role
        /// </summary>
        Task GrantDiseaseAccessToRoleAsync(string roleId, Guid diseaseId, string grantedByUserId, bool applyToChildren = false);
        
        /// <summary>
        /// Revokes disease access from a role
        /// </summary>
        Task RevokeDiseaseAccessFromRoleAsync(string roleId, Guid diseaseId, bool revokeFromChildren = false);
        
        /// <summary>
        /// Grants disease access to a user (with optional expiration)
        /// </summary>
        Task GrantDiseaseAccessToUserAsync(string userId, Guid diseaseId, string grantedByUserId, DateTime? expiresAt = null, string? reason = null, bool applyToChildren = false);
        
        /// <summary>
        /// Revokes disease access from a user
        /// </summary>
        Task RevokeDiseaseAccessFromUserAsync(string userId, Guid diseaseId, bool revokeFromChildren = false);
        
        /// <summary>
        /// Gets all roles that have access to a disease
        /// </summary>
        Task<List<string>> GetRolesWithDiseaseAccessAsync(Guid diseaseId);
        
        /// <summary>
        /// Gets all users that have access to a disease
        /// </summary>
        Task<List<string>> GetUsersWithDiseaseAccessAsync(Guid diseaseId);
        
        /// <summary>
        /// Removes expired user disease access records
        /// </summary>
        Task RemoveExpiredAccessAsync();
        
        /// <summary>
        /// Checks if a disease has inherited access (cannot grant directly)
        /// </summary>
        Task<bool> HasInheritedAccessAsync(string roleId, Guid diseaseId);
        
        /// <summary>
        /// Checks if a user disease has inherited access (cannot grant directly)
        /// </summary>
        Task<bool> HasInheritedUserAccessAsync(string userId, Guid diseaseId);
        
        /// <summary>
        /// Gets all child disease IDs recursively
        /// </summary>
        Task<List<Guid>> GetAllChildDiseaseIdsAsync(Guid parentDiseaseId);
    }
}
