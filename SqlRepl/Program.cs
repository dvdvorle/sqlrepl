using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Typin;
using Typin.Console;
using Typin.Modes;

namespace SqlRepl;

class Program
{
    static async Task<int> Main()
    {
        AnsiConsole.Write(new FigletText("SqlRepl").Color(Color.Yellow));
        AnsiConsole.MarkupLine("[grey]Oracle SQL REPL — Type 'help' for commands, Tab for autocompletion.[/]");
        AnsiConsole.WriteLine();

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ConnectionManager>();
                services.AddSingleton<ConnectionStore>();
                services.AddSingleton<QueryExecutor>();
            })
            .UseInteractiveMode(asStartup: true, options: cfg =>
            {
                cfg.SetPrompt((sp, _, console) =>
                {
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
}
