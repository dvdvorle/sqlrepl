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
    public bool Reconnected { get; init; }
}

/// <summary>
/// Abstraction for checking and restoring connection state, used by ReconnectingQueryExecutor.
/// ConnectionManager implements this directly.
/// </summary>
public interface IConnectionChecker
{
    bool IsConnected { get; }
    bool CanReconnect { get; }
    Task ReconnectAsync();
}

/// <summary>
/// Decorator that retries a failed query after reconnecting, when AutoReconnect is enabled
/// and the failure was caused by a dropped connection.
/// </summary>
public class ReconnectingQueryExecutor : IQueryExecutor
{
    private readonly IQueryExecutor _inner;
    private readonly IConnectionChecker _checker;
    private readonly ReplSettings _settings;

    public ReconnectingQueryExecutor(IQueryExecutor inner, IConnectionChecker checker, ReplSettings settings)
    {
        _inner = inner;
        _checker = checker;
        _settings = settings;
    }

    public async Task<QueryResult> ExecuteAsync(string sql)
    {
        try
        {
            return await _inner.ExecuteAsync(sql);
        }
        catch (Exception ex) when (_settings.AutoReconnect && !_checker.IsConnected && _checker.CanReconnect)
        {
            try
            {
                await _checker.ReconnectAsync();
            }
            catch
            {
                throw ex;
            }

            var result = await _inner.ExecuteAsync(sql);
            return result with { Reconnected = true };
        }
    }

    public async Task<StreamingQueryResult> ExecuteStreamAsync(string sql)
    {
        try
        {
            return await _inner.ExecuteStreamAsync(sql);
        }
        catch (Exception ex) when (_settings.AutoReconnect && !_checker.IsConnected && _checker.CanReconnect)
        {
            try
            {
                await _checker.ReconnectAsync();
            }
            catch
            {
                throw ex;
            }

            var result = await _inner.ExecuteStreamAsync(sql);
            result.Reconnected = true;
            return result;
        }
    }
}

public interface IQueryExecutor
{
    Task<QueryResult> ExecuteAsync(string sql);
    Task<StreamingQueryResult> ExecuteStreamAsync(string sql);
}

public class QueryExecutor : IQueryExecutor
{
    private readonly ConnectionManager _connectionManager;

    public QueryExecutor(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public static bool IsQuery(string sql)
    {
        var trimmedUpper = sql.TrimStart().ToUpperInvariant();
        return trimmedUpper.StartsWith("SELECT") ||
               trimmedUpper.StartsWith("WITH") ||
               trimmedUpper.StartsWith("SHOW") ||
               trimmedUpper.StartsWith("DESCRIBE") ||
               trimmedUpper.StartsWith("DESC ");
    }

    public async Task<QueryResult> ExecuteAsync(string sql)
    {
        var conn = _connectionManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var sw = Stopwatch.StartNew();

        if (IsQuery(sql))
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

    public async Task<StreamingQueryResult> ExecuteStreamAsync(string sql)
    {
        var conn = _connectionManager.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var sw = Stopwatch.StartNew();
        var reader = await cmd.ExecuteReaderAsync();

        var columnNames = new string[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
            columnNames[i] = reader.GetName(i);

        return new StreamingQueryResult(reader, cmd, columnNames, sw);
    }
}
