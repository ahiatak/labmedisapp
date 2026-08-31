using LABMEDIS.Core.Repositories.Base;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;

namespace LABMEDIS.Core.Repositories.Product;

public interface IProductRepository : IBaseRepository<ProductEntity>
{
    /// <summary>True if an active product with this designation exists (excluding <paramref name="excludeId"/>).</summary>
    Task<bool> DesignationExistsAsync(string designation, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>True if an active product with this CIP code exists (excluding <paramref name="excludeId"/>).</summary>
    Task<bool> CodeCipExistsAsync(string codeCip, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<ProductEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated/filtered catalogue listing. When <paramref name="selectableOnly"/> is true,
    /// inactive products are excluded from the result (FR-005).
    /// </summary>
    Task<(IReadOnlyList<ProductEntity> Items, int TotalCount)> SearchAsync(
        string? search, bool selectableOnly, int page, int pageSize, CancellationToken cancellationToken = default);
}
