using ApplicationUserEntity = LABMEDIS.Core.Models.Entities.ApplicationUser;

namespace LABMEDIS.Service.DTOs.Responses;

public class UserResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public UserResponse()
    {
    }

    public UserResponse(ApplicationUserEntity entity, IReadOnlyList<string> roles)
    {
        Id = entity.Id;
        Email = entity.Email ?? string.Empty;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsActive = entity.IsActive;
        LastLoginDate = entity.LastLoginDate;
        Roles = roles;
    }
}
