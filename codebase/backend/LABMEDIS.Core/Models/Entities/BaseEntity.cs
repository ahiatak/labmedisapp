namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Base class for all business entities. Soft delete is mandatory (Principle III of the
/// project constitution): physical deletion is forbidden, IsDeleted/DeletedAt are the only
/// supported deletion mechanism.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }
}
