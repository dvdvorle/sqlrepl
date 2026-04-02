using Spectre.Console;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command(Description = "Execute SQL queries against the connected Oracle database.")]
public class SqlCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly QueryExecutor _queryExecutor;
    private readonly CommandHistory _commandHistory;

    [CommandParameter(0, Name = "sql", Description = "SQL statement to execute")]
    public IReadOnlyList<string> SqlParts { get; init; } = [];

    public SqlCommand(ConnectionManager connectionManager, QueryExecutor queryExecutor, CommandHistory commandHistory)
    {
        _connectionManager = connectionManager;
        _queryExecutor = queryExecutor;
        _commandHistory = commandHistory;
    }

    public static string NormalizeSql(string input)
    {
        var sql = input.Trim().TrimEnd(';').Trim();
        return sql;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var sql = NormalizeSql(string.Join(" ", SqlParts));
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
            ResultRenderer.Render(result);
        }
        catch (Exception ex)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red, o => o.WriteLine($"Error: {ex.Message}"));
        }
    }
}
