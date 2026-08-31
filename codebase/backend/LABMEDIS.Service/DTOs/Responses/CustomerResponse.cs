using LABMEDIS.Service.Extensions;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;
using CustomerProductPriceEntity = LABMEDIS.Core.Models.Entities.CustomerProductPrice;

namespace LABMEDIS.Service.DTOs.Responses;

public class CustomerResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Address { get; set; }

    public int PaymentDays { get; set; }

    public string? CreditLimit { get; set; }

    public bool IsActive { get; set; }

    public CustomerResponse(CustomerEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Type = entity.Type.ToString();
        Address = entity.Address;
        PaymentDays = entity.PaymentDays;
        CreditLimit = entity.CreditLimit?.ToInvariantString("0.##");
        IsActive = entity.IsActive;
    }
}

public class OutstandingBalanceResponse
{
    public Guid CustomerId { get; set; }

    public string OutstandingBalance { get; set; } = "0";

    public string? CreditLimit { get; set; }

    public bool IsOverLimit { get; set; }
}

public class NegotiatedPriceResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public string UnitPrice { get; set; } = "0";

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    public NegotiatedPriceResponse(CustomerProductPriceEntity entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        UnitPrice = entity.UnitPrice.ToInvariantString("0.##");
        ValidFrom = entity.ValidFrom;
        ValidTo = entity.ValidTo;
    }
}
