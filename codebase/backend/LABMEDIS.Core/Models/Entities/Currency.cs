namespace LABMEDIS.Core.Models.Entities;

/// <summary>Supported currency (EUR, USD, XOF — FR-085).</summary>
public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
