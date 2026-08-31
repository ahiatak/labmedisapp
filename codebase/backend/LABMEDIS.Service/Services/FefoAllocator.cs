using LABMEDIS.Core.Models.Entities;

namespace LABMEDIS.Service.Services;

public enum FefoAllocationOutcome
{
    Success,
    NoAvailableLot,
    InsufficientStock
}

public record FefoAllocationLine(Guid LotId, DateOnly ExpiryDate, int QuantityAllocated);

public record FefoAllocationResult(FefoAllocationOutcome Outcome, IReadOnlyList<FefoAllocationLine> Lines, int TotalAvailable);

/// <summary>
/// Pure FEFO selection/allocation algorithm (RG-001, FR-036) — deliberately framework-free
/// (no AppException, no EF Core) so it can be exercised by a fast, DB-independent unit test
/// (T079, constitution §Qualité — blocking). StockLotService.GetFefoSuggestionAsync fetches
/// the row-locked candidates from PostgreSQL (research.md §5) and hands them here for the
/// actual selection logic.
/// </summary>
public static class FefoAllocator
{
    /// <summary>
    /// Only "Libéré" lots with available quantity > 0 are eligible — expired (Périmé),
    /// quarantined, non-conforme and destroyed lots are excluded by construction since none
    /// of them carry QualityStatus.Libere.
    /// </summary>
    public static FefoAllocationResult Allocate(IEnumerable<StockLot> lots, int requestedQuantity)
    {
        var eligible = lots
            .Where(l => l.QualityStatus == QualityStatus.Libere && l.AvailableQuantity > 0)
            .OrderBy(l => l.ExpiryDate)
            .ToList();

        if (eligible.Count == 0)
        {
            return new FefoAllocationResult(FefoAllocationOutcome.NoAvailableLot, [], 0);
        }

        var totalAvailable = eligible.Sum(l => l.AvailableQuantity);
        if (totalAvailable < requestedQuantity)
        {
            return new FefoAllocationResult(FefoAllocationOutcome.InsufficientStock, [], totalAvailable);
        }

        var lines = new List<FefoAllocationLine>();
        var remaining = requestedQuantity;
        foreach (var lot in eligible)
        {
            if (remaining <= 0)
            {
                break;
            }

            var allocated = Math.Min(remaining, lot.AvailableQuantity);
            lines.Add(new FefoAllocationLine(lot.Id, lot.ExpiryDate, allocated));
            remaining -= allocated;
        }

        return new FefoAllocationResult(FefoAllocationOutcome.Success, lines, totalAvailable);
    }
}
