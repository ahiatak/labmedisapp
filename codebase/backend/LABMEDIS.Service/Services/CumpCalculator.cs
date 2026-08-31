namespace LABMEDIS.Service.Services;

/// <summary>
/// Pure weighted-average cost (PMP/CUMP) calculation (FR-033) — framework-free so it can be
/// exercised by a fast unit test (T103, constitution §Qualité — blocking).
/// </summary>
public static class CumpCalculator
{
    public static decimal Calculate(IEnumerable<(int Quantity, decimal UnitCost)> lots)
    {
        var lotList = lots.ToList();
        var totalQuantity = lotList.Sum(l => l.Quantity);
        return totalQuantity == 0 ? 0m : lotList.Sum(l => l.Quantity * l.UnitCost) / totalQuantity;
    }
}
