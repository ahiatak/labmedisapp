using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T081 — blocage réception d'un lot sous le seuil de péremption strict de 30 jours (FR-031).</summary>
public class ExpiryThresholdTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Receive_LotBelowThirtyDayThreshold_IsBlocked()
    {
        Guid productId, packagingId, supplierId, xofId, locationId, lineId, orderId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var xof = new Currency { Code = "XOF", Name = "Franc CFA" };
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}", Kind = CategoryKind.Medicament };
            var supplier = new Supplier { Name = $"Fournisseur-{Guid.NewGuid():N}", Country = "France", DefaultCurrencyId = Guid.NewGuid(), IsActive = true };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0m, IsActive = true };
            var warehouse = new Warehouse { Name = $"Entrepôt-{Guid.NewGuid():N}" };

            context.Currencies.Add(xof);
            context.Categories.Add(category);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var packaging = new ProductPackaging { ProductId = product.Id, PackagingType = PackagingType.Unite, QuantityPerPackage = 1 };
            var location = new StorageLocation { Code = $"LOC-{Guid.NewGuid():N}"[..12], WarehouseId = warehouse.Id, LocationType = LocationType.Stockage };
            context.Set<ProductPackaging>().Add(packaging);
            context.StorageLocations.Add(location);
            await context.SaveChangesAsync();

            productId = product.Id;
            packagingId = packaging.Id;
            supplierId = supplier.Id;
            xofId = xof.Id;
            locationId = location.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"achats-{Guid.NewGuid()}@labmedis.test", "ResponsableAchats", "Logistique");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            CurrencyId = xofId,
            TransportMode = "Aerien",
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = productId, Quantity = 10, UnitPriceForeign = "1000", PackagingId = packagingId }]
        });
        createResponse.EnsureSuccessStatusCode();
        var order = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        orderId = order!.Id;
        lineId = order.Lines[0].Id;

        (await client.PostAsync($"/api/purchase-orders/{orderId}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/purchase-orders/{orderId}/validate", null)).EnsureSuccessStatusCode();

        // 10 days to expiry — below the 30-day generic strict blocking threshold (FR-031),
        // even though this product's category (Médicament) only carries a 90-day *alert* threshold.
        var receiveResponse = await client.PostAsJsonAsync($"/api/purchase-orders/{orderId}/receive", new List<ReceiveLotLineRequest>
        {
            new()
            {
                LineId = lineId,
                LotNumber = $"SUP-{Guid.NewGuid():N}"[..15],
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                QuantityReceived = 10,
                StorageLocationId = locationId,
                QualityStatus = "EnAttenteLiberation"
            }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, receiveResponse.StatusCode);
        var error = await receiveResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("EXPIRY_BELOW_THRESHOLD", error!["code"]);
    }
}
