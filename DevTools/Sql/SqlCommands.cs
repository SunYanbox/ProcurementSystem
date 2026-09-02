using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;

namespace DevTools.Sql;

// Executes a raw SQLite statement through the same connection EF Core uses,
// so ad-hoc inspection does not require a separate database tool.
internal static class SqlCommands
{
    public static async Task<int> RunAsync(ProcurementDbContext db, string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            PrintHelp();
            return 0;
        }

        sql = TryTranslateShorthand(sql) ?? sql;

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();

            if (reader.FieldCount == 0)
            {
                var affected = reader.RecordsAffected;
                Console.WriteLine($"Executed SQL, affected rows: {affected}.");
                return 0;
            }

            var headers = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToArray();
            Console.WriteLine(string.Join("\t", headers));

            var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var values = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL")
                    .ToArray();
                Console.WriteLine(string.Join("\t", values));
                rowCount++;
            }

            Console.WriteLine($"({rowCount} row(s))");
            return 0;
        }
        catch (DbException ex)
        {
            Console.Error.WriteLine($"SQL error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    // SQLite has no SHOW statements, so translate a few friendlier shorthands
    // into the sqlite_master queries that expose the same information.
    private static string? TryTranslateShorthand(string sql)
    {
        var normalized = sql.Trim().TrimEnd(';').Trim().ToLowerInvariant();

        return normalized switch
        {
            "show databases" => "PRAGMA database_list;",
            "show tables" => "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';",
            "show views" => "SELECT name FROM sqlite_master WHERE type = 'view';",
            _ => null
        };
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            SQL usage:
              dotnet run --project DevTools sql "<SQL statement>" [dbPath]

            Simple commands:
              show databases;
              show tables;
              show views;

            Common statements:
              List all tables:
                SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';

              List all views:
                SELECT name FROM sqlite_master WHERE type = 'view';

              Select rows:
                SELECT Id, WorkId, Name FROM Users;
                SELECT * FROM ProcurementRequests WHERE Status = 'Pending';
                SELECT * FROM StockTransactions ORDER BY CreatedAt DESC;

              Update rows:
                UPDATE Users SET Name = '新名字' WHERE WorkId = 'A001';
                UPDATE Items SET IsActive = 0 WHERE Id = 1;
                UPDATE Items SET Price = 29.9 WHERE Name = 'A4复印纸';
            """);
    }
}
