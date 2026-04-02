using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("connect", Description = "Connect to an Oracle database. Usage: connect user/pass@host or connect \"connection string\"")]
public class ConnectLongCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;

    [CommandParameter(0, Name = "connection-spec", Description = "Connection: user/pass@host or a connection string")]
    public string ConnectionSpec { get; init; } = "";

    public ConnectLongCommand(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
    }
}
