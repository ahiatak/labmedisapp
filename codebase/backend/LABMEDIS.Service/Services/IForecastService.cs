using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IForecastService
{
    Task<IReadOnlyList<ReorderSuggestionResponse>> GetSuggestionsAsync(string? status, CancellationToken cancellationToken = default);

    /// <summary>Delegates to IPurchaseOrderService.CreateAsync with a pre-filled Brouillon order (FR-065).</summary>
    Task<PurchaseOrderResponse> ConvertAsync(Guid suggestionId, Guid userId, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default);

    Task<ForecastParametersResponse> GetParametersAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ForecastParametersResponse> UpdateParametersAsync(Guid productId, ForecastParametersRequest request, CancellationToken cancellationToken = default);

    /// <summary>Daily MRP sweep (FR-063/FR-064) — also triggerable manually (POST /api/forecast/run). Returns the number of suggestions created.</summary>
    Task<int> RunCalculationAsync(CancellationToken cancellationToken = default);
}
