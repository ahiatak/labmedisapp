using Microsoft.AspNetCore.Authorization;

namespace LABMEDIS.Api.Authorization;

/// <summary>Authorization requirement satisfied when the caller's JWT carries a "permission" claim equal to <see cref="PermissionCode"/> (FR-015/FR-016).</summary>
public class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
