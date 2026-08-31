using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierResponse>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
