using LABMEDIS.Service.Extensions;

namespace LABMEDIS.Tests.Unit;

/// <summary>T104 — bloquant (constitution §Qualité). Arrondi CFA (Math.Round, AwayFromZero, zéro décimale).</summary>
public class CfaRoundingTests
{
    [Theory]
    [InlineData(100.4, 100)]
    [InlineData(100.5, 101)]
    [InlineData(100.49, 100)]
    [InlineData(100.51, 101)]
    public void ToCfaRounded_RoundsToZeroDecimalsAwayFromZero(double input, decimal expected)
    {
        Assert.Equal(expected, ((decimal)input).ToCfaRounded());
    }

    [Fact]
    public void ToCfaRounded_NegativeMidpoint_RoundsAwayFromZero()
    {
        Assert.Equal(-101m, (-100.5m).ToCfaRounded());
    }

    [Fact]
    public void ToCfaRounded_ExactInteger_IsUnchanged()
    {
        Assert.Equal(6560m, 6560m.ToCfaRounded());
    }

    [Fact]
    public void ToDecimal_ParsesInvariantCultureDecimalString()
    {
        Assert.Equal(655.957m, "655.957".ToDecimal());
    }
}
