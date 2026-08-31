using LABMEDIS.Core.Repositories.Base;
using InvoiceEntity = LABMEDIS.Core.Models.Entities.Invoice;

namespace LABMEDIS.Core.Repositories.Invoice;

public interface IInvoiceRepository : IBaseRepository<InvoiceEntity>
{
    Task<InvoiceEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceEntity?> GetBySaleOrderIdAsync(Guid saleOrderId, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceEntity>> GetUnpaidByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
