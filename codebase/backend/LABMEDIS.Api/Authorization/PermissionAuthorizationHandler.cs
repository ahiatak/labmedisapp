using Microsoft.AspNetCore.Authorization;

namespace LABMEDIS.Api.Authorization;

/// <summary>Grants a PermissionRequirement when the user is in role "Admin" (implicit full access) or carries the matching "permission" claim.</summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.HasClaim("permission", requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
