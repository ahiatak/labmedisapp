using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LABMEDIS.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "text", nullable: false),
                    SaleOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerReturns_SaleOrders_SaleOrderId",
                        column: x => x.SaleOrderId,
                        principalTable: "SaleOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerReturnId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_CustomerReturns_CustomerReturnId1",
                        column: x => x.CustomerReturnId1,
                        principalTable: "CustomerReturns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalStockLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Disposition = table.Column<int>(type: "integer", nullable: false),
                    Motif = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnLines_CustomerReturns_CustomerReturnId",
                        column: x => x.CustomerReturnId,
                        principalTable: "CustomerReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnLines_SaleOrderLines_SaleOrderLineId",
                        column: x => x.SaleOrderLineId,
                        principalTable: "SaleOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnLines_StockLots_OriginalStockLotId",
                        column: x => x.OriginalStockLotId,
                        principalTable: "StockLots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreditNoteNumber",
                table: "CreditNotes",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CustomerReturnId1",
                table: "CreditNotes",
                column: "CustomerReturnId1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CustomerId",
                table: "CustomerReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_SaleOrderId",
                table: "CustomerReturns",
                column: "SaleOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_CustomerReturnId",
                table: "ReturnLines",
                column: "CustomerReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_OriginalStockLotId",
                table: "ReturnLines",
                column: "OriginalStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_SaleOrderLineId",
                table: "ReturnLines",
                column: "SaleOrderLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "ReturnLines");

            migrationBuilder.DropTable(
                name: "CustomerReturns");
        }
    }
}
