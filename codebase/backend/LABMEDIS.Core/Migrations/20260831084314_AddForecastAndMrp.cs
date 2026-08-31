using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LABMEDIS.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastAndMrp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalcDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AvgDailyConsumption = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    DaysOfStockRemaining = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalLeadDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastCalculations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyStockDays = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionWindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ManualEstimatedConsumption = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastParameters_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReorderSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OrderDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    SuggestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConvertedPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReorderSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReorderSuggestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierLeadTimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufactureDays = table.Column<int>(type: "integer", nullable: false),
                    TransportDays = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierLeadTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierLeadTimes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierLeadTimes_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastCalculations_ProductId",
                table: "ForecastCalculations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastParameters_ProductId",
                table: "ForecastParameters",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSuggestions_ProductId",
                table: "ReorderSuggestions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLeadTimes_ProductId",
                table: "SupplierLeadTimes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLeadTimes_SupplierId",
                table: "SupplierLeadTimes",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastCalculations");

            migrationBuilder.DropTable(
                name: "ForecastParameters");

            migrationBuilder.DropTable(
                name: "ReorderSuggestions");

            migrationBuilder.DropTable(
                name: "SupplierLeadTimes");
        }
    }
}
