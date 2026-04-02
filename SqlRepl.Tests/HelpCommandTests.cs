using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class HelpCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_ShowsHelp()
    {
        using var connectionManager = new ConnectionManager();
        var command = new HelpCommand(connectionManager);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("conn", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_DoesNotThrow()
    {
        using var connectionManager = new ConnectionManager();
        var command = new HelpCommand(connectionManager);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        // Should not throw — this is the bug: help was routed to SqlCommand
        var ex = await Record.ExceptionAsync(() => command.ExecuteAsync(console).AsTask());
        Assert.Null(ex);
    }
}
