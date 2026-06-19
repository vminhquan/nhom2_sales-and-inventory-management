using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom2.Infrastructure.Data;

#nullable disable

namespace nhom2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619144500_AddOrderItemVariants")]
public partial class AddOrderItemVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ProductVariantId", table: "OrderItems", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ProductVariantColorId", table: "OrderItems", nullable: true);
        migrationBuilder.AddColumn<string>(name: "VariantName", table: "OrderItems", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ColorName", table: "OrderItems", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Sku", table: "OrderItems", maxLength: 120, nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_ProductId_ProductVariantId_ProductVariantColorId",
            table: "OrderItems",
            columns: new[] { "ProductId", "ProductVariantId", "ProductVariantColorId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_OrderItems_ProductId_ProductVariantId_ProductVariantColorId", table: "OrderItems");
        migrationBuilder.DropColumn(name: "ProductVariantId", table: "OrderItems");
        migrationBuilder.DropColumn(name: "ProductVariantColorId", table: "OrderItems");
        migrationBuilder.DropColumn(name: "VariantName", table: "OrderItems");
        migrationBuilder.DropColumn(name: "ColorName", table: "OrderItems");
        migrationBuilder.DropColumn(name: "Sku", table: "OrderItems");
    }
}
