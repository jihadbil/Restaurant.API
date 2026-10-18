using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAccountingAndAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders");

            migrationBuilder.Sql(MigrationSql.DeferCompilation(RequestKeyCompatibility.RestoreOnUpgrade));

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_CashboxId",
                table: "CashDrawerEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PaymentMethods",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "Orders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserName",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryIdSnapshot",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderDiscountShare",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductBarCodeSnapshot",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodNameSnapshot",
                table: "CashDrawerEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders",
                column: "ClientRequestId",
                filter: "[ClientRequestId] IS NOT NULL");

            // Backfill from the best available historical data without changing posted money.
            migrationBuilder.Sql(MigrationSql.DeferCompilation("""
                UPDATE i SET [ProductNameSnapshot] = p.[Name], [ProductBarCodeSnapshot] = p.[BarCode],
                    [CategoryIdSnapshot] = p.[CategoryId], [CategoryNameSnapshot] = c.[Name]
                FROM [OrderItems] i JOIN [Products] p ON p.[Id] = i.[ProductId]
                JOIN [Categories] c ON c.[Id] = p.[CategoryId];
                UPDATE e SET [PaymentMethodNameSnapshot] = p.[Name]
                FROM [CashDrawerEntries] e JOIN [PaymentMethods] p ON p.[Id] = e.[PaymentMethodId];
                -- Fix defaults created by older fresh-install seeders; posted entries remain immutable.
                UPDATE [PaymentMethods] SET [IsCash] = 0 WHERE [Name] = N'بطاقة';
                ;WITH Shares AS (
                    SELECT i.[Id], o.[Discount],
                        SUM(i.[Total]) OVER (PARTITION BY i.[OrderId] ORDER BY i.[Id] ROWS UNBOUNDED PRECEDING) AS Cumulative,
                        SUM(i.[Total]) OVER (PARTITION BY i.[OrderId]) AS Gross, i.[Total]
                    FROM [OrderItems] i JOIN [Orders] o ON o.[Id] = i.[OrderId]
                )
                UPDATE i SET [OrderDiscountShare] = CASE WHEN s.Gross = 0 THEN 0 ELSE
                    ROUND(s.[Discount] * (s.Cumulative / s.Gross), 2)
                    - ROUND(s.[Discount] * ((s.Cumulative - s.[Total]) / s.Gross), 2) END
                FROM [OrderItems] i JOIN Shares s ON s.[Id] = i.[Id];
                UPDATE o SET [CancelledAt] = COALESCE((SELECT MIN(e.[Date]) FROM [CashDrawerEntries] e
                    WHERE e.[OrderId] = o.[Id] AND e.[EntryType] = 4), o.[Date])
                FROM [Orders] o WHERE o.[OrderStatus] = 3;
                """));

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Date",
                table: "Orders",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_ClientRequestId",
                table: "Orders",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_CashboxId_Id",
                table: "CashDrawerEntries",
                columns: new[] { "CashboxId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_Date_EntryType",
                table: "CashDrawerEntries",
                columns: new[] { "Date", "EntryType" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MigrationSql.DeferCompilation(RequestKeyCompatibility.PrepareDowngrade));

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Date",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_ClientRequestId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_CashboxId_Id",
                table: "CashDrawerEntries");

            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_Date_EntryType",
                table: "CashDrawerEntries");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CategoryIdSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CategoryNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderDiscountShare",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductBarCodeSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PaymentMethodNameSnapshot",
                table: "CashDrawerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_CashboxId",
                table: "CashDrawerEntries",
                column: "CashboxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
