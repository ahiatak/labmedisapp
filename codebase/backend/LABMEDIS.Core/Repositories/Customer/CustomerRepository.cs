using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;

namespace LABMEDIS.Core.Repositories.Customer;

public class CustomerRepository(AppDbContext context) : BaseRepository<CustomerEntity>(context), ICustomerRepository
{
    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(c => c.IsActive && c.Name == name && (excludeId == null || c.Id != excludeId), cancellationToken);

    public Task<CustomerEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(c => c.NegotiatedPrices).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerEntity>> SearchAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));
        }

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public Task<decimal> GetOutstandingBalanceAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        Context.Set<Models.Entities.Invoice>()
            .Where(i => i.CustomerId == customerId && (i.Status == Models.Entities.InvoiceStatus.Emise || i.Status == Models.Entities.InvoiceStatus.EnRetard))
            .SumAsync(i => i.TotalTtc, cancellationToken);

    public Task<bool> HasOverlappingNegotiatedPriceAsync(
        Guid customerId, Guid productId, DateOnly validFrom, DateOnly validTo, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        Context.Set<Models.Entities.CustomerProductPrice>().AnyAsync(p =>
            p.CustomerId == customerId
            && p.ProductId == productId
            && (excludeId == null || p.Id != excludeId)
            && p.ValidFrom <= validTo
            && p.ValidTo >= validFrom,
            cancellationToken);
}
