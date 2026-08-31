using LABMEDIS.Core.Repositories.Base;
using SaleOrderEntity = LABMEDIS.Core.Models.Entities.SaleOrder;
using SaleOrderStatusEntity = LABMEDIS.Core.Models.Entities.SaleOrderStatus;

namespace LABMEDIS.Core.Repositories.SaleOrder;

public interface ISaleOrderRepository : IBaseRepository<SaleOrderEntity>
{
    Task<SaleOrderEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleOrderEntity>> SearchAsync(SaleOrderStatusEntity? status, Guid? customerId, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default);
}
