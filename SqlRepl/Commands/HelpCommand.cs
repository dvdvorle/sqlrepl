using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("help", Description = "Show available commands.")]
public class HelpCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;

    public HelpCommand(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var status = _connectionManager.IsConnected
            ? $"Connected to {_connectionManager.CurrentDataSource ?? "oracle"}"
            : "Not connected";

        console.Output.WriteLine();
        console.Output.WithForegroundColor(ConsoleColor.Yellow, o => o.WriteLine("  SqlRepl — Oracle SQL REPL"));
        console.Output.WriteLine($"  Status: {status}");
        console.Output.WriteLine();
        console.Output.WithForegroundColor(ConsoleColor.White, o => o.WriteLine("  Commands:"));
        console.Output.WriteLine("    conn <user/pass@host>       Connect via credentials and TNS alias or host");
        console.Output.WriteLine("    conn <name-or-fuzzy>        Connect using a saved connection (fuzzy match)");
        console.Output.WriteLine("    conn \"<connection string>\"  Connect via full connection string");
        console.Output.WriteLine("    connect ...                 Alias for conn");
        console.Output.WriteLine("    conn save <name> <spec>     Save a connection for later use");
        console.Output.WriteLine("    conn list                   List saved connections");
        console.Output.WriteLine("    conn delete <name>          Delete a saved connection");
        console.Output.WriteLine("    help                        Show this help");
        console.Output.WriteLine("    exit / quit / q             Exit the REPL");
        console.Output.WriteLine("    <SQL statement>             Execute SQL against the connected database");
        console.Output.WriteLine();
        console.Output.WithForegroundColor(ConsoleColor.White, o => o.WriteLine("  Interactive:"));
        console.Output.WriteLine("    Tab / Shift+Tab             Autocomplete commands");
        console.Output.WriteLine("    Up / Down                   Navigate command history");
        console.Output.WriteLine();
        console.Output.WithForegroundColor(ConsoleColor.White, o => o.WriteLine("  Configuration (appsettings.json or env vars):"));
        console.Output.WriteLine("    DateFormat                  DateTime format (default: yyyy-MM-dd HH:mm:ss)");
        console.Output.WriteLine("    DateOnlyFormat              Date-only format (default: yyyy-MM-dd)");
        console.Output.WriteLine("    Env: SQLREPL_DateFormat, SQLREPL_DateOnlyFormat");
        console.Output.WriteLine();

        return default;
    }
}
