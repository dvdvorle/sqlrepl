using Spectre.Console;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command(Description = "Execute SQL queries against the connected Oracle database.")]
public class SqlCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly IQueryExecutor _queryExecutor;
    private readonly CommandHistory _commandHistory;
    private readonly ReplSettings _settings;
    private readonly SqlBuffer _sqlBuffer;

    [CommandParameter(0, Name = "sql", Description = "SQL statement to execute")]
    public IReadOnlyList<string> SqlParts { get; init; } = [];

    public SqlCommand(ConnectionManager connectionManager, IQueryExecutor queryExecutor, CommandHistory commandHistory, ReplSettings settings, SqlBuffer sqlBuffer)
    {
        _connectionManager = connectionManager;
        _queryExecutor = queryExecutor;
        _commandHistory = commandHistory;
        _settings = settings;
        _sqlBuffer = sqlBuffer;
    }

    public static string NormalizeSql(string input)
    {
        var sql = input.Trim().TrimEnd(';').Trim();
        return sql;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var rawInput = string.Join(" ", SqlParts);
        if (string.IsNullOrWhiteSpace(rawInput))
            return;

        var bufferResult = _sqlBuffer.Append(rawInput);
        if (!bufferResult.IsComplete)
        {
            // Buffering — continuation prompt will be shown by the prompt handler
            return;
        }

        var sql = bufferResult.Sql?.Trim() ?? "";
        if (string.IsNullOrEmpty(sql))
            return;

        if (!_connectionManager.IsConnected)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red, o => o.WriteLine("Not connected. Use 'conn' to connect first."));
            return;
        }

        try
        {
            var result = await _queryExecutor.ExecuteAsync(sql);
            _commandHistory.Add(sql, _connectionManager.CurrentDataSource);
            ResultRenderer.Render(result, settings: _settings);
        }
        catch (Exception ex)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red, o => o.WriteLine($"Error: {ex.Message}"));
        }
    }
}
