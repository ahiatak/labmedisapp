using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T125 — retour → disposition → génération avoir (FR-060 à FR-062).</summary>
public class CustomerReturnTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateReturn_OnDeliveredOrder_GeneratesCreditNote()
    {
        Guid saleOrderId, saleOrderLineId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var xof = new Currency { Code = "XOF", Name = "Franc CFA" };
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
            var customer = new Customer { Name = $"Client-{Guid.NewGuid():N}", Type = CustomerType.Pharmacie, PaymentDays = 30, IsActive = true };

            context.Currencies.Add(xof);
            context.Categories.Add(category);
            context.Products.Add(product);
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var lot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 20,
                RemainingQuantity = 15,
                UnitCostCfa = 1000m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(lot);

            var saleOrder = new SaleOrder
            {
                OrderNumber = $"SO-TEST-{Guid.NewGuid():N}",
                CustomerId = customer.Id,
                CurrencyId = xof.Id,
                Status = SaleOrderStatus.Livree,
                TotalHt = 15000m,
                TotalTva = 2700m,
                TotalTtc = 17700m,
                CreatedByUserId = Guid.NewGuid(),
                Lines =
                [
                    new SaleOrderLine { ProductId = product.Id, Quantity = 5, UnitPriceHt = 1500m, AllocatedStockLotId = lot.Id }
                ]
            };
            context.SaleOrders.Add(saleOrder);
            await context.SaveChangesAsync();

            context.Deliveries.Add(new Delivery { SaleOrderId = saleOrder.Id, DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow) });
            await context.SaveChangesAsync();

            saleOrderId = saleOrder.Id;
            saleOrderLineId = saleOrder.Lines[0].Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/api/sale-orders/{saleOrderId}/returns", new CreateReturnRequest
        {
            SaleOrderLineId = saleOrderLineId,
            Quantity = 2,
            Disposition = "RemiseEnStock",
            Motif = "Client insatisfait"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerReturnResponse>();
        Assert.Equal("Traite", result!.Status);
        Assert.NotNull(result.CreditNoteId);
        Assert.NotNull(result.CreditNoteNumber);
        Assert.Equal("3000", result.CreditNoteAmount); // 2 × 1500

        using var verifyScope = factory.Services.CreateScope();
        var context2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newLot = context2.StockLots.Single(l => l.InternalLotNumber.Contains("-RET-"));
        Assert.Equal(QualityStatus.Libere, newLot.QualityStatus);
        Assert.Equal(2, newLot.RemainingQuantity);
    }
}
