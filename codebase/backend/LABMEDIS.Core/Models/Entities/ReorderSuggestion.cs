namespace LABMEDIS.Core.Models.Entities;

public enum ReorderSuggestionStatus
{
    EnAttente = 0,
    Converti = 1,
    Rejete = 2
}

/// <summary>Automatic replenishment suggestion (US10 — FR-064/FR-065).</summary>
public class ReorderSuggestion : BaseEntity
{
    public Guid ProductId { get; set; }

    public DateOnly SuggestionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly OrderDeadline { get; set; }

    public int SuggestedQuantity { get; set; }

    public ReorderSuggestionStatus Status { get; set; } = ReorderSuggestionStatus.EnAttente;

    public Guid? ConvertedPurchaseOrderId { get; set; }

    public Product? Product { get; set; }
}
