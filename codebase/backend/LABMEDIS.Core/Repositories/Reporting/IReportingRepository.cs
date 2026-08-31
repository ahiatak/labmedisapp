using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;
using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;

namespace LABMEDIS.Core.Repositories.Reporting;

/// <summary>Cross-domain aggregate queries backing US11 (FR-068 à FR-075) — not tied to a single BaseEntity, so it does not extend BaseRepository&lt;T&gt;.</summary>
public interface IReportingRepository
{
    Task<decimal> GetTotalRevenueAsync(DateOnly? from, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<decimal> GetTotalMarginAsync(DateOnly? from, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<decimal> GetStockValueAsync(CancellationToken cancellationToken = default);

    Task<int> GetStockoutProductCountAsync(CancellationToken cancellationToken = default);

    Task<(int Available, int Reserved, int Quarantine, int Expired)> GetStockBreakdownAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLotEntity>> GetExpiringLotsAsync(int days, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(ProductEntity Product, DateTime? LastMovementAt)>> GetSlowMovingProductsAsync(int inactivityDays, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(CustomerEntity Customer, decimal Revenue)>> GetRevenueByCustomerAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(ProductEntity Product, decimal Revenue)>> GetRevenueByProductAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetReturnRatePercentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductPriceEntity>> GetLatestPricesForAllProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLotEntity>> GetQualityLotsAsync(CancellationToken cancellationToken = default);
}
