using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stock");

            migrationBuilder.CreateTable(
                name: "Stocks",
                schema: "stock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The product identifier for this stock entry"),
                    Quantity = table.Column<int>(type: "integer", nullable: false, comment: "Total quantity of the product in stock"),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Quantity reserved for pending orders"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the stock entry was created"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When the stock entry was last updated")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_CreatedAt",
                schema: "stock",
                table: "Stocks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId",
                schema: "stock",
                table: "Stocks",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId_Quantity",
                schema: "stock",
                table: "Stocks",
                columns: new[] { "ProductId", "Quantity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stocks",
                schema: "stock");
        }
    }
}
