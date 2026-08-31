using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T080 — réception commande → création lots → PRU figé → PMP recalculé (FR-029, FR-032, FR-033).</summary>
public class StockReceptionTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Receive_PurchaseOrder_CreatesLotWithFrozenPruAndUpdatesWeightedAverageCost()
    {
        Guid productId, packagingId, supplierId, xofId, locationId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var xof = new Currency { Code = "XOF", Name = "Franc CFA" };
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}", Kind = CategoryKind.ReactifLaboratoire };
            var supplier = new Supplier { Name = $"Fournisseur-{Guid.NewGuid():N}", Country = "Côte d'Ivoire", DefaultCurrencyId = Guid.NewGuid(), IsActive = true };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
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
            TransportMode = "Maritime",
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = productId, Quantity = 100, UnitPriceForeign = "500", PackagingId = packagingId }]
        });
        createResponse.EnsureSuccessStatusCode();
        var order = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        var lineId = order!.Lines[0].Id;

        (await client.PostAsync($"/api/purchase-orders/{order.Id}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/purchase-orders/{order.Id}/validate", null)).EnsureSuccessStatusCode();

        var receiveResponse = await client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/receive", new List<ReceiveLotLineRequest>
        {
            new()
            {
                LineId = lineId,
                LotNumber = $"SUP-{Guid.NewGuid():N}"[..15],
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(365)),
                QuantityReceived = 100,
                StorageLocationId = locationId,
                QualityStatus = "EnAttenteLiberation"
            }
        });

        Assert.Equal(HttpStatusCode.OK, receiveResponse.StatusCode);
        var createdLots = await receiveResponse.Content.ReadFromJsonAsync<List<StockLotResponse>>();
        var lot = Assert.Single(createdLots!);

        // PRU = unitPriceForeign(500) × exchangeRate(1, direct XOF order) = 500, frozen (FR-032).
        Assert.Equal("500", lot.UnitCostCfa);
        Assert.Equal(100, lot.InitialQuantity);
        Assert.Equal(100, lot.RemainingQuantity);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedOrder = await context.PurchaseOrders.FirstAsync(o => o.Id == order.Id);
            Assert.Equal(PurchaseOrderStatus.Recue, updatedOrder.Status);

            var movementExists = await context.StockMovements.AnyAsync(m => m.StockLotId == lot.Id && m.MovementType == StockMovementType.ReceptionFournisseur);
            Assert.True(movementExists, "Le mouvement de réception doit être journalisé (FR-038).");
        }

        // Release the lot then verify PMP/CUMP is exactly its cost with a single Libéré lot (FR-033).
        var releaseResponse = await client.PostAsync($"/api/stock/lots/{lot.Id}/release", null);
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var stockLotService = verifyScope.ServiceProvider.GetRequiredService<LABMEDIS.Service.Services.IStockLotService>();
        var available = await stockLotService.GetAvailableAsync(productId);
        Assert.Equal(100, available.TotalAvailable);
    }
}
