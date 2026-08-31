using System.Globalization;

namespace LABMEDIS.Service.Extensions;

/// <summary>
/// Conversion helpers required by Principle VI of the constitution: every monetary/decimal
/// field on a Request DTO is a string; conversion to <see cref="decimal"/> happens here, and
/// CFA (XOF) rounding is applied ONLY on the final result of a calculation, never on
/// intermediate values.
/// </summary>
public static class DecimalExtensions
{
    public static decimal ToDecimal(this string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>
    /// CFA (XOF) rounding: zero decimal places, AwayFromZero. Apply only on the final
    /// output of a pricing calculation (see RG-004 / data-model.md §3).
    /// </summary>
    public static decimal ToCfaRounded(this decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    /// <summary>Culture-invariant string form of a decimal for Response DTOs (Principle VI).</summary>
    public static string ToInvariantString(this decimal value, string format = "0.####") =>
        value.ToString(format, CultureInfo.InvariantCulture);
}
