using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T096 — libération de lot réservée au rôle Responsable Qualité (403 sinon), FR-042/Principe VIII.</summary>
public class QualityReleaseTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<Guid> SeedQuarantinedLotAsync()
    {
        using var scope = factory.Services.CreateScope();
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
            InitialQuantity = 50,
            RemainingQuantity = 50,
            UnitCostCfa = 100m,
            QualityStatus = QualityStatus.EnQuarantaine,
            QuarantineReason = "Contrôle initial",
            ReceivedByUserId = Guid.NewGuid()
        };
        context.StockLots.Add(lot);
        await context.SaveChangesAsync();

        return lot.Id;
    }

    [Fact]
    public async Task Release_ByCommercialRole_IsForbidden()
    {
        var lotId = await SeedQuarantinedLotAsync();
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"/api/stock/lots/{lotId}/release", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Release_ByResponsableQualite_Succeeds()
    {
        var lotId = await SeedQuarantinedLotAsync();
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"qualite-{Guid.NewGuid()}@labmedis.test", "ResponsableQualite");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"/api/stock/lots/{lotId}/release", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lot = await response.Content.ReadFromJsonAsync<LABMEDIS.Service.DTOs.Responses.StockLotResponse>();
        Assert.Equal("Libere", lot!.QualityStatus);
    }
}
