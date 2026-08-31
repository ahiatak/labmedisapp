using Microsoft.AspNetCore.Identity;

namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// LABMEDIS user account. Extends ASP.NET Core Identity's user with the fields required by
/// FR-012 to FR-019 (spec.md). Failed-attempt lockout (FR-014: 5 tentatives / 15 minutes)
/// relies on Identity's built-in AccessFailedCount/LockoutEnd rather than a duplicate field.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginDate { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
