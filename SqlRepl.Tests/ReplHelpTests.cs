using Spectre.Console;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ReplHelpTests
{
    [Fact]
    public void PrintHelp_DoesNotThrowOnMarkup()
    {
        // Reproduces: malformed markup tag from unescaped brackets in help text
        var console = new TestConsole();
        console.Profile.Width = 120;
        AnsiConsole.Console = console;

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]Description[/]");

        // This is the exact row from Repl.PrintHelp() that causes the crash
        table.AddRow("[yellow]conn[/] user/pass@host[[:port]][[/service]]", "Connect using credentials and host");

        AnsiConsole.Write(table);
    }
}
