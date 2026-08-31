using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderResponse>> ListAsync(string? status, Guid? supplierId, CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse> SubmitAsync(Guid id, Guid changedByUserId, CancellationToken cancellationToken = default);

    /// <summary>EnAttenteValidation → Validée. Above the configured threshold, only a Direction caller may validate (FR-023).</summary>
    Task<PurchaseOrderResponse> ValidateAsync(Guid id, Guid changedByUserId, bool callerIsDirection, CancellationToken cancellationToken = default);

    /// <summary>Terminal, irreversible — requires a non-empty reason (FR-024).</summary>
    Task<PurchaseOrderResponse> CancelAsync(Guid id, Guid changedByUserId, CancelPurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderStatusHistoryResponse>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Physical reception (FR-029 à FR-033): creates one StockLot per requested line via
    /// IStockLotService, then marks the order Reçue (all lines fully received) or
    /// PartiellementReçue. Returns the created lots.
    /// </summary>
    Task<IReadOnlyList<StockLotResponse>> ReceiveAsync(
        Guid purchaseOrderId, List<ReceiveLotLineRequest> lines, Guid receivedByUserId, CancellationToken cancellationToken = default);
}
