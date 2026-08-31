using LABMEDIS.Core;
using LABMEDIS.Core.Repositories.Currency;
using LABMEDIS.Service.DTOs.Responses;
using CurrencyEntity = LABMEDIS.Core.Models.Entities.Currency;

namespace LABMEDIS.Service.Services;

public class CurrencyService(AppDbContext context) : CurrencyRepository(context), ICurrencyService
{
    private static readonly (string Code, string Name)[] SupportedCurrencies =
    [
        ("EUR", "Euro"),
        ("USD", "Dollar américain"),
        ("XOF", "Franc CFA (UEMOA)")
    ];

    public async Task<IReadOnlyList<LookupResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).Select(c => new LookupResponse(c.Id, c.Code)).ToList();

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (code, name) in SupportedCurrencies)
        {
            if (await GetByCodeAsync(code, cancellationToken) is null)
            {
                await AddAsync(new CurrencyEntity { Code = code, Name = name }, cancellationToken);
            }
        }
    }
}
