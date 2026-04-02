using Spectre.Console;

namespace SqlRepl;

public class Repl : IDisposable
{
    private readonly ConnectionManager _connectionManager = new();
    private readonly IQueryExecutor _queryExecutor;

    public Repl()
    {
        _queryExecutor = new QueryExecutor(_connectionManager);
    }

    public async Task RunAsync()
    {
        PrintBanner();

        while (true)
        {
            var prompt = BuildPrompt();
            AnsiConsole.Markup(prompt);

            var input = Console.ReadLine();
            if (input is null) // Ctrl+C / EOF
                break;

            var command = CommandParser.Parse(input);

            switch (command.Type)
            {
                case CommandType.Empty:
                    continue;

                case CommandType.Exit:
                    AnsiConsole.MarkupLine("[grey]Bye![/]");
                    return;

                case CommandType.Help:
                    PrintHelp();
                    continue;

                case CommandType.Connect:
                    await HandleConnectAsync(command);
                    continue;

                case CommandType.Query:
                    await HandleQueryAsync(command);
                    continue;
            }
        }
    }

    private string BuildPrompt()
    {
        if (_connectionManager.IsConnected)
            return $"[green]{Markup.Escape(_connectionManager.CurrentDataSource ?? "oracle")}[/] [grey]>[/] ";
        else
            return "[red]disconnected[/] [grey]>[/] ";
    }

    private static void PrintBanner()
    {
        AnsiConsole.Write(new FigletText("SqlRepl").Color(Color.Yellow));
        AnsiConsole.MarkupLine("[grey]Oracle SQL REPL — Type 'help' for commands, 'exit' to quit.[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintHelp()
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]Description[/]");

        table.AddRow("[yellow]conn[/] user/pass@host[[:port]][[/service]]", "Connect using credentials and host");
        table.AddRow("[yellow]conn[/] \"connection string\"", "Connect using a full connection string");
        table.AddRow("[yellow]conn[/] Key=Value;...", "Connect using a connection string (no quotes)");
        table.AddRow("[yellow]help[/] or [yellow]?[/]", "Show this help");
        table.AddRow("[yellow]exit[/] / [yellow]quit[/] / [yellow]q[/]", "Exit the REPL");
        table.AddRow("[grey]<any SQL>[/]", "Execute a SQL query or statement");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private async Task HandleConnectAsync(ParsedCommand command)
    {
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync("Connecting...", async _ =>
                {
                    if (command.ConnectionString is not null)
                        await _connectionManager.ConnectAsync(command.ConnectionString);
                    else
                        await _connectionManager.ConnectAsync(command.Username!, command.Password!, command.Host!);
                });

            AnsiConsole.MarkupLine($"[green]Connected to {Markup.Escape(_connectionManager.CurrentDataSource ?? "oracle")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Connection failed:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private async Task HandleQueryAsync(ParsedCommand command)
    {
        if (!_connectionManager.IsConnected)
        {
            AnsiConsole.MarkupLine("[red]Not connected. Use 'conn' to connect first.[/]");
            return;
        }

        try
        {
            var result = await _queryExecutor.ExecuteAsync(command.RawInput);
            ResultRenderer.Render(result);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.WriteLine();
    }

    public void Dispose()
    {
        _connectionManager.Dispose();
    }
}
