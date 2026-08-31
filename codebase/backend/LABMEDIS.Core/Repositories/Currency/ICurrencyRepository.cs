using LABMEDIS.Core.Repositories.Base;
using CurrencyEntity = LABMEDIS.Core.Models.Entities.Currency;

namespace LABMEDIS.Core.Repositories.Currency;

public interface ICurrencyRepository : IBaseRepository<CurrencyEntity>
{
    Task<CurrencyEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
