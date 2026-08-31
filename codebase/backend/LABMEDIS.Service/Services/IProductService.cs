using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductResponse> Items, int TotalCount)> ListAsync(
        string? search, bool selectableOnly, int page, int pageSize, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Bulk catalogue import from an Excel (.xlsx) stream — FR-006. Never throws on row errors: they are collected in the response.</summary>
    Task<ProductImportResponse> ImportAsync(Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>Adds a packaging unit-conversion row (e.g. 1 carton = 100 units) — consumed as PurchaseOrderLine.PackagingId (US3).</summary>
    Task<ProductPackagingResponse> AddPackagingAsync(Guid productId, CreateProductPackagingRequest request, CancellationToken cancellationToken = default);
}
