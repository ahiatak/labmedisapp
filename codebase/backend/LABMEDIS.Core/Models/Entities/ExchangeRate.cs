namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Exchange rate between two currencies, effective from a given date (FR-085/FR-086/RG-003).
/// EUR/XOF is fixed at 655.957 and modifiable only by an Admin via an explicit, audited
/// action; USD/XOF is variable, entered manually, and historized. Transactions that must
/// lock a rate (e.g. purchase orders, FR-021) store the Id of the row they used — this
/// entity itself is never mutated or recalculated retroactively.
/// </summary>
public class ExchangeRate : BaseEntity
{
    public Guid CurrencyFromId { get; set; }

    public Guid CurrencyToId { get; set; }

    public decimal Rate { get; set; }

    public DateOnly EffectiveDate { get; set; }

    /// <summary>True for the EUR/XOF fixed rate (655.957) — modifiable only by Admin.</summary>
    public bool IsFixed { get; set; }

    public Guid SetByUserId { get; set; }

    public Currency? CurrencyFrom { get; set; }

    public Currency? CurrencyTo { get; set; }
}
