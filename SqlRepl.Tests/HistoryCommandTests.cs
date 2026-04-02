using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class HistoryCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public HistoryCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "history.db");
    }

    [Fact]
    public async Task History_NoArgs_ShowsRecentCommands()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT 1 FROM dual", "devdb");
        history.Add("SELECT * FROM employees", "devdb");

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new HistoryCommand(history);
        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("SELECT 1 FROM dual", text);
        Assert.Contains("SELECT * FROM employees", text);
    }

    [Fact]
    public async Task History_Search_FiltersResults()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT * FROM employees", "devdb");
        history.Add("INSERT INTO logs VALUES ('x')", "devdb");
        history.Add("SELECT name FROM employees", "devdb");

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new HistorySearchCommand(history) { Query = "employees" };
        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("employees", text);
        Assert.DoesNotContain("logs", text);
    }

    [Fact]
    public async Task History_Empty_ShowsMessage()
    {
        using var history = new CommandHistory(_dbPath);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new HistoryCommand(history);
        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("No history", text, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
