using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SecureCheckoutAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                ;WITH NumberedOrders AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrderNumber] ORDER BY [Id]) AS [RowNumber]
                    FROM [Orders]
                )
                UPDATE orders
                SET [OrderNumber] = 1000000000 + orders.[Id]
                FROM [Orders] AS orders
                INNER JOIN NumberedOrders AS numbered ON numbered.[Id] = orders.[Id]
                WHERE numbered.[RowNumber] > 1;

                INSERT INTO [CashDrawerEntries]
                    ([Date], [Amount], [EntryType], [Notes], [CashboxId], [PaymentMethodId], [OrderId], [UserId])
                SELECT SYSDATETIME(), payment.[Amount], 4,
                       N'استرداد تلقائي لطلب ملغى سابقاً', payment.[CashboxId],
                       payment.[PaymentMethodId], payment.[OrderId], payment.[UserId]
                FROM [CashDrawerEntries] AS payment
                INNER JOIN [Orders] AS orders ON orders.[Id] = payment.[OrderId]
                WHERE payment.[EntryType] = 1
                  AND orders.[OrderStatus] = 3
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [CashDrawerEntries] AS refund
                      WHERE refund.[EntryType] = 4
                        AND refund.[OrderId] = payment.[OrderId]
                        AND refund.[CashboxId] = payment.[CashboxId]
                        AND (refund.[PaymentMethodId] = payment.[PaymentMethodId]
                             OR (refund.[PaymentMethodId] IS NULL AND payment.[PaymentMethodId] IS NULL))
                        AND refund.[Amount] = payment.[Amount]
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [CashDrawerEntries] WHERE [EntryType] = 4;");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientRequestId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Orders");
        }
    }
}
