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

        foreach (var entry in entries.Reverse())
        {
            console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                o => o.Write($"  {entry.Id,4}  "));
            console.Output.WriteLine(entry.Command);
        }

        return default;
    }
}
