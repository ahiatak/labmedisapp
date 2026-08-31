namespace LABMEDIS.Core.Models.Entities;

/// <summary>Controlled pharmaceutical form list — no free text allowed on Product (FR-003).</summary>
public class PharmaceuticalForm : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
