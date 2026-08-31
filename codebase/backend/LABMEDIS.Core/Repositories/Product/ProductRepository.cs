using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;

namespace LABMEDIS.Core.Repositories.Product;

public class ProductRepository(AppDbContext context) : BaseRepository<ProductEntity>(context), IProductRepository
{
    public Task<bool> DesignationExistsAsync(string designation, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(p => p.Designation == designation && (excludeId == null || p.Id != excludeId), cancellationToken);

    public Task<bool> CodeCipExistsAsync(string codeCip, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(p => p.CodeCip == codeCip && (excludeId == null || p.Id != excludeId), cancellationToken);

    public Task<ProductEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(p => p.Category)
            .Include(p => p.TherapeuticClass)
            .Include(p => p.PharmaceuticalFormEntity)
            .Include(p => p.Packagings)
            .Include(p => p.ProductSuppliers)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ProductEntity> Items, int TotalCount)> SearchAsync(
        string? search, bool selectableOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(p => p.Category).AsQueryable();

        if (selectableOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Designation, $"%{search}%") ||
                (p.CodeCip != null && EF.Functions.ILike(p.CodeCip, $"%{search}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Designation)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
