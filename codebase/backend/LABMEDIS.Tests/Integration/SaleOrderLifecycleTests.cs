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
/// T113 — cycle complet commande vente confirmée → réservation FEFO → livraison → facture
/// avec numéro de lot (FR-054 à FR-059). The invoice JSON already carries the lot number
/// (InvoiceLineResponse.InternalLotNumber, the same data the PDF renders per FR-058); PDF
/// byte generation itself needs the native libwkhtmltox library (research.md §8), not
/// available in this sandbox, so it is not independently asserted here.
/// </summary>
public class SaleOrderLifecycleTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullLifecycle_ConfirmDeliverInvoice_TracesLotOnInvoice()
    {
        Guid productId, customerId, xofId, lotId;
        const string internalLotNumber = "TESTLOT-0001";

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
                InternalLotNumber = internalLotNumber,
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 50,
                RemainingQuantity = 50,
                UnitCostCfa = 1000m,
                QualityStatus = QualityStatus.Libere,
                ReceivedByUserId = Guid.NewGuid()
            };
            context.StockLots.Add(lot);

            context.Set<ProductPrice>().Add(new ProductPrice
            {
                ProductId = product.Id,
                CumpCfa = 1000m,
                PvHtCalculated = 1500m,
                PvHtApplied = 1500m,
                PriceGap = 0m,
                VatRate = 0.18m,
                CreatedByUserId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            productId = product.Id;
            customerId = customer.Id;
            xofId = xof.Id;
            lotId = lot.Id;
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial", "Comptable");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/sale-orders", new CreateSaleOrderRequest
        {
            CustomerId = customerId,
            CurrencyId = xofId,
            Lines = [new CreateSaleOrderLineRequest { ProductId = productId, Quantity = 10 }]
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<SaleOrderResponse>();
        Assert.Equal("Brouillon", order!.Status);

        var confirmResponse = await client.PostAsync($"/api/sale-orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<SaleOrderResponse>();
        Assert.Equal("Confirmee", confirmed!.Status);
        Assert.Equal(lotId, confirmed.Lines[0].AllocatedStockLotId);

        var deliverResponse = await client.PostAsync($"/api/sale-orders/{order.Id}/deliver", null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var delivered = await deliverResponse.Content.ReadFromJsonAsync<SaleOrderResponse>();
        Assert.Equal("Livree", delivered!.Status);

        var invoiceResponse = await client.PostAsync($"/api/sale-orders/{order.Id}/invoice", null);
        Assert.Equal(HttpStatusCode.OK, invoiceResponse.StatusCode);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var invoiceLine = Assert.Single(invoice!.Lines);
        Assert.Equal(lotId, invoiceLine.StockLotId);
        Assert.Equal(internalLotNumber, invoiceLine.InternalLotNumber);

        using var verifyScope = factory.Services.CreateScope();
        var context2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockLot = await context2.StockLots.FindAsync(lotId);
        Assert.Equal(40, stockLot!.RemainingQuantity);
    }
}
