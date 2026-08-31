namespace LABMEDIS.Core.Models.Entities;

/// <summary>State machine of FR-022 — every transition is recorded in PurchaseOrderStatusHistory.</summary>
public enum PurchaseOrderStatus
{
    Brouillon = 0,
    EnAttenteValidation = 1,
    Validee = 2,
    Envoyee = 3,
    EnFabrication = 4,
    PreteAExpedier = 5,
    Expediee = 6,
    EnTransit = 7,
    PartiellementRecue = 8,
    Recue = 9,
    Close = 10,
    Annulee = 11
}

/// <summary>Purchase order (US3 — FR-020 à FR-024). Exchange rate is locked at creation and never recalculated (FR-021, RG-003).</summary>
public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid CurrencyId { get; set; }

    /// <summary>The ExchangeRate row locked at creation time — never recalculated, including at reception (FR-021).</summary>
    public Guid LockedExchangeRateId { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Brouillon;

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public string? Incoterm { get; set; }

    public TransportMode TransportMode { get; set; }

    /// <summary>Required when Status = Annulée (FR-024) — cancellation is terminal and irreversible.</summary>
    public string? CancellationReason { get; set; }

    public Guid? ValidatedByUserId { get; set; }

    public DateTime? ValidatedAt { get; set; }

    public Supplier? Supplier { get; set; }

    public Currency? Currency { get; set; }

    public ExchangeRate? LockedExchangeRate { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = [];
}
