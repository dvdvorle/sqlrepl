using NSubstitute;
using SqlRepl.Commands;
using Typin;
using Typin.Console;

namespace SqlRepl.Tests;

public class ExitCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_DoesNotThrow()
    {
        using var connectionManager = new ConnectionManager();
        var lifetime = Substitute.For<ICliApplicationLifetime>();
        var command = new ExitCommand(connectionManager, lifetime);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var ex = await Record.ExceptionAsync(() => command.ExecuteAsync(console).AsTask());
        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_WritesByeAndRequestsStop()
    {
        using var connectionManager = new ConnectionManager();
        var lifetime = Substitute.For<ICliApplicationLifetime>();
        var command = new ExitCommand(connectionManager, lifetime);

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        await command.ExecuteAsync(console);

        var text = output.GetString();
        Assert.Contains("Bye", text, StringComparison.OrdinalIgnoreCase);
        lifetime.Received().RequestStop();
    }
}
