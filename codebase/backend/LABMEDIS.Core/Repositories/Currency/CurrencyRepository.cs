using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using CurrencyEntity = LABMEDIS.Core.Models.Entities.Currency;

namespace LABMEDIS.Core.Repositories.Currency;

public class CurrencyRepository(AppDbContext context) : BaseRepository<CurrencyEntity>(context), ICurrencyRepository
{
    public Task<CurrencyEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
}
