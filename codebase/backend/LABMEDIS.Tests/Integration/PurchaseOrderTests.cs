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

/// <summary>T065 — cycle de vie commande d'achat + seuil de validation Direction (FR-020 à FR-023).</summary>
public class PurchaseOrderTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<(Guid ProductId, Guid PackagingId, Guid SupplierId, Guid CurrencyId)> SeedReferentielAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eur = await EnsureCurrencyAsync(context, "EUR", "Euro");
        var xof = await EnsureCurrencyAsync(context, "XOF", "Franc CFA");

        if (!context.ExchangeRates.Any(r => r.CurrencyFromId == eur.Id && r.CurrencyToId == xof.Id))
        {
            context.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyFromId = eur.Id,
                CurrencyToId = xof.Id,
                Rate = 655.957m,
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                IsFixed = true,
                SetByUserId = Guid.NewGuid()
            });
        }

        var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
        context.Categories.Add(category);

        var supplier = new Supplier { Name = $"Fournisseur-{Guid.NewGuid():N}", Country = "France", DefaultCurrencyId = eur.Id, IsActive = true };
        context.Suppliers.Add(supplier);

        var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
        context.Products.Add(product);

        await context.SaveChangesAsync();

        var packaging = new ProductPackaging { ProductId = product.Id, PackagingType = PackagingType.Unite, QuantityPerPackage = 1 };
        context.Set<ProductPackaging>().Add(packaging);
        await context.SaveChangesAsync();

        return (product.Id, packaging.Id, supplier.Id, eur.Id);
    }

    private static async Task<Currency> EnsureCurrencyAsync(AppDbContext context, string code, string name)
    {
        var existing = context.Currencies.FirstOrDefault(c => c.Code == code);
        if (existing is not null)
        {
            return existing;
        }

        var currency = new Currency { Code = code, Name = name };
        context.Currencies.Add(currency);
        await context.SaveChangesAsync();
        return currency;
    }

    [Fact]
    public async Task Lifecycle_BelowThreshold_AchatsCanValidateWithoutDirection()
    {
        var (productId, packagingId, supplierId, currencyId) = await SeedReferentielAsync();
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"achats-{Guid.NewGuid()}@labmedis.test", "ResponsableAchats");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            CurrencyId = currencyId,
            TransportMode = "Maritime",
            Lines =
            [
                new CreatePurchaseOrderLineRequest { ProductId = productId, Quantity = 10, UnitPriceForeign = "5", PackagingId = packagingId }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.Equal("Brouillon", order!.Status);

        var submitResponse = await client.PostAsync($"/api/purchase-orders/{order.Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.Equal("EnAttenteValidation", submitted!.Status);

        var validateResponse = await client.PostAsync($"/api/purchase-orders/{order.Id}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        var validated = await validateResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.Equal("Validee", validated!.Status);
    }

    [Fact]
    public async Task Validate_AboveThreshold_RequiresDirection()
    {
        var (productId, packagingId, supplierId, currencyId) = await SeedReferentielAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await context.CompanyProfiles.FirstOrDefaultAsync();
            if (profile is null)
            {
                context.CompanyProfiles.Add(new CompanyProfile { PurchaseOrderValidationThresholdCfa = 1000m });
            }
            else
            {
                profile.PurchaseOrderValidationThresholdCfa = 1000m;
            }
            await context.SaveChangesAsync();
        }

        var achatsToken = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"achats-{Guid.NewGuid()}@labmedis.test", "ResponsableAchats");
        var achatsClient = factory.CreateClient();
        achatsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", achatsToken);

        // 10,000 EUR * 655.957 far exceeds the 1,000 XOF threshold configured above.
        var createResponse = await achatsClient.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            CurrencyId = currencyId,
            TransportMode = "Maritime",
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = productId, Quantity = 10, UnitPriceForeign = "1000", PackagingId = packagingId }]
        });
        var order = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        await achatsClient.PostAsync($"/api/purchase-orders/{order!.Id}/submit", null);

        var deniedResponse = await achatsClient.PostAsync($"/api/purchase-orders/{order.Id}/validate", null);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        var directionToken = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"direction-{Guid.NewGuid()}@labmedis.test", "Direction");
        var directionClient = factory.CreateClient();
        directionClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directionToken);

        var allowedResponse = await directionClient.PostAsync($"/api/purchase-orders/{order.Id}/validate", null);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }
}
