using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Restaurant.DataAccess;
using Restaurant.Models;
using Restaurant.Models.Enums;
using System.Text.RegularExpressions;
using Xunit;

namespace Restaurant.Services.Tests;

public class ModelMigrationIntegrationTests
{
    [SqlServerFact]
    public async Task PrinterMigrationPreservesExistingDuplicatesAndEnforcesUniquenessAfterReview()
    {
        const string previousMigration = "20261015222254_ProtectAccountingAndAuthorization";
        const string newMigration = "20261017070959_EnforceUniquePrinterConfigurations";
        var databaseName = "RestaurantModelFixes_" + Guid.NewGuid().ToString("N");
        string Connection(string database) => new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("RESTAURANT_TEST_SQLSERVER"))
            { InitialCatalog = database, ConnectTimeout = 5 }.ConnectionString;
        var created = false;
        try
        {
            await using (var connection = new SqlConnection(Connection("master")))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
                await command.ExecuteNonQueryAsync();
                created = true;
            }

            await using var db = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseSqlServer(Connection(databaseName)).Options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(previousMigration);
            var station = new PrintStation { Name = "Test kitchen" };
            db.PrintStations.Add(station);
            await db.SaveChangesAsync();
            var original = new Printer { Name = "Original", PrinterName = "Queue", PrintStationId = station.Id,
                PrinterType = PrinterType.Kitchen };
            var duplicate = new Printer { Name = "Duplicate label", PrinterName = "Queue", PrintStationId = station.Id,
                PrinterType = PrinterType.Kitchen };
            db.Printers.AddRange(original, duplicate);
            await db.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(newMigration));
            Assert.Equal(51000, error.Number);
            Assert.Equal(2, await db.Printers.CountAsync());
            Assert.DoesNotContain(newMigration, await db.Database.GetAppliedMigrationsAsync());

            // Simulate the operator choosing which redundant configuration to remove in this test database.
            db.Printers.Remove(duplicate);
            await db.SaveChangesAsync();
            await migrator.MigrateAsync(newMigration);
            Assert.Contains(newMigration, await db.Database.GetAppliedMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            db.Printers.Add(new Printer { Name = "Rejected duplicate", PrinterName = "Queue",
                PrintStationId = station.Id, PrinterType = PrinterType.Kitchen });
            var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.True(conflict.InnerException is SqlException { Number: 2601 or 2627 });
            db.ChangeTracker.Clear();
            Assert.Equal("Original", (await db.Printers.SingleAsync()).Name);

            await migrator.MigrateAsync(previousMigration);
            Assert.Equal("Original", (await db.Printers.SingleAsync()).Name);
            db.Printers.Add(new Printer { Name = "Allowed after rollback", PrinterName = "Queue",
                PrintStationId = station.Id, PrinterType = PrinterType.Kitchen });
            await db.SaveChangesAsync();
            Assert.Equal(2, await db.Printers.CountAsync());
        }
        finally
        {
            if (created)
            {
                if (!Regex.IsMatch(databaseName, "^RestaurantModelFixes_[a-f0-9]{32}$"))
                    throw new InvalidOperationException("Unsafe test database cleanup target.");
                SqlConnection.ClearAllPools();
                await using var connection = new SqlConnection(Connection("master"));
                await connection.OpenAsync();
                await using var command = new SqlCommand(
                    $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
