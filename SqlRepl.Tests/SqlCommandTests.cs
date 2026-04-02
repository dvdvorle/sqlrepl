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
    public async Task ExecuteAsync_StripsTrailingSemicolon()
    {
        // We can't test actual SQL execution without a DB, but we can verify
        // the semicolon is stripped by checking that "SELECT 1;" doesn't get
        // passed through literally. When not connected, the SQL is never sent,
        // so we test via a helper method instead.
        Assert.Equal("SELECT 1", SqlCommand.NormalizeSql("SELECT 1;"));
        Assert.Equal("SELECT 1", SqlCommand.NormalizeSql("SELECT 1 ;"));
        Assert.Equal("SELECT 1", SqlCommand.NormalizeSql("SELECT 1 ; "));
        Assert.Equal("SELECT 1", SqlCommand.NormalizeSql("  SELECT 1;  "));
        Assert.Equal("SELECT 1", SqlCommand.NormalizeSql("SELECT 1"));
        Assert.Equal("", SqlCommand.NormalizeSql(";"));
        Assert.Equal("", SqlCommand.NormalizeSql("  ;  "));
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
