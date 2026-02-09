using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if the policy name matches our permission pattern: "Permission.Module.Action"
            if (policyName.StartsWith("Permission."))
            {
                var parts = policyName.Split('.');
                if (parts.Length == 3)
                {
                    if (Enum.TryParse<PermissionModule>(parts[1], out var module) &&
                        Enum.TryParse<PermissionAction>(parts[2], out var action))
                    {
                        var policy = new AuthorizationPolicyBuilder()
                            .AddRequirements(new PermissionRequirement(module, action))
                            .Build();

                        return Task.FromResult<AuthorizationPolicy?>(policy);
                    }
                }
            }

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
