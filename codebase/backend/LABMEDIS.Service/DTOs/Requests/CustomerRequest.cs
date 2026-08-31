using LABMEDIS.Service.Extensions;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;

namespace LABMEDIS.Service.DTOs.Requests;

/// <summary>FR-008/FR-009. CreditLimit is a string (Principle VI).</summary>
public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "Autre";

    public string? Address { get; set; }

    public int PaymentDays { get; set; } = 30;

    public string? CreditLimit { get; set; }

    public CustomerEntity ToCustomer() => new()
    {
        Name = Name,
        Type = Enum.Parse<Core.Models.Entities.CustomerType>(Type),
        Address = Address,
        PaymentDays = PaymentDays,
        CreditLimit = string.IsNullOrWhiteSpace(CreditLimit) ? null : CreditLimit.ToDecimal(),
        IsActive = true
    };
}

public class UpdateCustomerRequest : CreateCustomerRequest
{
    public bool IsActive { get; set; } = true;

    public void ApplyTo(CustomerEntity entity)
    {
        entity.Name = Name;
        entity.Type = Enum.Parse<Core.Models.Entities.CustomerType>(Type);
        entity.Address = Address;
        entity.PaymentDays = PaymentDays;
        entity.CreditLimit = string.IsNullOrWhiteSpace(CreditLimit) ? null : CreditLimit.ToDecimal();
        entity.IsActive = IsActive;
    }
}

public class NegotiatedPriceRequest
{
    public Guid ProductId { get; set; }

    public string UnitPrice { get; set; } = "0";

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }
}
