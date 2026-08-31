using LABMEDIS.Core.Repositories.Base;
using SupplierEntity = LABMEDIS.Core.Models.Entities.Supplier;

namespace LABMEDIS.Core.Repositories.Supplier;

public interface ISupplierRepository : IBaseRepository<SupplierEntity>
{
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<SupplierEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierEntity>> SearchAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);
}
