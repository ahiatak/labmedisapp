using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IStockLotService
{
    Task<StockLotResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Not documented in contracts/stock.md (which assumes lookup by id) but needed for the Warehouse/Reception pages to list existing lots.</summary>
    Task<IReadOnlyList<StockLotResponse>> ListAsync(Guid? productId, CancellationToken cancellationToken = default);

    /// <summary>Current PMP/CUMP across "Libéré" lots (FR-033) — consumed by PricingService when a price is applied.</summary>
    Task<decimal> GetWeightedAverageCostAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<AvailableStockResponse> GetAvailableAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Ordered FEFO candidates for a requested quantity (FR-036). Throws 422 NO_AVAILABLE_LOT / INSUFFICIENT_STOCK.</summary>
    Task<FefoSuggestionResponse> GetFefoSuggestionAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Manual allocation departing from the FEFO order — requires a non-empty reason (FR-037).</summary>
    Task<StockLotResponse> AllocateAsync(AllocateLotRequest request, CancellationToken cancellationToken = default);

    /// <summary>Standard FEFO reservation (no derogation) — used by SaleOrderService.ConfirmAsync (FR-055).</summary>
    Task<StockLotResponse> ReserveAsync(Guid lotId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Releases a previously reserved quantity — used by SaleOrderService.CancelAsync (FR-055) and delivery (converts reservation into a physical decrement).</summary>
    Task ReleaseReservationAsync(Guid lotId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Converts a reservation into a physical stock decrement at delivery time (FR-057).</summary>
    Task DeliverAsync(Guid lotId, int quantity, Guid userId, Guid saleOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new traceable lot for a returned quantity (US8 — FR-061): "RemiseEnStock" →
    /// Libéré and immediately sellable; "Quarantaine" → EnQuarantaine pending a quality
    /// decision; "Destruction" → recorded as Détruit with zero remaining quantity (paper
    /// trail only, never added to sellable stock). The original lot's own quantities are
    /// untouched — the return is its own traceable unit, carrying forward the frozen PRU.
    /// </summary>
    Task<StockLotResponse> CreateFromReturnAsync(
        Guid originalLotId, int quantity, string disposition, string? quarantineReason, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives one purchase-order line into a new StockLot: enforces lot-number uniqueness
    /// (FR-030), the FR-031 expiry block, computes and freezes the PRU (FR-032), and records
    /// the ReceptionFournisseur movement. Called by PurchaseOrderService.ReceiveAsync per line.
    /// </summary>
    Task<StockLotResponse> ReceiveLineAsync(
        Guid purchaseOrderLineId, decimal lockedExchangeRate, Guid receivedByUserId,
        ReceiveLotLineRequest request, CancellationToken cancellationToken = default);

    Task<StockLotResponse> QuarantineAsync(Guid id, Guid userId, QuarantineLotRequest request, CancellationToken cancellationToken = default);

    Task<StockLotResponse> ReleaseAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<StockLotResponse> MarkNonConformeAsync(Guid id, Guid userId, RejectLotRequest request, CancellationToken cancellationToken = default);

    Task<StockLotResponse> DestroyAsync(Guid id, Guid userId, DestroyLotRequest request, CancellationToken cancellationToken = default);

    Task<StockLotResponse> SuspectFalsifiedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Daily sweep (ExpiryAlertJob, FR-043/FR-076): auto-transitions expired "Libéré" lots to
    /// "Périmé" and counts lots inside their category's alert window. Returns (expiredCount, alertCount).
    /// </summary>
    Task<(int ExpiredCount, int AlertCount)> ProcessExpiryAlertsAsync(CancellationToken cancellationToken = default);
}
