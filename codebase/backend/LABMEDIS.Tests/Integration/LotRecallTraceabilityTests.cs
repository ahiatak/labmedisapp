using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T162 — recherche des clients ayant reçu un lot donné, pour un rappel produit (FR-081).</summary>
public class LotRecallTraceabilityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetLotTraceability_ReturnsEveryCustomerThatReceivedTheLot()
    {
        Guid lotId;
        string customerAName, customerBName;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
            var xof = new Currency { Code = "XOF", Name = "Franc CFA" };
            var customerA = new Customer { Name = $"Pharmacie-A-{Guid.NewGuid():N}", Type = CustomerType.Pharmacie, PaymentDays = 30, IsActive = true };
            var customerB = new Customer { Name = $"Pharmacie-B-{Guid.NewGuid():N}", Type = CustomerType.Pharmacie, PaymentDays = 30, IsActive = true };

            context.Categories.Add(category);
            context.Products.Add(product);
            context.Currencies.Add(xof);
            context.Customers.Add(customerA);
            context.Customers.Add(customerB);
            await context.SaveChangesAsync();

            var lot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 100,
                RemainingQuantity = 80,
                UnitCostCfa = 1000m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(lot);
            await context.SaveChangesAsync();

            context.SaleOrders.Add(new SaleOrder
            {
                OrderNumber = $"SO-TEST-{Guid.NewGuid():N}",
                CustomerId = customerA.Id,
                CurrencyId = xof.Id,
                Status = SaleOrderStatus.Facturee,
                TotalHt = 15000m,
                TotalTva = 2700m,
                TotalTtc = 17700m,
                CreatedByUserId = Guid.NewGuid(),
                Lines = [new SaleOrderLine { ProductId = product.Id, Quantity = 10, UnitPriceHt = 1500m, AllocatedStockLotId = lot.Id }]
            });
            context.SaleOrders.Add(new SaleOrder
            {
                OrderNumber = $"SO-TEST-{Guid.NewGuid():N}",
                CustomerId = customerB.Id,
                CurrencyId = xof.Id,
                Status = SaleOrderStatus.Livree,
                TotalHt = 15000m,
                TotalTva = 2700m,
                TotalTtc = 17700m,
                CreatedByUserId = Guid.NewGuid(),
                Lines = [new SaleOrderLine { ProductId = product.Id, Quantity = 10, UnitPriceHt = 1500m, AllocatedStockLotId = lot.Id }]
            });
            // A third order NOT using this lot must never appear in the recall list.
            var otherLot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 50,
                RemainingQuantity = 50,
                UnitCostCfa = 1000m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(otherLot);
            await context.SaveChangesAsync();

            var unrelatedCustomer = new Customer { Name = $"Pharmacie-C-{Guid.NewGuid():N}", Type = CustomerType.Pharmacie, PaymentDays = 30, IsActive = true };
            context.Customers.Add(unrelatedCustomer);
            await context.SaveChangesAsync();
            context.SaleOrders.Add(new SaleOrder
            {
                OrderNumber = $"SO-TEST-{Guid.NewGuid():N}",
                CustomerId = unrelatedCustomer.Id,
                CurrencyId = xof.Id,
                Status = SaleOrderStatus.Livree,
                TotalHt = 7500m,
                TotalTva = 1350m,
                TotalTtc = 8850m,
                CreatedByUserId = Guid.NewGuid(),
                Lines = [new SaleOrderLine { ProductId = product.Id, Quantity = 5, UnitPriceHt = 1500m, AllocatedStockLotId = otherLot.Id }]
            });
            await context.SaveChangesAsync();

            lotId = lot.Id;
            customerAName = customerA.Name;
            customerBName = customerB.Name;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"qualite-{Guid.NewGuid()}@labmedis.test", "ResponsableQualite");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/compliance/lots/{lotId}/traceability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var traceability = await response.Content.ReadFromJsonAsync<LotTraceabilityResponse>();

        Assert.Equal(2, traceability!.Customers.Count);
        Assert.Contains(traceability.Customers, c => c.Name == customerAName);
        Assert.Contains(traceability.Customers, c => c.Name == customerBName);

        // Attaching a regulatory document to the same lot must be retrievable afterwards.
        var attachResponse = await client.PostAsJsonAsync("/api/compliance/attachments", new CreateAttachmentRequest
        {
            AttachableType = "StockLot",
            AttachableId = lotId,
            DocumentType = "Certificat",
            FileReference = "certificats/lot-analyse.pdf"
        });
        Assert.Equal(HttpStatusCode.OK, attachResponse.StatusCode);

        var attachmentsResponse = await client.GetAsync($"/api/compliance/attachments?attachableType=StockLot&attachableId={lotId}");
        var attachments = await attachmentsResponse.Content.ReadFromJsonAsync<List<AttachmentResponse>>();
        Assert.Single(attachments!);
    }
}
