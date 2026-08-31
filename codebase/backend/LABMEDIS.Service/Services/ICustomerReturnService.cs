using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface ICustomerReturnService
{
    Task<CustomerReturnResponse> CreateAsync(Guid saleOrderId, CreateReturnRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<CustomerReturnResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerReturnResponse>> ListBySaleOrderAsync(Guid saleOrderId, CancellationToken cancellationToken = default);
}
