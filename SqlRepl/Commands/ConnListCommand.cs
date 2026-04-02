using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("conn list", Description = "List saved connections.")]
public class ConnListCommand : ICommand
{
    private readonly ConnectionStore _store;

    public ConnListCommand(ConnectionStore store)
    {
        _store = store;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var connections = _store.GetAll();

        if (connections.Count == 0)
        {
            console.Output.WriteLine("No saved connections.");
            return default;
        }

        console.Output.WriteLine();
        foreach (var conn in connections)
        {
            var detail = conn.IsComponentBased
                ? $"{conn.Username}@{conn.Host}"
                : conn.ConnectionString ?? "(empty)";

            console.Output.WithForegroundColor(ConsoleColor.Yellow, o => o.Write($"  {conn.Name}"));
            console.Output.WriteLine($"  {detail}");
        }
        console.Output.WriteLine();

        return default;
    }
}
