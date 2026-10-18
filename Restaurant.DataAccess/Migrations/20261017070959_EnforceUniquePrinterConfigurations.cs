using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniquePrinterConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing configuration: duplicates must be reviewed before adding the constraint.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [Printers]
                    GROUP BY [PrintStationId], [PrinterType], UPPER(LTRIM(RTRIM([PrinterName])))
                    HAVING COUNT(*) > 1
                )
                    THROW 51000, N'Duplicate printer configurations exist. Review Printers grouped by PrintStationId, PrinterType and trimmed PrinterName before applying this migration.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Printers_PrintStationId",
                table: "Printers");

            migrationBuilder.CreateIndex(
                name: "IX_Printers_PrintStationId_PrinterType_PrinterName",
                table: "Printers",
                columns: new[] { "PrintStationId", "PrinterType", "PrinterName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Printers_PrintStationId_PrinterType_PrinterName",
                table: "Printers");

            migrationBuilder.CreateIndex(
                name: "IX_Printers_PrintStationId",
                table: "Printers",
                column: "PrintStationId");
        }
    }
}
