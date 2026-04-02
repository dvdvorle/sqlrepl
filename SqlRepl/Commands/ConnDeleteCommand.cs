using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("conn delete", Description = "Delete a saved connection.")]
public class ConnDeleteCommand : ICommand
{
    private readonly ConnectionStore _store;

    [CommandParameter(0, Name = "name", Description = "Name of the connection to delete")]
    public string Name { get; init; } = "";

    public ConnDeleteCommand(ConnectionStore store)
    {
        _store = store;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (_store.Delete(Name))
        {
            console.Output.WithForegroundColor(ConsoleColor.Green,
                o => o.WriteLine($"Deleted connection '{Name}'."));
        }
        else
        {
            console.Output.WithForegroundColor(ConsoleColor.Red,
                o => o.WriteLine($"Connection '{Name}' not found."));
        }

        return default;
    }
}
