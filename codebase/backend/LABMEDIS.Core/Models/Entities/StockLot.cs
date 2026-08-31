using System.ComponentModel.DataAnnotations.Schema;

namespace LABMEDIS.Core.Models.Entities;

/// <summary>State machine of FR-040 — only Libéré is sellable (FR-041); quarantine/rejection require a reason (FR-042).</summary>
public enum QualityStatus
{
    EnReception = 0,
    EnQuarantaine = 1,
    EnAttenteLiberation = 2,
    Libere = 3,
    NonConforme = 4,
    Detruit = 5,
    Perime = 6,
    SuspecteFalsifie = 7
}

/// <summary>
/// Central traceability entity (US4/US5 — FR-029 à FR-044). UnitCostCfa (PRU) and
/// InitialQuantity are immutable once set (FR-032). RemainingQuantity/ReservedQuantity are
/// enforced by CHECK constraints in the migration as a concurrency safety net
/// (research.md §5) in addition to the `SELECT ... FOR UPDATE` lock taken by
/// IStockLotRepository.GetFefoCandidatesAsync.
/// </summary>
public class StockLot : BaseEntity
{
    public Guid ProductId { get; set; }

    public Guid? ShipmentId { get; set; }

    /// <summary>Unique per (supplier, product) couple — RG-006. Supplier resolved via Shipment→PurchaseOrderLine→PurchaseOrder.</summary>
    public string SupplierLotNumber { get; set; } = string.Empty;

    /// <summary>Unique globally, format {code_produit}-{AAAAMMJJ}-{NNN}.</summary>
    public string InternalLotNumber { get; set; } = string.Empty;

    public DateOnly ReceptionDate { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public DateOnly ExpiryDate { get; set; }

    /// <summary>Immutable after creation.</summary>
    public int InitialQuantity { get; set; }

    public int RemainingQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    /// <summary>Prix de revient unitaire — figé à réception, jamais recalculé (FR-032).</summary>
    public decimal UnitCostCfa { get; set; }

    public Guid? PricingProfileId { get; set; }

    public QualityStatus QualityStatus { get; set; } = QualityStatus.EnReception;

    /// <summary>Required when QualityStatus is EnQuarantaine/NonConforme (FR-042).</summary>
    public string? QuarantineReason { get; set; }

    public Guid? ReleasedByUserId { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public Guid ReceivedByUserId { get; set; }

    public Product? Product { get; set; }

    public List<StockLotLocation> Locations { get; set; } = [];

    [NotMapped]
    public int AvailableQuantity => RemainingQuantity - ReservedQuantity;
}
