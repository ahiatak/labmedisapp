using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IStockMovementService
{
    /// <summary>Free-form movement (transfer/adjustment) — reason required for adjustments (FR-038).</summary>
    Task<StockMovementResponse> RecordAsync(RecordMovementRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementResponse>> GetByStockLotIdAsync(Guid stockLotId, CancellationToken cancellationToken = default);
}
