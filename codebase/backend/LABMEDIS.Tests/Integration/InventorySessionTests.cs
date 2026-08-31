using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T132 — session inventaire → gel mouvements → écarts → ajustements motivés (FR-044).</summary>
public class InventorySessionTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Session_CountWithVariance_RequiresReasonAndAdjustsStock()
    {
        Guid lotId;
        var locationCode = $"LOC-{Guid.NewGuid():N}"[..12];

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
            var warehouse = new Warehouse { Name = $"Entrepôt-{Guid.NewGuid():N}" };
            context.Categories.Add(category);
            context.Products.Add(product);
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var location = new StorageLocation { Code = locationCode, WarehouseId = warehouse.Id, LocationType = LocationType.Stockage };
            context.StorageLocations.Add(location);

            var lot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 100,
                RemainingQuantity = 100,
                UnitCostCfa = 500m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(lot);
            await context.SaveChangesAsync();

            context.Set<StockLotLocation>().Add(new StockLotLocation { StockLotId = lot.Id, StorageLocationId = location.Id, Quantity = 100 });
            await context.SaveChangesAsync();

            lotId = lot.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"logistique-{Guid.NewGuid()}@labmedis.test", "Logistique");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/stock/inventory-sessions", new CreateInventorySessionRequest { Perimeter = locationCode });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var session = await createResponse.Content.ReadFromJsonAsync<InventorySessionResponse>();
        Assert.Equal("Gelee", session!.Status);
        var count = Assert.Single(session.Counts);
        Assert.Equal(100, count.SystemQuantity);

        var countResponse = await client.PostAsJsonAsync($"/api/stock/inventory-sessions/{session.Id}/counts", new RecordCountRequest { StockLotId = lotId, CountedQuantity = 97 });
        Assert.Equal(HttpStatusCode.OK, countResponse.StatusCode);

        var validateWithoutReason = await client.PostAsJsonAsync($"/api/stock/inventory-sessions/{session.Id}/validate", new ValidateInventorySessionRequest());
        Assert.Equal(HttpStatusCode.BadRequest, validateWithoutReason.StatusCode);

        var validateResponse = await client.PostAsJsonAsync($"/api/stock/inventory-sessions/{session.Id}/validate", new ValidateInventorySessionRequest { AdjustmentReason = "Casse constatée en rayon" });
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        var validated = await validateResponse.Content.ReadFromJsonAsync<InventorySessionResponse>();
        Assert.Equal("Cloturee", validated!.Status);

        using var verifyScope = factory.Services.CreateScope();
        var context2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lot2 = await context2.StockLots.FindAsync(lotId);
        Assert.Equal(97, lot2!.RemainingQuantity);
    }
}
