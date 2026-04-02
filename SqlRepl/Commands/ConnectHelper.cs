using System.Text.RegularExpressions;
using Typin.Console;

namespace SqlRepl.Commands;

public static partial class ConnectHelper
{
    // Matches: user/pass@host
    [GeneratedRegex(@"^(\S+)/(\S+)@(\S+)$")]
    private static partial Regex ComponentsRegex();

    public static async Task ExecuteConnectAsync(ConnectionManager connectionManager, string spec, IConsole console)
    {
        try
        {
            var match = ComponentsRegex().Match(spec);
            if (match.Success)
            {
                var username = match.Groups[1].Value;
                var password = match.Groups[2].Value;
                var host = match.Groups[3].Value;
                await connectionManager.ConnectAsync(username, password, host);
            }
            else
            {
                // Treat as a full connection string
                await connectionManager.ConnectAsync(spec);
            }

            console.Output.WithForegroundColor(ConsoleColor.Green,
                o => o.WriteLine($"Connected to {connectionManager.CurrentDataSource ?? "oracle"}"));
        }
        catch (Exception ex)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red,
                o => o.WriteLine($"Connection failed: {ex.Message}"));
        }
    }
}
