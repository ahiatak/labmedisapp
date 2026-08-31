using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IReportingService
{
    Task<DirectionDashboardResponse> GetDirectionDashboardAsync(CancellationToken cancellationToken = default);

    Task<StockReportResponse> GetStockReportAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpiringLotReportLine>> GetExpiringLotsAsync(int days, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlowMovingProductReportLine>> GetSlowMovingProductsAsync(CancellationToken cancellationToken = default);

    Task<SalesReportResponse> GetSalesReportAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingReportLine>> GetPricingReportAsync(CancellationToken cancellationToken = default);

    Task<QualityReportResponse> GetQualityReportAsync(CancellationToken cancellationToken = default);

    /// <summary>Renders the requested report as a downloadable file (FR-074).</summary>
    Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(ExportReportRequest request, CancellationToken cancellationToken = default);
}
