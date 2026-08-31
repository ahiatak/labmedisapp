namespace LABMEDIS.Core.Models.Entities;

public enum ImportCostType
{
    Freight = 0,
    Transit = 1,
    Douane = 2,
    Commission = 3,
    Transfert = 4,
    Assurance = 5,
    Manutention = 6
}

/// <summary>How an ImportCost is spread across shipment lines when allocated (FR-026).</summary>
public enum AllocationKey
{
    Valeur = 0,
    Quantite = 1,
    Volume = 2
}

/// <summary>Logistics cost attached to a shipment, allocated across its lines by value/quantity/volume (FR-026).</summary>
public class ImportCost : BaseEntity
{
    public Guid ShipmentId { get; set; }

    public ImportCostType CostType { get; set; }

    public decimal Amount { get; set; }

    public AllocationKey AllocationKey { get; set; }

    public Shipment? Shipment { get; set; }
}
