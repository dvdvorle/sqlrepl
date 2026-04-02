using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using TextCopy;
using Typin;
using Typin.AutoCompletion;
using Typin.Console;
using Typin.Modes;

namespace SqlRepl;

class Program
{
    // Holder so the Ctrl+R shortcut closure can access DI-registered services
    private static IServiceProvider? _services;

    static async Task<int> Main()
    {
        AnsiConsole.Write(new FigletText("SqlRepl").Color(Color.Yellow));
        AnsiConsole.MarkupLine("[grey]Oracle SQL REPL — Type 'help' for commands, Tab for autocompletion.[/]");
        AnsiConsole.MarkupLine("[grey]Ctrl+R to search history.[/]");
        AnsiConsole.WriteLine();

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ConnectionManager>();
                services.AddSingleton<ConnectionStore>();
                services.AddSingleton<IQueryExecutor, QueryExecutor>();
                services.AddSingleton<CommandHistory>();
                services.AddSingleton<SqlBuffer>();
                services.AddSingleton(ReplSettings.Load());
            })
            .UseInteractiveMode(asStartup: true, options: cfg =>
            {
                cfg.UserDefinedShortcuts.Add(
                    new ShortcutDefinition(
                        ConsoleKey.R,
                        ConsoleModifiers.Control,
                        () => ShowHistorySearch()));

                cfg.SetPrompt((sp, _, console) =>
                {
                    _services = sp;
                    var sqlBuffer = sp.GetRequiredService<SqlBuffer>();
                    if (sqlBuffer.IsBuffering)
                    {
                        console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                            o => o.Write("  ..."));
                        console.Output.Write(" > ");
                        return;
                    }

                    var conn = sp.GetRequiredService<ConnectionManager>();
                    if (conn.IsConnected)
                    {
                        console.Output.WithForegroundColor(ConsoleColor.Green,
                            o => o.Write(conn.CurrentDataSource ?? "oracle"));
                    }
                    else
                    {
                        console.Output.WithForegroundColor(ConsoleColor.Red,
                            o => o.Write("disconnected"));
                    }
                    console.Output.Write(" > ");
                });
            })
            .Build()
            .RunAsync();
    }

    private static void ShowHistorySearch()
    {
        if (_services is null)
            return;

        var history = _services.GetRequiredService<CommandHistory>();
        var entries = history.GetRecent(50);
        if (entries.Count == 0)
        {
            AnsiConsole.MarkupLine("\n[grey]No history yet.[/]");
            return;
        }

        var choices = entries.Select(e => e.Command).Distinct().ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[yellow]Search history (type to filter):[/]")
                .PageSize(15)
                .EnableSearch()
                .AddChoices(choices));

        // Write the selected command to the console so Typin picks it up
        AnsiConsole.MarkupLine($"[grey]> {Markup.Escape(selected)}[/]");

        try
        {
            var clipboard = selected.TrimEnd().EndsWith(';') ? selected : selected.TrimEnd() + ";";
            ClipboardService.SetText(clipboard);
            AnsiConsole.MarkupLine("[grey]Copied to clipboard.[/]");
        }
        catch
        {
            // Clipboard may not be available in all environments
        }
    }
}
