using LABMEDIS.Service.Services;

namespace LABMEDIS.Tests.Unit;

/// <summary>T103 — bloquant (constitution §Qualité). Calcul CUMP/PMP pondéré multi-lots (FR-033).</summary>
public class CumpCalculationTests
{
    [Fact]
    public void Calculate_SingleLot_ReturnsItsUnitCost()
    {
        var result = CumpCalculator.Calculate([(100, 500m)]);
        Assert.Equal(500m, result);
    }

    [Fact]
    public void Calculate_TwoLotsEqualQuantities_ReturnsSimpleAverage()
    {
        var result = CumpCalculator.Calculate([(50, 400m), (50, 600m)]);
        Assert.Equal(500m, result);
    }

    [Fact]
    public void Calculate_TwoLotsDifferentQuantities_WeightsByQuantity()
    {
        // (100×400 + 300×600) / 400 = 550
        var result = CumpCalculator.Calculate([(100, 400m), (300, 600m)]);
        Assert.Equal(550m, result);
    }

    [Fact]
    public void Calculate_NoLots_ReturnsZero()
    {
        var result = CumpCalculator.Calculate([]);
        Assert.Equal(0m, result);
    }
}
