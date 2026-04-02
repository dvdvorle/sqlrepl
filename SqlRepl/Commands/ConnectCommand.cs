using Spectre.Console;
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
        // 1. Exact saved connection match
        var saved = _store.Get(ConnectionSpec);
        if (saved is not null)
        {
            await ConnectWithSaved(saved, console);
            return;
        }

        // 2. If it looks like user/pass@host or a connection string, try directly
        if (ConnectHelper.IsDirectConnectionSpec(ConnectionSpec))
        {
            await ConnectHelper.ExecuteConnectAsync(_connectionManager, ConnectionSpec, console);
            return;
        }

        // 3. Fuzzy search saved connections
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

        // 4. No matches — try as direct connection spec
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

