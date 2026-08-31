using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>
/// T097 — vente bloquée si le lot n'est pas au statut "Libéré" (FR-041). Sale order allocation
/// (US7) is not implemented yet — it will reuse POST /api/stock/lots/allocate, the mechanism
/// this test exercises directly.
/// </summary>
public class QualityBlockSaleTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Allocate_LotNotReleased_IsRejected()
    {
        Guid lotId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var lot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 20,
                RemainingQuantity = 20,
                UnitCostCfa = 100m,
                QualityStatus = QualityStatus.EnQuarantaine,
                QuarantineReason = "En cours de contrôle",
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(lot);
            await context.SaveChangesAsync();
            lotId = lot.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Commercial lacks Stock.Move, so this is expected to fail with 403 first — the
        // permission gate itself is a first line of defense; ResponsableQualite carries
        // neither Stock.Move, so we use Logistique (which does) to reach the business rule.
        var logistiqueToken = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"logistique-{Guid.NewGuid()}@labmedis.test", "Logistique");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", logistiqueToken);

        var response = await client.PostAsJsonAsync("/api/stock/lots/allocate", new AllocateLotRequest
        {
            LotId = lotId,
            Quantity = 5,
            Reason = "Test de blocage qualité"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("LOT_NOT_RELEASED", error!["code"]);
    }
}
