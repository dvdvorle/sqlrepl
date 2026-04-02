using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class ConnectCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithUserPassHost_AttemptsConnection()
    {
        using var connectionManager = new ConnectionManager();
        var command = new ConnectCommand(connectionManager)
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
        var command = new ConnectCommand(connectionManager)
        {
            ConnectionSpec = "User Id=scott;Password=tiger;Data Source=fakehost:1521/ORCL"
        };

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Connection failed", text, StringComparison.OrdinalIgnoreCase);
    }
}
