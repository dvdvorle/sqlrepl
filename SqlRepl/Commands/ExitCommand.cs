using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("exit", Description = "Exit the REPL.")]
public class ExitCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ICliApplicationLifetime _lifetime;

    public ExitCommand(ConnectionManager connectionManager, ICliApplicationLifetime lifetime)
    {
        _connectionManager = connectionManager;
        _lifetime = lifetime;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await _connectionManager.DisconnectAsync();
        console.Output.WriteLine("Bye!");
        _lifetime.RequestStop();
    }
}

[Command("quit", Description = "Exit the REPL.")]
public class QuitCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ICliApplicationLifetime _lifetime;

    public QuitCommand(ConnectionManager connectionManager, ICliApplicationLifetime lifetime)
    {
        _connectionManager = connectionManager;
        _lifetime = lifetime;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await _connectionManager.DisconnectAsync();
        console.Output.WriteLine("Bye!");
        _lifetime.RequestStop();
    }
}

[Command("q", Description = "Exit the REPL.")]
public class QCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ICliApplicationLifetime _lifetime;

    public QCommand(ConnectionManager connectionManager, ICliApplicationLifetime lifetime)
    {
        _connectionManager = connectionManager;
        _lifetime = lifetime;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await _connectionManager.DisconnectAsync();
        console.Output.WriteLine("Bye!");
        _lifetime.RequestStop();
    }
}
