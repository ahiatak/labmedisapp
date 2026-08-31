namespace LABMEDIS.Core.Models.Entities;

public enum InventorySessionStatus
{
    Ouverte = 0,
    Gelee = 1,
    EnComptage = 2,
    Validee = 3,
    Cloturee = 4
}

/// <summary>Physical inventory session over a perimeter (US9 — FR-044). Freezes stock movements for its scope while open.</summary>
public class InventorySession : BaseEntity
{
    public string SessionNumber { get; set; } = string.Empty;

    /// <summary>Warehouse/zone/location scope — matched against StorageLocation.Code (exact or prefix).</summary>
    public string Perimeter { get; set; } = string.Empty;

    public InventorySessionStatus Status { get; set; } = InventorySessionStatus.Ouverte;

    public DateTime? FrozenAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public List<InventoryCount> Counts { get; set; } = [];
}
