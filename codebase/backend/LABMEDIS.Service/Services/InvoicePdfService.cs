using System.Globalization;
using System.Text;
using DinkToPdf;
using DinkToPdf.Contracts;
using LABMEDIS.Core.Repositories.Invoice;
using LABMEDIS.Service.Exceptions;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Invoice/BL PDF generation (US7 — FR-058, `DinkToPdf`, Principle IX). Requires the native
/// `libwkhtmltox` library at runtime (research.md §8, bundled in the container image) —
/// IConverter is registered as a lazy singleton factory in Program.cs so a missing native
/// library only fails PDF requests, never application startup.
/// </summary>
public class InvoicePdfService(IConverter converter, IInvoiceRepository invoiceRepository) : IInvoicePdfService
{
    public async Task<byte[]> GenerateInvoicePdfAsync(Guid saleOrderId, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetBySaleOrderIdAsync(saleOrderId, cancellationToken)
            ?? throw new AppException(404, "INVOICE_NOT_FOUND", "Aucune facture trouvée pour cette commande.");

        var document = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4
            },
            Objects =
            {
                new ObjectSettings { HtmlContent = BuildHtml(invoice), WebSettings = { DefaultEncoding = "utf-8" } }
            }
        };

        return converter.Convert(document);
    }

    private static string BuildHtml(Core.Models.Entities.Invoice invoice)
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var rows = new StringBuilder();
        foreach (var line in invoice.Lines)
        {
            rows.Append(CultureInfo.InvariantCulture, $"""
                <tr>
                  <td>{line.Product?.Designation}</td>
                  <td><strong>{line.StockLot?.InternalLotNumber}</strong></td>
                  <td>{line.Quantity}</td>
                  <td>{line.UnitPriceHt.ToString("N0", culture)} XOF</td>
                </tr>
                """);
        }

        return $"""
            <html><head><meta charset="utf-8" /></head>
            <body style="font-family: Arial, sans-serif;">
              <h1>Facture {invoice.InvoiceNumber}</h1>
              <p>Client : {invoice.Customer?.Name}</p>
              <p>Date : {invoice.InvoiceDate:dd/MM/yyyy} — Échéance : {invoice.DueDate:dd/MM/yyyy}</p>
              <table border="1" cellpadding="6" cellspacing="0" width="100%">
                <thead><tr><th>Produit</th><th>N° Lot</th><th>Quantité</th><th>Prix unitaire HT</th></tr></thead>
                <tbody>{rows}</tbody>
              </table>
              <h3>Total TTC : {invoice.TotalTtc.ToString("N0", culture)} XOF</h3>
            </body></html>
            """;
    }
}
