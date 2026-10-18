namespace Restaurant.DataAccess.Migrations;

internal static class MigrationSql
{
    // Idempotent scripts can put ALTER TABLE and its backfill in one batch. Compile the backfill
    // only after the preceding schema changes have executed, including on repeat deployments.
    internal static string DeferCompilation(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";
}
