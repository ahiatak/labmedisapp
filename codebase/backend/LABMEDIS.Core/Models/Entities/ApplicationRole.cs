using Microsoft.AspNetCore.Identity;

namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// LABMEDIS role (Admin, Direction, ResponsableAchats, Logistique, Magasinier,
/// ResponsableQualite, Commercial, Comptable, Preparateur, LectureSeule — FR-015).
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>System roles (the 10 built-in LABMEDIS roles) cannot be deleted from the UI.</summary>
    public bool IsSystem { get; set; }
}
