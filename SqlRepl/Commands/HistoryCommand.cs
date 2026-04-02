using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("history", Description = "Show recent command history.")]
public class HistoryCommand : ICommand
{
    private readonly CommandHistory _commandHistory;

    [CommandOption("limit", 'n', Description = "Number of entries to show.")]
    public int Limit { get; init; } = 50;

    public HistoryCommand(CommandHistory commandHistory)
    {
        _commandHistory = commandHistory;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var entries = _commandHistory.GetRecent(Limit);
        if (entries.Count == 0)
        {
            console.Output.WriteLine("No history yet.");
            return default;
        }

        foreach (var entry in entries.Reverse())
        {
            console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                o => o.Write($"  {entry.Id,4}  "));
            console.Output.Write(entry.Command);
            if (entry.ExecutionCount > 1)
            {
                console.Output.WithForegroundColor(ConsoleColor.DarkGray,
                    o => o.Write($"  ({entry.ExecutionCount}x)"));
            }
            console.Output.WriteLine();
        }

        return default;
    }
}
