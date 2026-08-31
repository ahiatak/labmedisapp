using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface ICurrencyService
{
    Task<IReadOnlyList<LookupResponse>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds the 3 supported currencies (EUR, USD, XOF — FR-085) on first startup.</summary>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}
