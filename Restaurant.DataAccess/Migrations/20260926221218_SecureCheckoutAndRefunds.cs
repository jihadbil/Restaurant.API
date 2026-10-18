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
                -- Allocate from gaps in the positive int range, including when int.MaxValue is in use.
                DECLARE @Duplicates TABLE ([Id] int PRIMARY KEY, [Ordinal] bigint);
                ;WITH Ranked AS (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrderNumber] ORDER BY [Id]) AS rn
                    FROM [Orders]
                )
                INSERT INTO @Duplicates SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) FROM Ranked WHERE rn > 1;
                ;WITH Used AS (
                    SELECT DISTINCT CAST([OrderNumber] AS bigint) AS n FROM [Orders] WHERE [OrderNumber] > 0
                    UNION SELECT CAST(0 AS bigint) UNION SELECT CAST(2147483648 AS bigint)
                ), Gaps AS (
                    SELECT n + 1 AS [Start], LEAD(n) OVER (ORDER BY n) - n - 1 AS Slots FROM Used
                ), Ranges AS (
                    SELECT [Start], Slots, SUM(Slots) OVER (ORDER BY [Start]) AS [LastOrdinal]
                    FROM Gaps WHERE Slots > 0
                )
                UPDATE o SET [OrderNumber] = CAST(r.[Start] + d.[Ordinal] - (r.[LastOrdinal] - r.Slots) - 1 AS int)
                FROM [Orders] o JOIN @Duplicates d ON d.[Id] = o.[Id]
                JOIN Ranges r ON d.[Ordinal] > r.[LastOrdinal] - r.Slots AND d.[Ordinal] <= r.[LastOrdinal];

                -- Multiple payments and partial refunds are reconciled by their totals, not by matching one amount.
                ;WITH NetPayments AS (
                    SELECT e.[OrderId], e.[CashboxId], e.[PaymentMethodId], MIN(e.[UserId]) AS [UserId],
                        SUM(CASE WHEN e.[EntryType] = 1 THEN e.[Amount] ELSE -e.[Amount] END) AS Remaining
                    FROM [CashDrawerEntries] e JOIN [Orders] o ON o.[Id] = e.[OrderId]
                    WHERE o.[OrderStatus] = 3 AND e.[EntryType] IN (1, 4)
                    GROUP BY e.[OrderId], e.[CashboxId], e.[PaymentMethodId]
                )
                INSERT INTO [CashDrawerEntries]
                    ([Date], [Amount], [EntryType], [Notes], [CashboxId], [PaymentMethodId], [OrderId], [UserId])
                SELECT SYSDATETIME(), payment.Remaining, 4,
                       N'استرداد تلقائي لطلب ملغى سابقاً', payment.[CashboxId],
                       payment.[PaymentMethodId], payment.[OrderId], payment.[UserId]
                FROM NetPayments payment WHERE payment.Remaining > 0;
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
            // Keep financial entries when reverting the schema, including refunds created after this migration.

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
