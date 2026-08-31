using LABMEDIS.Core.Repositories.Base;
using ReorderSuggestionEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestion;
using ReorderSuggestionStatusEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestionStatus;

namespace LABMEDIS.Core.Repositories.Forecast;

public interface IForecastRepository : IBaseRepository<ReorderSuggestionEntity>
{
    Task<IReadOnlyList<ReorderSuggestionEntity>> SearchSuggestionsAsync(ReorderSuggestionStatusEntity? status, CancellationToken cancellationToken = default);

    /// <summary>Total units sold (StockMovement.Vente) for a product over the last <paramref name="windowDays"/> days — the FR-063 rolling consumption window.</summary>
    Task<int> GetConsumptionOverWindowAsync(Guid productId, int windowDays, CancellationToken cancellationToken = default);
}
