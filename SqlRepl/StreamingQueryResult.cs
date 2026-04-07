using System.Data.Common;
using System.Diagnostics;

namespace SqlRepl;

/// <summary>
/// A query result that streams rows page-by-page from an open DbDataReader,
/// instead of buffering the entire result set into memory.
/// </summary>
public sealed class StreamingQueryResult : IAsyncDisposable
{
    private readonly DbDataReader _reader;
    private readonly DbCommand? _command;
    private readonly Stopwatch _stopwatch;
    private bool _exhausted;

    public StreamingQueryResult(DbDataReader reader, DbCommand? command, string[] columnNames, Stopwatch stopwatch)
    {
        _reader = reader;
        _command = command;
        _stopwatch = stopwatch;
        ColumnNames = columnNames;
    }

    public string[] ColumnNames { get; }
    public bool Reconnected { get; set; }
    public int TotalRowsRead { get; private set; }
    public bool HasMore => !_exhausted;
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void StopTimer() => _stopwatch.Stop();

    /// <summary>
    /// Reads up to <paramref name="pageSize"/> rows from the underlying reader.
    /// Returns fewer rows (or empty) when the result set is exhausted.
    /// </summary>
    public async Task<List<object[]>> ReadPageAsync(int pageSize)
    {
        if (_exhausted)
            return [];

        var rows = new List<object[]>(pageSize);
        for (var i = 0; i < pageSize; i++)
        {
            if (!await _reader.ReadAsync())
            {
                _exhausted = true;
                break;
            }

            var values = new object[ColumnNames.Length];
            _reader.GetValues(values);
            rows.Add(values);
            TotalRowsRead++;
        }

        return rows;
    }

    public async ValueTask DisposeAsync()
    {
        await _reader.DisposeAsync();
        if (_command is not null)
            await _command.DisposeAsync();
        _stopwatch.Stop();
    }
}
