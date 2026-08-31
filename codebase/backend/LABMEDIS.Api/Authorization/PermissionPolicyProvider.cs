using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LABMEDIS.Api.Authorization;

/// <summary>
/// Dynamically resolves a policy name (e.g. "Products.Read") to a PermissionRequirement so
/// controllers can write `[Authorize(Policy = "Products.Read")]` directly against the
/// FR-015 "Module.Action" catalogue without pre-registering a policy per permission.
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallbackPolicyProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.Contains('.', StringComparison.Ordinal))
        {
            return fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
