using System.Data;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;

namespace SqlRepl;

public record QueryResult
{
    public DataTable? Data { get; init; }
    public int RowsAffected { get; init; }
    public TimeSpan Elapsed { get; init; }
    public bool IsQuery { get; init; }
}

public class QueryExecutor
{
    private readonly ConnectionManager _connectionManager;

    public QueryExecutor(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task<QueryResult> ExecuteAsync(string sql)
    {
        var conn = _connectionManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var sw = Stopwatch.StartNew();

        // Determine if it's a SELECT/WITH or a DML/DDL statement
        var trimmedUpper = sql.TrimStart().ToUpperInvariant();
        var isQuery = trimmedUpper.StartsWith("SELECT") ||
                      trimmedUpper.StartsWith("WITH") ||
                      trimmedUpper.StartsWith("SHOW") ||
                      trimmedUpper.StartsWith("DESCRIBE") ||
                      trimmedUpper.StartsWith("DESC ");

        if (isQuery)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();
            table.Load(reader);
            sw.Stop();

            return new QueryResult
            {
                Data = table,
                RowsAffected = table.Rows.Count,
                Elapsed = sw.Elapsed,
                IsQuery = true
            };
        }
        else
        {
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            sw.Stop();

            return new QueryResult
            {
                RowsAffected = rowsAffected,
                Elapsed = sw.Elapsed,
                IsQuery = false
            };
        }
    }
}
