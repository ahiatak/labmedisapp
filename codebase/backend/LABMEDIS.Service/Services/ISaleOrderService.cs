using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface ISaleOrderService
{
    Task<SaleOrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleOrderResponse>> ListAsync(string? status, Guid? customerId, CancellationToken cancellationToken = default);

    Task<SaleOrderResponse> CreateAsync(CreateSaleOrderRequest request, Guid createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>Reserves stock for each line via FEFO (FR-055). 409 INSUFFICIENT_STOCK on concurrent conflict (FR-091/SC-013).</summary>
    Task<SaleOrderResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SaleOrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SaleOrderResponse> DeliverAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<InvoiceResponse> InvoiceAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
}
