using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class ConnSaveCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConnectionStore _store;

    public ConnSaveCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new ConnectionStore(Path.Combine(_tempDir, "connections.json"));
    }

    [Fact]
    public async Task Save_StoresConnectionSpec()
    {
        using var connectionManager = new ConnectionManager();
        var command = new ConnSaveCommand(connectionManager, _store)
        {
            Name = "myconn",
            ConnectionSpec = "scott/tiger@devdb"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var saved = _store.Get("myconn");
        Assert.NotNull(saved);
        Assert.Equal("scott", saved.Username);
        Assert.Equal("tiger", saved.Password);
        Assert.Equal("devdb", saved.Host);

        var text = output.GetString();
        Assert.Contains("Saved", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Save_ConnectionString_Stores()
    {
        using var connectionManager = new ConnectionManager();
        var connStr = "User Id=scott;Password=tiger;Data Source=devdb";
        var command = new ConnSaveCommand(connectionManager, _store)
        {
            Name = "myconn",
            ConnectionSpec = connStr
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var saved = _store.Get("myconn");
        Assert.NotNull(saved);
        Assert.Equal(connStr, saved.ConnectionString);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
