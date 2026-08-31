using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LABMEDIS.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAndWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierLotNumber = table.Column<string>(type: "text", nullable: false),
                    InternalLotNumber = table.Column<string>(type: "text", nullable: false),
                    ReceptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ManufacturingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InitialQuantity = table.Column<int>(type: "integer", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCostCfa = table.Column<decimal>(type: "numeric", nullable: false),
                    PricingProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    QualityStatus = table.Column<int>(type: "integer", nullable: false),
                    QuarantineReason = table.Column<string>(type: "text", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLots", x => x.Id);
                    table.CheckConstraint("CK_StockLots_RemainingWithinInitial", "\"RemainingQuantity\" >= 0 AND \"RemainingQuantity\" <= \"InitialQuantity\"");
                    table.CheckConstraint("CK_StockLots_ReservedWithinRemaining", "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"RemainingQuantity\"");
                    table.ForeignKey(
                        name: "FK_StockLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDocumentType = table.Column<int>(type: "integer", nullable: true),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLotLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLotLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLotLocations_StockLots_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLotLocations_StorageLocations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockLotLocations_StockLotId",
                table: "StockLotLocations",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLotLocations_StorageLocationId",
                table: "StockLotLocations",
                column: "StorageLocationId");

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
                name: "IX_StorageLocations_Code",
                table: "StorageLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_WarehouseId",
                table: "StorageLocations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockLotLocations");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "StockLots");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
