using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("conn", Description = "Connect to an Oracle database. Usage: conn user/pass@host, conn <saved-name>, or conn \"connection string\"")]
public class ConnectCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ConnectionStore _store;

    [CommandParameter(0, Name = "connection-spec", Description = "Connection: user/pass@host, saved name, or connection string")]
    public string ConnectionSpec { get; init; } = "";

    public ConnectCommand(ConnectionManager connectionManager, ConnectionStore store)
    {
        _connectionManager = connectionManager;
        _store = store;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // Check if it's a saved connection name
        var saved = _store.Get(ConnectionSpec);
        if (saved is not null)
        {
            var spec = saved.IsComponentBased
                ? $"{saved.Username}/{saved.Password}@{saved.Host}"
                : saved.ConnectionString!;

            console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                o => o.WriteLine($"Using saved connection '{saved.Name}'..."));

            await ConnectHelper.ExecuteConnectAsync(_connectionManager, spec, console);
            return;
        }

        await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
    }
}

