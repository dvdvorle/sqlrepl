using Spectre.Console;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("connect", Description = "Connect to an Oracle database. Usage: connect user/pass@host, connect <saved-name>, or connect \"connection string\"")]
public class ConnectLongCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly ConnectionStore _store;

    [CommandParameter(0, Name = "connection-spec", Description = "Connection: user/pass@host, saved name, or connection string")]
    public string ConnectionSpec { get; init; } = "";

    public ConnectLongCommand(ConnectionManager connectionManager, ConnectionStore store)
    {
        _connectionManager = connectionManager;
        _store = store;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var saved = _store.Get(ConnectionSpec);
        if (saved is not null)
        {
            await ConnectWithSaved(saved, console);
            return;
        }

        if (ConnectHelper.IsDirectConnectionSpec(ConnectionSpec))
        {
            await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
            return;
        }

        var matches = _store.FuzzySearch(ConnectionSpec);
        if (matches.Count == 1)
        {
            await ConnectWithSaved(matches[0], console);
            return;
        }

        if (matches.Count > 1)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Multiple connections match [yellow]{Markup.Escape(ConnectionSpec)}[/]:")
                    .AddChoices(matches.Select(m => m.Name)));

            var chosen = _store.Get(choice)!;
            await ConnectWithSaved(chosen, console);
            return;
        }

        await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
    }

    private async ValueTask ConnectWithSaved(SavedConnection saved, IConsole console)
    {
        var spec = saved.IsComponentBased
            ? $"{saved.Username}/{saved.Password}@{saved.Host}"
            : saved.ConnectionString!;

        console.Output.WithForegroundColor(ConsoleColor.DarkGray,
            o => o.WriteLine($"Using saved connection '{saved.Name}'..."));

        await ConnectHelper.ExecuteConnectAsync(_connectionManager, spec, console);
    }
}
