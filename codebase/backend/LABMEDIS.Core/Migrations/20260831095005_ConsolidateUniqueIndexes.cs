using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LABMEDIS.Core.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_Code",
                table: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_InternalLotNumber",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_ProductId_SupplierLotNumber",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_ShipmentNumber",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_OrderNumber",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_CreditNoteNumber",
                table: "CreditNotes");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Code",
                table: "StorageLocations",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_InternalLotNumber",
                table: "StockLots",
                column: "InternalLotNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ProductId_SupplierLotNumber",
                table: "StockLots",
                columns: new[] { "ProductId", "SupplierLotNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipmentNumber",
                table: "Shipments",
                column: "ShipmentNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_OrderNumber",
                table: "SaleOrders",
                column: "OrderNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_SessionNumber",
                table: "InventorySessions",
                column: "SessionNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns",
                column: "ReturnNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreditNoteNumber",
                table: "CreditNotes",
                column: "CreditNoteNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_Code",
                table: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_InternalLotNumber",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_ProductId_SupplierLotNumber",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_ShipmentNumber",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_OrderNumber",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InventorySessions_SessionNumber",
                table: "InventorySessions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_Currencies_Code",
                table: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_CreditNoteNumber",
                table: "CreditNotes");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Code",
                table: "StorageLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_InternalLotNumber",
                table: "StockLots",
                column: "InternalLotNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ProductId_SupplierLotNumber",
                table: "StockLots",
                columns: new[] { "ProductId", "SupplierLotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipmentNumber",
                table: "Shipments",
                column: "ShipmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_OrderNumber",
                table: "SaleOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreditNoteNumber",
                table: "CreditNotes",
                column: "CreditNoteNumber",
                unique: true);
        }
    }
}
