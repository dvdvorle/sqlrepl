using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class SqlCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public SqlCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "history.db");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_WritesError()
    {
        using var connectionManager = new ConnectionManager();
        var queryExecutor = new QueryExecutor(connectionManager);
        using var history = new CommandHistory(_dbPath);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new SqlCommand(connectionManager, queryExecutor, history, new ReplSettings(), new SqlBuffer())
        {
            SqlParts = ["SELECT", "*", "FROM", "dual;"]
        };

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Not connected", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_StripsTrailingSemicolon()
    {
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
        using var history = new CommandHistory(_dbPath);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new SqlCommand(connectionManager, queryExecutor, history, new ReplSettings(), new SqlBuffer())
        {
            SqlParts = []
        };

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Empty(text);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
