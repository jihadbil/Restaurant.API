using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixCashLedgerAndShiftClosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_UserId",
                table: "CashDrawerEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsCash",
                table: "PaymentMethods",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AffectsCash",
                table: "CashDrawerEntries",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CashShiftId",
                table: "CashDrawerEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "CashDrawerEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDrawer",
                table: "Cashboxes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Preserve legacy custom methods; classify established electronic methods once.
            // Future accounting uses the explicit IsCash flag and the immutable entry snapshot.
            migrationBuilder.Sql(MigrationSql.DeferCompilation("""
                UPDATE [PaymentMethods] SET [IsCash] = 0
                WHERE LOWER(LTRIM(RTRIM([Name]))) IN
                    (N'بطاقة', N'بطاقة مصرفية', N'بطاقة بنكية', N'بطاقة ائتمان', N'فيزا', N'ماستر كارد',
                     N'تداول', N'سداد', N'تحويل بنكي', N'تحويل مصرفي', N'card', N'credit card',
                     N'debit card', N'visa', N'mastercard', N'master card', N'bank', N'bank transfer');
                UPDATE e SET [AffectsCash] = p.[IsCash]
                FROM [CashDrawerEntries] e JOIN [PaymentMethods] p ON p.[Id] = e.[PaymentMethodId]
                WHERE e.[EntryType] IN (1, 4);
                UPDATE [Cashboxes] SET [IsDrawer] = 1
                WHERE [Name] LIKE N'%درج%' OR LOWER([Name]) LIKE N'%drawer%'
                   OR LOWER([Name]) = N'pos' OR LOWER([Name]) LIKE N'pos %' OR LOWER([Name]) LIKE N'% pos'
                   OR LOWER([Name]) LIKE N'%point of sale%';
                """));

            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceCashboxId = table.Column<int>(type: "int", nullable: false),
                    TargetCashboxId = table.Column<int>(type: "int", nullable: false),
                    PreviousShiftId = table.Column<int>(type: "int", nullable: false),
                    LastEntryId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashShifts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashShifts_Cashboxes_SourceCashboxId",
                        column: x => x.SourceCashboxId,
                        principalTable: "Cashboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashShifts_Cashboxes_TargetCashboxId",
                        column: x => x.TargetCashboxId,
                        principalTable: "Cashboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_CashShiftId",
                table: "CashDrawerEntries",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_UserId_ClientRequestId",
                table: "CashDrawerEntries",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_SourceCashboxId_PreviousShiftId",
                table: "CashShifts",
                columns: new[] { "SourceCashboxId", "PreviousShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_TargetCashboxId",
                table: "CashShifts",
                column: "TargetCashboxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_UserId_ClientRequestId",
                table: "CashShifts",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CashDrawerEntries_CashShifts_CashShiftId",
                table: "CashDrawerEntries",
                column: "CashShiftId",
                principalTable: "CashShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashDrawerEntries_CashShifts_CashShiftId",
                table: "CashDrawerEntries");

            migrationBuilder.DropTable(
                name: "CashShifts");

            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_CashShiftId",
                table: "CashDrawerEntries");

            migrationBuilder.DropIndex(
                name: "IX_CashDrawerEntries_UserId_ClientRequestId",
                table: "CashDrawerEntries");

            migrationBuilder.DropColumn(
                name: "IsCash",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "AffectsCash",
                table: "CashDrawerEntries");

            migrationBuilder.DropColumn(
                name: "CashShiftId",
                table: "CashDrawerEntries");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "CashDrawerEntries");

            migrationBuilder.DropColumn(
                name: "IsDrawer",
                table: "Cashboxes");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerEntries_UserId",
                table: "CashDrawerEntries",
                column: "UserId");
        }
    }
}
