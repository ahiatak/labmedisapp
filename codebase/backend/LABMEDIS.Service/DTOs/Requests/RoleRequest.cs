namespace LABMEDIS.Service.DTOs.Requests;

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<Guid> PermissionIds { get; set; } = [];
}

public class UpdateRolePermissionsRequest
{
    public List<Guid> PermissionIds { get; set; } = [];
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}

public class UpdateUserPermissionExceptionsRequest
{
    public List<UserPermissionExceptionItem> Exceptions { get; set; } = [];
}

public class UserPermissionExceptionItem
{
    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; }

    public string? Reason { get; set; }
}
