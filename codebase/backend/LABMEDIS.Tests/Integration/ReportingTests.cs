using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T147 — rapports ventes/stock/pricing renvoient des agrégations correctes + export (FR-068 à FR-075).</summary>
public class ReportingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task StockReport_And_SalesReport_ReturnCorrectAggregates_AndExportSucceeds()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
            var product = new Product { Designation = $"Produit-{Guid.NewGuid():N}", CategoryId = category.Id, VatRate = 0.18m, IsActive = true };
            var customer = new Customer { Name = $"Client-{Guid.NewGuid():N}", Type = CustomerType.Pharmacie, PaymentDays = 30, IsActive = true };
            var xof = new Currency { Code = "XOF", Name = "Franc CFA" };

            context.Categories.Add(category);
            context.Products.Add(product);
            context.Customers.Add(customer);
            context.Currencies.Add(xof);
            await context.SaveChangesAsync();

            var lot = new StockLot
            {
                ProductId = product.Id,
                SupplierLotNumber = $"SUP-{Guid.NewGuid():N}",
                InternalLotNumber = $"INT-{Guid.NewGuid():N}",
                ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                InitialQuantity = 100,
                RemainingQuantity = 60,
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
                Status = SaleOrderStatus.Facturee,
                TotalHt = 40000m,
                TotalTva = 7200m,
                TotalTtc = 47200m,
                CreatedByUserId = Guid.NewGuid()
            };
            context.SaleOrders.Add(saleOrder);
            await context.SaveChangesAsync();

            context.Invoices.Add(new Invoice
            {
                InvoiceNumber = $"INV-TEST-{Guid.NewGuid():N}",
                SaleOrderId = saleOrder.Id,
                CustomerId = customer.Id,
                CurrencyId = xof.Id,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = InvoiceStatus.Emise,
                TotalHt = 40000m,
                TotalTva = 7200m,
                TotalTtc = 47200m,
                Lines =
                [
                    new InvoiceLine { ProductId = product.Id, StockLotId = lot.Id, Quantity = 40, UnitPriceHt = 1000m, VatRate = 0.18m }
                ]
            });
            await context.SaveChangesAsync();
        }

        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"direction-{Guid.NewGuid()}@labmedis.test", "Direction");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var stockResponse = await client.GetAsync("/api/reports/stock");
        Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
        var stockReport = await stockResponse.Content.ReadFromJsonAsync<StockReportResponse>();
        Assert.True(stockReport!.TotalAvailable >= 60);

        var salesResponse = await client.GetAsync("/api/reports/sales");
        Assert.Equal(HttpStatusCode.OK, salesResponse.StatusCode);
        var salesReport = await salesResponse.Content.ReadFromJsonAsync<SalesReportResponse>();
        Assert.True(decimal.Parse(salesReport!.TotalRevenueCfa, System.Globalization.CultureInfo.InvariantCulture) >= 40000m);

        var directionResponse = await client.GetAsync("/api/reports/dashboard/direction");
        Assert.Equal(HttpStatusCode.OK, directionResponse.StatusCode);
        var dashboard = await directionResponse.Content.ReadFromJsonAsync<DirectionDashboardResponse>();
        Assert.True(decimal.Parse(dashboard!.TotalRevenueCfa, System.Globalization.CultureInfo.InvariantCulture) >= 40000m);

        var exportResponse = await client.PostAsJsonAsync("/api/reports/export", new ExportReportRequest { ReportType = "sales", Format = "Excel" });
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", exportResponse.Content.Headers.ContentType?.MediaType);
        var exportBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        Assert.True(exportBytes.Length > 0);
    }
}
