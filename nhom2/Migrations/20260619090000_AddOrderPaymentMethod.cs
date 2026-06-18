using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom2.Infrastructure.Data;

#nullable disable

namespace nhom2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619090000_AddOrderPaymentMethod")]
public partial class AddOrderPaymentMethod : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaymentMethod",
            table: "Orders",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Cash");

        migrationBuilder.Sql("""
            UPDATE "Orders" AS o
            SET "PaymentMethod" = 'PayOS'
            WHERE EXISTS (
                SELECT 1
                FROM "PaymentTransactions" AS p
                WHERE p."OrderId" = o."Id"
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentMethod",
            table: "Orders");
    }
}
