using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.Services;

namespace LABMEDIS.Tests.Unit;

/// <summary>
/// T079 — bloquant (constitution §Qualité). Vérifie la sélection FEFO (RG-001/FR-036) :
/// ordre par péremption la plus proche, exclusion des lots périmés/non-libérés,
/// insuffisance de stock. Pure/DB-independent — exerce directement FefoAllocator.
/// </summary>
public class FefoAllocationTests
{
    private static StockLot MakeLot(DateOnly expiry, int remaining, int reserved = 0, QualityStatus status = QualityStatus.Libere) =>
        new()
        {
            ExpiryDate = expiry,
            InitialQuantity = remaining,
            RemainingQuantity = remaining,
            ReservedQuantity = reserved,
            QualityStatus = status
        };

    [Fact]
    public void Allocate_MultipleLibereLots_PicksNearestExpiryFirst()
    {
        var farLot = MakeLot(new DateOnly(2027, 6, 1), 50);
        var nearLot = MakeLot(new DateOnly(2026, 12, 1), 50);
        var lots = new[] { farLot, nearLot };

        var result = FefoAllocator.Allocate(lots, 30);

        Assert.Equal(FefoAllocationOutcome.Success, result.Outcome);
        var line = Assert.Single(result.Lines);
        Assert.Equal(nearLot.Id, line.LotId);
        Assert.Equal(30, line.QuantityAllocated);
    }

    [Fact]
    public void Allocate_QuantityExceedingNearestLot_SpillsIntoNextLotByExpiryOrder()
    {
        var nearLot = MakeLot(new DateOnly(2026, 12, 1), 20);
        var farLot = MakeLot(new DateOnly(2027, 6, 1), 50);

        var result = FefoAllocator.Allocate([farLot, nearLot], 30);

        Assert.Equal(FefoAllocationOutcome.Success, result.Outcome);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(nearLot.Id, result.Lines[0].LotId);
        Assert.Equal(20, result.Lines[0].QuantityAllocated);
        Assert.Equal(farLot.Id, result.Lines[1].LotId);
        Assert.Equal(10, result.Lines[1].QuantityAllocated);
    }

    [Fact]
    public void Allocate_ExcludesExpiredAndNonReleasedLots()
    {
        var perime = MakeLot(new DateOnly(2020, 1, 1), 100, status: QualityStatus.Perime);
        var quarantaine = MakeLot(new DateOnly(2026, 1, 1), 100, status: QualityStatus.EnQuarantaine);
        var nonConforme = MakeLot(new DateOnly(2026, 1, 1), 100, status: QualityStatus.NonConforme);
        var libere = MakeLot(new DateOnly(2026, 12, 1), 10, status: QualityStatus.Libere);

        var result = FefoAllocator.Allocate([perime, quarantaine, nonConforme, libere], 10);

        Assert.Equal(FefoAllocationOutcome.Success, result.Outcome);
        var line = Assert.Single(result.Lines);
        Assert.Equal(libere.Id, line.LotId);
    }

    [Fact]
    public void Allocate_ExcludesLotsWithNoAvailableQuantity()
    {
        var fullyReserved = MakeLot(new DateOnly(2026, 12, 1), remaining: 20, reserved: 20);
        var available = MakeLot(new DateOnly(2027, 1, 1), remaining: 5);

        var result = FefoAllocator.Allocate([fullyReserved, available], 5);

        Assert.Equal(FefoAllocationOutcome.Success, result.Outcome);
        var line = Assert.Single(result.Lines);
        Assert.Equal(available.Id, line.LotId);
    }

    [Fact]
    public void Allocate_NoEligibleLots_ReturnsNoAvailableLot()
    {
        var perime = MakeLot(new DateOnly(2020, 1, 1), 100, status: QualityStatus.Perime);

        var result = FefoAllocator.Allocate([perime], 1);

        Assert.Equal(FefoAllocationOutcome.NoAvailableLot, result.Outcome);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public void Allocate_RequestedQuantityAboveTotalAvailable_ReturnsInsufficientStock()
    {
        var lot = MakeLot(new DateOnly(2026, 12, 1), 10);

        var result = FefoAllocator.Allocate([lot], 50);

        Assert.Equal(FefoAllocationOutcome.InsufficientStock, result.Outcome);
        Assert.Equal(10, result.TotalAvailable);
        Assert.Empty(result.Lines);
    }
}
