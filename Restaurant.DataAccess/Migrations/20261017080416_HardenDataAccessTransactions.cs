using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HardenDataAccessTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MigrationSql.DeferCompilation(LegacyLedgerRepair.Sql));

            migrationBuilder.CreateTable(
                name: "__TransactionReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___TransactionReceipts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Financial corrections and their audit history survive schema downgrades.
            migrationBuilder.DropTable(
                name: "__TransactionReceipts");
        }
    }
}
