using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class ConnectCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConnectionStore _store;

    public ConnectCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new ConnectionStore(Path.Combine(_tempDir, "connections.json"));
    }

    [Fact]
    public async Task ExecuteAsync_WithUserPassHost_AttemptsConnection()
    {
        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager, _store)
        {
            ConnectionSpec = "scott/tiger@fakehost"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Connection failed", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithConnectionString_AttemptsConnection()
    {
        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager, _store)
        {
            ConnectionSpec = "User Id=scott;Password=tiger;Data Source=fakehost:1521/ORCL"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Connection failed", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithSavedName_UsesSavedConnection()
    {
        _store.Save(new SavedConnection { Name = "dev", Username = "scott", Password = "tiger", Host = "fakehost" });

        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager, _store)
        {
            ConnectionSpec = "dev"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Using saved connection", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithFuzzyMatch_SingleResult_AutoConnects()
    {
        _store.Save(new SavedConnection { Name = "dev-oracle", Username = "scott", Password = "tiger", Host = "fakehost" });
        _store.Save(new SavedConnection { Name = "prod-postgres", Username = "admin", Password = "secret", Host = "prodhost" });

        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager, _store)
        {
            ConnectionSpec = "dev-ora"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Using saved connection 'dev-oracle'", text);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSavedConnections_FallsThrough()
    {
        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager, _store)
        {
            ConnectionSpec = "dev"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        // No saved connections, so it should try as a direct connection spec and fail
        Assert.Contains("Connection failed", text, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
