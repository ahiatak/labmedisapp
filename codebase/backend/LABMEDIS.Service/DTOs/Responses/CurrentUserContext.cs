namespace LABMEDIS.Service.DTOs.Responses;

/// <summary>
/// Identity of the caller. The Id/FirstName/LastName/UserName fields feed the mandatory log
/// line of Principle IV/VII ("{LastName} {FirstName} ({UserName}) | ..."); Roles/Permissions
/// are populated by GET /api/auth/me and the login response (US2, FR-019) to drive the
/// frontend's ProtectedRoute/PermissionGate.
/// </summary>
public class CurrentUserContext
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];

    public IReadOnlyList<string> Permissions { get; set; } = [];
}
