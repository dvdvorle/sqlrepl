using System.Text.RegularExpressions;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("conn save", Description = "Save a connection for later use.")]
public partial class ConnSaveCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ConnectionStore _store;

    [CommandParameter(0, Name = "name", Description = "Name for the saved connection")]
    public string Name { get; init; } = "";

    [CommandParameter(1, Name = "connection-spec", Description = "Connection: user/pass@host or a connection string")]
    public string ConnectionSpec { get; init; } = "";

    [GeneratedRegex(@"^(\S+)/(\S+)@(\S+)$")]
    private static partial Regex ComponentsRegex();

    public ConnSaveCommand(ConnectionManager connectionManager, ConnectionStore store)
    {
        _connectionManager = connectionManager;
        _store = store;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var match = ComponentsRegex().Match(ConnectionSpec);
        SavedConnection saved;

        if (match.Success)
        {
            saved = new SavedConnection
            {
                Name = Name,
                Username = match.Groups[1].Value,
                Password = match.Groups[2].Value,
                Host = match.Groups[3].Value
            };
        }
        else
        {
            saved = new SavedConnection
            {
                Name = Name,
                ConnectionString = ConnectionSpec
            };
        }

        _store.Save(saved);
        console.Output.WithForegroundColor(ConsoleColor.Green,
            o => o.WriteLine($"Saved connection '{Name}'."));

        return default;
    }
}
