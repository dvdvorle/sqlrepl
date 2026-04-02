using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class ConnListCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConnectionStore _store;

    public ConnListCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new ConnectionStore(Path.Combine(_tempDir, "connections.json"));
    }

    [Fact]
    public async Task List_WhenEmpty_ShowsNoConnections()
    {
        var command = new ConnListCommand(_store);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("No saved connections", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_ShowsSavedConnections()
    {
        _store.Save(new SavedConnection { Name = "dev", Username = "scott", Password = "tiger", Host = "devdb" });
        _store.Save(new SavedConnection { Name = "prod", ConnectionString = "Data Source=proddb" });

        var command = new ConnListCommand(_store);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("dev", text);
        Assert.Contains("prod", text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
