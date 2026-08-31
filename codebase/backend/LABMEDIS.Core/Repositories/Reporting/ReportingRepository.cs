using LABMEDIS.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;
using InvoiceEntity = LABMEDIS.Core.Models.Entities.Invoice;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;
using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;
using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;

namespace LABMEDIS.Core.Repositories.Reporting;

public class ReportingRepository(AppDbContext context) : IReportingRepository
{
    protected AppDbContext Context { get; } = context;

    public async Task<decimal> GetTotalRevenueAsync(DateOnly? from, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<InvoiceEntity>().Where(i => i.Status != InvoiceStatus.Annulee).AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= from.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        return await query.SumAsync(i => (decimal?)i.TotalHt, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetTotalMarginAsync(DateOnly? from, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<InvoiceLine>()
            .Include(l => l.Invoice)
            .Include(l => l.StockLot)
            .Where(l => l.Invoice!.Status != InvoiceStatus.Annulee)
            .AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(l => l.Invoice!.InvoiceDate >= from.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(l => l.Invoice!.InvoiceDate <= toDate.Value);
        }

        var lines = await query.ToListAsync(cancellationToken);
        return lines.Sum(l => (l.UnitPriceHt - (l.StockLot?.UnitCostCfa ?? 0m)) * l.Quantity);
    }

    public async Task<decimal> GetStockValueAsync(CancellationToken cancellationToken = default) =>
        await Context.Set<StockLotEntity>()
            .Where(l => l.QualityStatus == QualityStatus.Libere)
            .SumAsync(l => (decimal?)(l.RemainingQuantity * l.UnitCostCfa), cancellationToken) ?? 0m;

    public async Task<int> GetStockoutProductCountAsync(CancellationToken cancellationToken = default)
    {
        var productIdsWithStock = await Context.Set<StockLotEntity>()
            .Where(l => l.QualityStatus == QualityStatus.Libere && l.RemainingQuantity > l.ReservedQuantity)
            .Select(l => l.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await Context.Set<ProductEntity>().CountAsync(p => p.IsActive && !productIdsWithStock.Contains(p.Id), cancellationToken);
    }

    public async Task<(int Available, int Reserved, int Quarantine, int Expired)> GetStockBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var lots = await Context.Set<StockLotEntity>().ToListAsync(cancellationToken);
        var available = lots.Where(l => l.QualityStatus == QualityStatus.Libere).Sum(l => l.AvailableQuantity);
        var reserved = lots.Sum(l => l.ReservedQuantity);
        var quarantine = lots.Where(l => l.QualityStatus == QualityStatus.EnQuarantaine).Sum(l => l.RemainingQuantity);
        var expired = lots.Where(l => l.QualityStatus == QualityStatus.Perime).Sum(l => l.RemainingQuantity);
        return (available, reserved, quarantine, expired);
    }

    public async Task<IReadOnlyList<StockLotEntity>> GetExpiringLotsAsync(int days, CancellationToken cancellationToken = default)
    {
        var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        return await Context.Set<StockLotEntity>()
            .Include(l => l.Product)
            .Where(l => l.RemainingQuantity > 0 && l.ExpiryDate <= threshold && l.QualityStatus != QualityStatus.Perime && l.QualityStatus != QualityStatus.Detruit)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(ProductEntity Product, DateTime? LastMovementAt)>> GetSlowMovingProductsAsync(int inactivityDays, CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddDays(-inactivityDays);
        var products = await Context.Set<ProductEntity>().Where(p => p.IsActive).ToListAsync(cancellationToken);
        var result = new List<(ProductEntity, DateTime?)>();

        foreach (var product in products)
        {
            var lastMovement = await Context.Set<StockMovementEntity>()
                .Where(m => m.MovementType == StockMovementType.Vente && Context.Set<StockLotEntity>().Any(l => l.Id == m.StockLotId && l.ProductId == product.Id))
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastMovement is null || lastMovement < threshold)
            {
                result.Add((product, lastMovement));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<(CustomerEntity Customer, decimal Revenue)>> GetRevenueByCustomerAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await Context.Set<InvoiceEntity>().Include(i => i.Customer).Where(i => i.Status != InvoiceStatus.Annulee).ToListAsync(cancellationToken);
        return invoices
            .GroupBy(i => i.Customer!)
            .Select(g => (g.Key, g.Sum(i => i.TotalHt)))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    public async Task<IReadOnlyList<(ProductEntity Product, decimal Revenue)>> GetRevenueByProductAsync(CancellationToken cancellationToken = default)
    {
        var lines = await Context.Set<InvoiceLine>()
            .Include(l => l.Product)
            .Include(l => l.Invoice)
            .Where(l => l.Invoice!.Status != InvoiceStatus.Annulee)
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(l => l.Product!)
            .Select(g => (g.Key, g.Sum(l => l.UnitPriceHt * l.Quantity)))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    public async Task<decimal> GetReturnRatePercentAsync(CancellationToken cancellationToken = default)
    {
        var totalSold = await Context.Set<InvoiceLine>().SumAsync(l => (int?)l.Quantity, cancellationToken) ?? 0;
        if (totalSold == 0)
        {
            return 0m;
        }

        var totalReturned = await Context.Set<ReturnLine>().SumAsync(l => (int?)l.Quantity, cancellationToken) ?? 0;
        return Math.Round((decimal)totalReturned / totalSold * 100m, 2);
    }

    public async Task<IReadOnlyList<ProductPriceEntity>> GetLatestPricesForAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var allPrices = await Context.Set<ProductPriceEntity>().Include(p => p.Product).OrderByDescending(p => p.EffectiveDate).ToListAsync(cancellationToken);
        return allPrices.GroupBy(p => p.ProductId).Select(g => g.First()).ToList();
    }

    public async Task<IReadOnlyList<StockLotEntity>> GetQualityLotsAsync(CancellationToken cancellationToken = default) =>
        await Context.Set<StockLotEntity>()
            .Include(l => l.Product)
            .Where(l => l.QualityStatus == QualityStatus.EnQuarantaine || l.QualityStatus == QualityStatus.NonConforme || l.QualityStatus == QualityStatus.SuspecteFalsifie)
            .ToListAsync(cancellationToken);
}
