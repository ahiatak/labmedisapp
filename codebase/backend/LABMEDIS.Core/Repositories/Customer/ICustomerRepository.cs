using LABMEDIS.Core.Repositories.Base;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;

namespace LABMEDIS.Core.Repositories.Customer;

public interface ICustomerRepository : IBaseRepository<CustomerEntity>
{
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<CustomerEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerEntity>> SearchAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Outstanding balance = sum of unpaid invoices (FR-009). Invoices are introduced by
    /// US7 — until then this returns 0, the real aggregation is wired once InvoiceRepository
    /// exists (see LABMEDIS.Service.Services.CustomerService).
    /// </summary>
    Task<decimal> GetOutstandingBalanceAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>True when a negotiated price period for (customerId, productId) overlaps [validFrom, validTo].</summary>
    Task<bool> HasOverlappingNegotiatedPriceAsync(
        Guid customerId, Guid productId, DateOnly validFrom, DateOnly validTo, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
