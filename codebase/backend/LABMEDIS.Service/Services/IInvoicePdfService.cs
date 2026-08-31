namespace LABMEDIS.Service.Services;

public interface IInvoicePdfService
{
    /// <summary>Renders the invoice for a sale order as a PDF — every line shows its lot number (FR-058).</summary>
    Task<byte[]> GenerateInvoicePdfAsync(Guid saleOrderId, CancellationToken cancellationToken = default);
}
