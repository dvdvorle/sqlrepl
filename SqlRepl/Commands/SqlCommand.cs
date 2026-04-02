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

    [CommandParameter(0, Name = "sql", Description = "SQL statement to execute")]
    public IReadOnlyList<string> SqlParts { get; init; } = [];

    public SqlCommand(ConnectionManager connectionManager, QueryExecutor queryExecutor)
    {
        _connectionManager = connectionManager;
        _queryExecutor = queryExecutor;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var sql = string.Join(" ", SqlParts).Trim();
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
            ResultRenderer.Render(result);
        }
        catch (Exception ex)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red, o => o.WriteLine($"Error: {ex.Message}"));
        }
    }
}
