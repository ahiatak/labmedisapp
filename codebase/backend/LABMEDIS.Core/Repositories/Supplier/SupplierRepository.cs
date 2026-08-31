using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using SupplierEntity = LABMEDIS.Core.Models.Entities.Supplier;

namespace LABMEDIS.Core.Repositories.Supplier;

public class SupplierRepository(AppDbContext context) : BaseRepository<SupplierEntity>(context), ISupplierRepository
{
    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(s => s.IsActive && s.Name == name && (excludeId == null || s.Id != excludeId), cancellationToken);

    public Task<SupplierEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(s => s.DefaultCurrency).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SupplierEntity>> SearchAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }
}
