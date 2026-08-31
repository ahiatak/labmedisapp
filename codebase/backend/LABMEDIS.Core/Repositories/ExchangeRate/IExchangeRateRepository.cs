using LABMEDIS.Core.Repositories.Base;
using ExchangeRateEntity = LABMEDIS.Core.Models.Entities.ExchangeRate;

namespace LABMEDIS.Core.Repositories.ExchangeRate;

public interface IExchangeRateRepository : IBaseRepository<ExchangeRateEntity>
{
    /// <summary>
    /// Returns the exchange rate applicable on <paramref name="asOfDate"/> for the given currency
    /// pair (most recent row whose EffectiveDate is on or before that date), or null if none
    /// exists — callers must treat that as the EXCHANGE_RATE_MISSING error case (RG-004).
    /// </summary>
    Task<ExchangeRateEntity?> GetApplicableRateAsync(
        Guid currencyFromId, Guid currencyToId, DateOnly asOfDate, CancellationToken cancellationToken = default);
}
