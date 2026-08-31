using LABMEDIS.Core.Repositories.Base;
using PurchaseOrderEntity = LABMEDIS.Core.Models.Entities.PurchaseOrder;
using PurchaseOrderStatusEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderStatus;
using PurchaseOrderStatusHistoryEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderStatusHistory;

namespace LABMEDIS.Core.Repositories.PurchaseOrder;

public interface IPurchaseOrderRepository : IBaseRepository<PurchaseOrderEntity>
{
    Task<PurchaseOrderEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderEntity>> SearchAsync(PurchaseOrderStatusEntity? status, Guid? supplierId, CancellationToken cancellationToken = default);

    /// <summary>Next sequence number for today's PO-AAAAMMJJ-NNNN order number (FR-020).</summary>
    Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default);

    Task AddStatusHistoryAsync(PurchaseOrderStatusHistoryEntity entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderStatusHistoryEntity>> GetStatusHistoryAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
}
