using Spectre.Console;
using TextCopy;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("history search", Description = "Search command history using full-text search.")]
public class HistorySearchCommand : ICommand
{
    private readonly CommandHistory _commandHistory;

    [CommandParameter(0, Name = "query", Description = "Search term")]
    public string Query { get; init; } = "";

    public HistorySearchCommand(CommandHistory commandHistory)
    {
        _commandHistory = commandHistory;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            console.Output.WriteLine("Usage: history search <term>");
            return default;
        }

        var entries = _commandHistory.Search(Query);
        if (entries.Count == 0)
        {
            console.Output.WriteLine($"No history matching '{Query}'.");
            return default;
        }

        if (entries.Count == 1)
        {
            var cmd = entries[0].Command;
            CopyToClipboard(cmd);
            console.Output.WriteLine(cmd);
            console.Output.WithForegroundColor(ConsoleColor.DarkGray, o => o.WriteLine("Copied to clipboard."));
            return default;
        }

        if (!AnsiConsole.Console.Profile.Capabilities.Interactive)
        {
            // Non-interactive: just list results
            foreach (var entry in entries.Reverse())
            {
                console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                    o => o.Write($"  {entry.Id,4}  "));
                console.Output.WriteLine(entry.Command);
            }
            return default;
        }

        var choices = entries.Select(e => e.Command).ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]Results for '{Markup.Escape(Query)}':[/]")
                .PageSize(15)
                .AddChoices(choices));

        CopyToClipboard(selected);
        AnsiConsole.MarkupLine($"[grey]Copied to clipboard.[/]");

        return default;
    }

    private static void CopyToClipboard(string text)
    {
        var clipboard = text.TrimEnd().EndsWith(';') ? text : text.TrimEnd() + ";";
        try { ClipboardService.SetText(clipboard); } catch { }
    }
}
