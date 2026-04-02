using Spectre.Console;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("conn", Description = "Connect to an Oracle database. Usage: conn user/pass@host or conn \"connection string\"")]
public class ConnectCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;

    [CommandParameter(0, Name = "connection-spec", Description = "Connection: user/pass@host or a connection string")]
    public string ConnectionSpec { get; init; } = "";

    public ConnectCommand(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
    }
}

