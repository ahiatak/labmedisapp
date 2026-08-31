using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerResponse>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OutstandingBalanceResponse> GetOutstandingBalanceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NegotiatedPriceResponse>> GetNegotiatedPricesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NegotiatedPriceResponse> AddNegotiatedPriceAsync(Guid id, NegotiatedPriceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws if the customer is inactive (FR-010), or — when the company's credit-limit
    /// enforcement mode is Block — if the outstanding balance already exceeds the credit
    /// limit (FR-009). Called by SaleOrderService before confirming a new sale order (US7).
    /// </summary>
    Task EnsureCanOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
