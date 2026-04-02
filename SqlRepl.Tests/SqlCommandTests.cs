using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class SqlCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_WritesError()
    {
        using var connectionManager = new ConnectionManager();
        var queryExecutor = new QueryExecutor(connectionManager);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new SqlCommand(connectionManager, queryExecutor)
        {
            SqlParts = ["SELECT", "*", "FROM", "dual"]
        };

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Not connected", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyParts_DoesNothing()
    {
        using var connectionManager = new ConnectionManager();
        var queryExecutor = new QueryExecutor(connectionManager);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new SqlCommand(connectionManager, queryExecutor)
        {
            SqlParts = []
        };

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Empty(text);
    }
}
