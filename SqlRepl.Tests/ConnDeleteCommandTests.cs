using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class ConnDeleteCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConnectionStore _store;

    public ConnDeleteCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new ConnectionStore(Path.Combine(_tempDir, "connections.json"));
    }

    [Fact]
    public async Task Delete_ExistingConnection_Removes()
    {
        _store.Save(new SavedConnection { Name = "dev", Username = "u", Password = "p", Host = "h" });

        var command = new ConnDeleteCommand(_store) { Name = "dev" };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        Assert.Null(_store.Get("dev"));
        var text = output.GetString();
        Assert.Contains("Deleted", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_NonExistent_ShowsError()
    {
        var command = new ConnDeleteCommand(_store) { Name = "nope" };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("not found", text, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
