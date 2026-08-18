using System.Security.Claims;
using Sentinel.Services;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Middleware that loads accessible disease IDs for the current user and caches them
    /// in HttpContext.Items for use by the global query filter.
    /// </summary>
    public class DiseaseAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiseaseAccessMiddleware> _logger;

        public DiseaseAccessMiddleware(
            RequestDelegate next,
            ILogger<DiseaseAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IDiseaseAccessService diseaseAccessService,
            ISystemSettingsService systemSettingsService)
        {
            // Load the installation-wide patient visibility policy once for the request.
            // If the settings store cannot be read, use the more restrictive policy.
            try
            {
                var settings = await systemSettingsService.GetSettingsAsync();
                context.Items["CaseScopedPatientAccess"] = settings?.CaseScopedPatientAccess ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the patient visibility policy");
                context.Items["CaseScopedPatientAccess"] = true;
            }

            // Only load disease access for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // An authenticated request must fail closed if its effective disease
                // access cannot be determined. The global query filters use this item
                // as their shared visibility boundary.
                context.Items["AccessibleDiseaseIds"] = new List<Guid>();

                try
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Load accessible disease IDs for this user
                        var accessibleDiseases = await diseaseAccessService
                            .GetAccessibleDiseaseIdsAsync(userId);
                        
                        // Cache in HttpContext.Items for this request lifetime
                        context.Items["AccessibleDiseaseIds"] = accessibleDiseases;
                        
                        _logger.LogDebug(
                            "Loaded {Count} accessible diseases for user {UserId}", 
                            accessibleDiseases.Count, 
                            userId);
                    }
                    else
                    {
                        _logger.LogWarning("Authenticated request has no user identifier claim");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, 
                        "Error loading disease access for user {UserId}", 
                        context.User.FindFirstValue(ClaimTypes.NameIdentifier));
                    
                    // Don't block the request - let it continue with empty access
                    // The query filter will handle this by showing no restricted diseases
                    context.Items["AccessibleDiseaseIds"] = new List<Guid>();
                }
            }
            else
            {
                // For unauthenticated users, they can only see public diseases
                // The query filter will handle this automatically
                _logger.LogDebug("Unauthenticated request - will show public diseases only");
            }

            await _next(context);
        }
    }
}
