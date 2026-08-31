namespace LABMEDIS.Core.Models.Entities;

public enum StockMovementType
{
    ReceptionFournisseur = 0,
    MiseEnStock = 1,
    Transfert = 2,
    Vente = 3,
    RetourClient = 4,
    AjustementPositif = 5,
    AjustementNegatif = 6,
    Destruction = 7,
    Perte = 8,
    Echantillon = 9,
    Quarantaine = 10,
    Liberation = 11
}

/// <summary>Polymorphic reference to the document that originated a movement (PO/SaleOrder/Return/InventorySession).</summary>
public enum SourceDocumentType
{
    PurchaseOrder = 0,
    SaleOrder = 1,
    CustomerReturn = 2,
    InventorySession = 3
}

/// <summary>Append-only stock ledger (FR-038) — never updated nor soft-deleted after creation.</summary>
public class StockMovement : AppendOnlyEntity
{
    public Guid StockLotId { get; set; }

    public StockMovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public Guid? SourceLocationId { get; set; }

    public Guid? DestinationLocationId { get; set; }

    public Guid UserId { get; set; }

    public SourceDocumentType? SourceDocumentType { get; set; }

    public Guid? SourceDocumentId { get; set; }

    /// <summary>Required for AjustementPositif/AjustementNegatif (FR-038).</summary>
    public string? Reason { get; set; }
}
