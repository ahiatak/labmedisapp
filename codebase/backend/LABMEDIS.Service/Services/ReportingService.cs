using System.Globalization;
using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using LABMEDIS.Core.Repositories.Reporting;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;

namespace LABMEDIS.Service.Services;

/// <summary>Reporting & dashboards (US11 — FR-068 à FR-075). Inherits ReportingRepository directly (Principle II).</summary>
public class ReportingService(LABMEDIS.Core.AppDbContext context, IConverter converter) : ReportingRepository(context), IReportingService
{
    public async Task<DirectionDashboardResponse> GetDirectionDashboardAsync(CancellationToken cancellationToken = default) => new()
    {
        TotalRevenueCfa = (await GetTotalRevenueAsync(null, null, cancellationToken)).ToInvariantString("0"),
        TotalMarginCfa = (await GetTotalMarginAsync(null, null, cancellationToken)).ToInvariantString("0"),
        StockValueCfa = (await GetStockValueAsync(cancellationToken)).ToInvariantString("0"),
        StockoutProductCount = await GetStockoutProductCountAsync(cancellationToken)
    };

    public async Task<StockReportResponse> GetStockReportAsync(CancellationToken cancellationToken = default)
    {
        var (available, reserved, quarantine, expired) = await GetStockBreakdownAsync(cancellationToken);
        var slowMoving = await GetSlowMovingProductsAsync(90, cancellationToken);
        return new StockReportResponse
        {
            TotalAvailable = available,
            TotalReserved = reserved,
            TotalQuarantine = quarantine,
            TotalExpired = expired,
            SlowMovingProductCount = slowMoving.Count
        };
    }

    async Task<IReadOnlyList<ExpiringLotReportLine>> IReportingService.GetExpiringLotsAsync(int days, CancellationToken cancellationToken)
    {
        var lots = await GetExpiringLotsAsync(days, cancellationToken);
        return lots.Select(l => new ExpiringLotReportLine
        {
            LotId = l.Id,
            ProductDesignation = l.Product?.Designation,
            InternalLotNumber = l.InternalLotNumber,
            ExpiryDate = l.ExpiryDate,
            RemainingQuantity = l.RemainingQuantity
        }).ToList();
    }

    public async Task<IReadOnlyList<SlowMovingProductReportLine>> GetSlowMovingProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await GetSlowMovingProductsAsync(90, cancellationToken);
        return products.Select(p => new SlowMovingProductReportLine
        {
            ProductId = p.Product.Id,
            ProductDesignation = p.Product.Designation,
            LastMovementAt = p.LastMovementAt,
            DaysSinceLastMovement = p.LastMovementAt.HasValue ? (int)(DateTime.UtcNow - p.LastMovementAt.Value).TotalDays : -1
        }).ToList();
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(CancellationToken cancellationToken = default)
    {
        var byCustomer = await GetRevenueByCustomerAsync(cancellationToken);
        var byProduct = await GetRevenueByProductAsync(cancellationToken);
        var returnRate = await GetReturnRatePercentAsync(cancellationToken);

        return new SalesReportResponse
        {
            TotalRevenueCfa = byCustomer.Sum(x => x.Revenue).ToInvariantString("0"),
            ReturnRatePercent = returnRate.ToInvariantString("0.##"),
            ByCustomer = byCustomer.Select(x => new SalesReportLine { Id = x.Customer.Id, Name = x.Customer.Name, RevenueCfa = x.Revenue.ToInvariantString("0") }).ToList(),
            ByProduct = byProduct.Select(x => new SalesReportLine { Id = x.Product.Id, Name = x.Product.Designation, RevenueCfa = x.Revenue.ToInvariantString("0") }).ToList()
        };
    }

    public async Task<IReadOnlyList<PricingReportLine>> GetPricingReportAsync(CancellationToken cancellationToken = default)
    {
        var prices = await GetLatestPricesForAllProductsAsync(cancellationToken);
        return prices.Select(p => new PricingReportLine
        {
            ProductId = p.ProductId,
            ProductDesignation = p.Product?.Designation,
            TheoreticalMarginCfa = (p.PvHtCalculated - p.CumpCfa).ToInvariantString("0"),
            RealMarginCfa = (p.PvHtApplied - p.CumpCfa).ToInvariantString("0"),
            PriceGapCfa = p.PriceGap.ToInvariantString("0")
        }).ToList();
    }

