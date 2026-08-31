using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using ExchangeRateEntity = LABMEDIS.Core.Models.Entities.ExchangeRate;

namespace LABMEDIS.Core.Repositories.ExchangeRate;

public class ExchangeRateRepository(AppDbContext context)
    : BaseRepository<ExchangeRateEntity>(context), IExchangeRateRepository
{
    public Task<ExchangeRateEntity?> GetApplicableRateAsync(
        Guid currencyFromId, Guid currencyToId, DateOnly asOfDate, CancellationToken cancellationToken = default) =>
        DbSet
            .Where(r => r.CurrencyFromId == currencyFromId
                        && r.CurrencyToId == currencyToId
                        && r.EffectiveDate <= asOfDate)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
}
