using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using ReorderSuggestionEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestion;
using ReorderSuggestionStatusEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestionStatus;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;
using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;
using StockMovementTypeEntity = LABMEDIS.Core.Models.Entities.StockMovementType;

namespace LABMEDIS.Core.Repositories.Forecast;

public class ForecastRepository(AppDbContext context) : BaseRepository<ReorderSuggestionEntity>(context), IForecastRepository
{
    public async Task<IReadOnlyList<ReorderSuggestionEntity>> SearchSuggestionsAsync(ReorderSuggestionStatusEntity? status, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(s => s.Product).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.OrderByDescending(s => s.SuggestionDate).ToListAsync(cancellationToken);
    }

    public async Task<int> GetConsumptionOverWindowAsync(Guid productId, int windowDays, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-windowDays);
        return await Context.Set<StockMovementEntity>()
            .Where(m => m.MovementType == StockMovementTypeEntity.Vente && m.CreatedAt >= since
                        && Context.Set<StockLotEntity>().Any(l => l.Id == m.StockLotId && l.ProductId == productId))
            .SumAsync(m => m.Quantity, cancellationToken);
    }
}
