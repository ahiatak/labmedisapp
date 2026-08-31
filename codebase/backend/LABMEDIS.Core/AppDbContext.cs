using LABMEDIS.Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LABMEDIS.Core;

/// <summary>
/// Application database context. Entities are added domain-by-domain as each user story is
/// implemented (see specs/001-gestion-depositaire-pharmaceutique/data-model.md). A global
/// query filter excluding IsDeleted = true is applied to every BaseEntity-derived type
/// (Principle III of the constitution — soft delete exclusif).
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();

    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<TherapeuticClass> TherapeuticClasses => Set<TherapeuticClass>();

    public DbSet<PharmaceuticalForm> PharmaceuticalForms => Set<PharmaceuticalForm>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductPackaging> ProductPackagings => Set<ProductPackaging>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerProductPrice> CustomerProductPrices => Set<CustomerProductPrice>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermissionException> UserPermissionExceptions => Set<UserPermissionException>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<PurchaseOrderStatusHistory> PurchaseOrderStatusHistories => Set<PurchaseOrderStatusHistory>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();

    public DbSet<ImportCost> ImportCosts => Set<ImportCost>();

    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();

    public DbSet<StockLot> StockLots => Set<StockLot>();

    public DbSet<StockLotLocation> StockLotLocations => Set<StockLotLocation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<PricingProfile> PricingProfiles => Set<PricingProfile>();

    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public DbSet<SaleOrder> SaleOrders => Set<SaleOrder>();

    public DbSet<SaleOrderLine> SaleOrderLines => Set<SaleOrderLine>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<CustomerReturn> CustomerReturns => Set<CustomerReturn>();

    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();

    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

    public DbSet<InventorySession> InventorySessions => Set<InventorySession>();

    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();

    public DbSet<ForecastParameter> ForecastParameters => Set<ForecastParameter>();

    public DbSet<SupplierLeadTime> SupplierLeadTimes => Set<SupplierLeadTime>();

    public DbSet<ForecastCalculation> ForecastCalculations => Set<ForecastCalculation>();

    public DbSet<ReorderSuggestion> ReorderSuggestions => Set<ReorderSuggestion>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();

    public DbSet<RegulatoryAttachment> RegulatoryAttachments => Set<RegulatoryAttachment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().HasIndex(p => p.Designation).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<Product>().HasIndex(p => p.CodeCip).IsUnique().HasFilter("\"IsDeleted\" = false AND \"CodeCip\" IS NOT NULL");
        builder.Entity<Supplier>().HasIndex(s => s.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<Customer>().HasIndex(c => c.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<Permission>().HasIndex(p => p.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<RefreshToken>().HasIndex(t => t.Token).IsUnique();
        builder.Entity<Currency>().HasIndex(c => c.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        // Business document numbers (PO-.../SH-.../SO-.../INV-.../RET-.../AV-...) and location
        // codes are unique among active rows only (partial unique index, Principle III) —
        // consistent with the Product/Supplier/Customer/Permission indexes above.
        builder.Entity<PurchaseOrder>().HasIndex(o => o.OrderNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<Shipment>().HasIndex(s => s.ShipmentNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<SaleOrder>().HasIndex(o => o.OrderNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<CustomerReturn>().HasIndex(r => r.ReturnNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<CreditNote>().HasIndex(c => c.CreditNoteNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<StorageLocation>().HasIndex(l => l.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<InventorySession>().HasIndex(s => s.SessionNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<StockLot>().HasIndex(l => l.InternalLotNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<StockLot>().HasIndex(l => new { l.ProductId, l.SupplierLotNumber }).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Entity<StockLot>().ToTable(t => t.HasCheckConstraint(
            "CK_StockLots_RemainingWithinInitial", "\"RemainingQuantity\" >= 0 AND \"RemainingQuantity\" <= \"InitialQuantity\""));
        builder.Entity<StockLot>().ToTable(t => t.HasCheckConstraint(
            "CK_StockLots_ReservedWithinRemaining", "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"RemainingQuantity\""));

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // Global soft-delete query filter: IsDeleted = false, applied to every
            // BaseEntity-derived entity registered in the model.
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
            var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
