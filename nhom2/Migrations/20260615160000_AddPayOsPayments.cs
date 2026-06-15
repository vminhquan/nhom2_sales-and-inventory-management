using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using nhom2.Infrastructure.Data;

#nullable disable

namespace nhom2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260615160000_AddPayOsPayments")]
public class AddPayOsPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OrderId = table.Column<int>(type: "integer", nullable: false),
                OrderCode = table.Column<long>(type: "bigint", nullable: false),
                CheckoutUrl = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PayOsTransactionReference = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentTransactions", value => value.Id);
                table.ForeignKey(
                    name: "FK_PaymentTransactions_Orders_OrderId",
                    column: value => value.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_OrderCode",
            table: "PaymentTransactions",
            column: "OrderCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_OrderId",
            table: "PaymentTransactions",
            column: "OrderId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentTransactions");
    }
}
