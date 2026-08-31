namespace LABMEDIS.Core.Models.Entities;

/// <summary>Controlled therapeutic class list — no free text allowed on Product (FR-003).</summary>
public class TherapeuticClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
