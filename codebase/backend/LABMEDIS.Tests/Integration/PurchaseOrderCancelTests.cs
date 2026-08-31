using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T066 — annulation d'une commande d'achat sans motif refusée (400), FR-024.</summary>
public class PurchaseOrderCancelTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Cancel_WithEmptyReason_ReturnsBadRequest()
    {
        Guid productId, packagingId, supplierId, currencyId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var xof = context.Currencies.FirstOrDefault(c => c.Code == "XOF");
            if (xof is null)
            {
                xof = new Currency { Code = "XOF", Name = "Franc CFA" };
                context.Currencies.Add(xof);
            }

            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var supplier = new Supplier { Name = $"Fournisseur-{Guid.NewGuid():N}", Country = "Côte d'Ivoire", DefaultCurrencyId = xof.Id, IsActive = true };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };

            context.Categories.Add(category);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var packaging = new ProductPackaging { ProductId = product.Id, PackagingType = PackagingType.Unite, QuantityPerPackage = 1 };
            context.Set<ProductPackaging>().Add(packaging);
            await context.SaveChangesAsync();

            productId = product.Id;
            packagingId = packaging.Id;
            supplierId = supplier.Id;
            // Ordering directly in XOF skips exchange-rate resolution entirely — irrelevant to this test's purpose.
            currencyId = xof.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"achats-{Guid.NewGuid()}@labmedis.test", "ResponsableAchats");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            CurrencyId = currencyId,
            TransportMode = "Maritime",
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = productId, Quantity = 5, UnitPriceForeign = "10", PackagingId = packagingId }]
        });
        createResponse.EnsureSuccessStatusCode();
        var order = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();

        var cancelResponse = await client.PostAsJsonAsync($"/api/purchase-orders/{order!.Id}/cancel", new CancelPurchaseOrderRequest { Reason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
    }
}
