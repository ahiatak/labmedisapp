using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>
/// T114 — conflit de réservation concurrente : deux confirmations parallèles sur la dernière
/// unité disponible ne doivent jamais survendre (FR-091/SC-013). Exercises the real
/// `SELECT ... FOR UPDATE` row lock (research.md §5) through two genuinely concurrent HTTP
/// requests against the same running host.
/// </summary>
public class ConcurrentReservationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Confirm_TwoOrdersForLastUnit_OnlyOneSucceeds()
    {
        Guid productId, customerId, xofId;

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

            // Exactly ONE unit of available stock — the two confirmations below race for it.
            context.StockLots.Add(new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 1,
                RemainingQuantity = 1,
                UnitCostCfa = 1000m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            });

            context.Set<ProductPrice>().Add(new ProductPrice
            {
                ProductId = product.Id,
                CumpCfa = 1000m,
                PvHtCalculated = 1500m,
                PvHtApplied = 1500m,
                VatRate = 0.18m,
                CreatedByUserId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            productId = product.Id;
            customerId = customer.Id;
            xofId = xof.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var clientA = factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var clientB = factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        async Task<HttpResponseMessage> CreateAndConfirmAsync(HttpClient client)
        {
            var createResponse = await client.PostAsJsonAsync("/api/sale-orders", new CreateSaleOrderRequest
            {
                CustomerId = customerId,
                CurrencyId = xofId,
                Lines = [new CreateSaleOrderLineRequest { ProductId = productId, Quantity = 1 }]
            });
            createResponse.EnsureSuccessStatusCode();
            var order = await createResponse.Content.ReadFromJsonAsync<SaleOrderResponse>();
            return await client.PostAsync($"/api/sale-orders/{order!.Id}/confirm", null);
        }

        var confirmTaskA = CreateAndConfirmAsync(clientA);
        var confirmTaskB = CreateAndConfirmAsync(clientB);
        var results = await Task.WhenAll(confirmTaskA, confirmTaskB);

        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        using var verifyScope = factory.Services.CreateScope();
        var context2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lot = context2.StockLots.Single(l => l.ProductId == productId);
        Assert.Equal(1, lot.ReservedQuantity);
        Assert.Equal(0, lot.AvailableQuantity);
    }
}