    public async Task<QualityReportResponse> GetQualityReportAsync(CancellationToken cancellationToken = default)
    {
        var lots = await GetQualityLotsAsync(cancellationToken);
        return new QualityReportResponse
        {
            QuarantineCount = lots.Count(l => l.QualityStatus == Core.Models.Entities.QualityStatus.EnQuarantaine),
            NonConformeCount = lots.Count(l => l.QualityStatus == Core.Models.Entities.QualityStatus.NonConforme),
            Lots = lots.Select(l => new StockLotResponse(l)).ToList()
        };
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(ExportReportRequest request, CancellationToken cancellationToken = default)
    {
        var rows = request.ReportType.ToLowerInvariant() switch
        {
            "stock" => (await ((IReportingService)this).GetExpiringLotsAsync(365, cancellationToken))
                .Select(l => new Dictionary<string, string>
                {
                    ["Produit"] = l.ProductDesignation ?? "",
                    ["Lot"] = l.InternalLotNumber,
                    ["Péremption"] = l.ExpiryDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    ["Quantité"] = l.RemainingQuantity.ToString(CultureInfo.InvariantCulture)
                }).ToList(),
            "sales" => (await GetSalesReportAsync(cancellationToken)).ByProduct
                .Select(l => new Dictionary<string, string> { ["Produit"] = l.Name ?? "", ["CA (CFA)"] = l.RevenueCfa }).ToList(),
            "pricing" => (await GetPricingReportAsync(cancellationToken))
                .Select(l => new Dictionary<string, string> { ["Produit"] = l.ProductDesignation ?? "", ["Marge théorique"] = l.TheoreticalMarginCfa, ["Marge réelle"] = l.RealMarginCfa, ["Écart PV"] = l.PriceGapCfa }).ToList(),
            "quality" => (await GetQualityReportAsync(cancellationToken)).Lots
                .Select(l => new Dictionary<string, string> { ["Lot"] = l.InternalLotNumber, ["Statut"] = l.QualityStatus, ["Motif"] = l.QuarantineReason ?? "" }).ToList(),
            _ => throw new AppException(400, "INVALID_REPORT_TYPE", "Type de rapport invalide.")
        };

        return request.Format.Equals("Pdf", StringComparison.OrdinalIgnoreCase)
            ? (GeneratePdf(request.ReportType, rows), "application/pdf", $"rapport-{request.ReportType}.pdf")
            : (GenerateExcel(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"rapport-{request.ReportType}.xlsx");
    }

    private static byte[] GenerateExcel(List<Dictionary<string, string>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Rapport");
        if (rows.Count > 0)
        {
            var headers = rows[0].Keys.ToList();
            for (var col = 0; col < headers.Count; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
            }

            for (var row = 0; row < rows.Count; row++)
            {
                for (var col = 0; col < headers.Count; col++)
                {
                    sheet.Cell(row + 2, col + 1).Value = rows[row][headers[col]];
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] GeneratePdf(string reportType, List<Dictionary<string, string>> rows)
    {
        var headers = rows.Count > 0 ? rows[0].Keys.ToList() : [];
        var headerHtml = string.Join("", headers.Select(h => $"<th>{h}</th>"));
        var rowsHtml = string.Join("", rows.Select(r => "<tr>" + string.Join("", headers.Select(h => $"<td>{r[h]}</td>")) + "</tr>"));

        var html = $"""
            <html><head><meta charset="utf-8" /></head>
            <body style="font-family: Arial, sans-serif;">
              <h1>Rapport — {reportType}</h1>
              <table border="1" cellpadding="6" cellspacing="0" width="100%">
                <thead><tr>{headerHtml}</tr></thead>
                <tbody>{rowsHtml}</tbody>
              </table>
            </body></html>
            """;

        var document = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings { ColorMode = ColorMode.Color, Orientation = Orientation.Landscape, PaperSize = PaperKind.A4 },
            Objects = { new ObjectSettings { HtmlContent = html, WebSettings = { DefaultEncoding = "utf-8" } } }
        };

        return converter.Convert(document);
    }
}
