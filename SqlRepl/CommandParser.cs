using System.Text.RegularExpressions;

namespace SqlRepl;

public enum CommandType
{
    Empty,
    Exit,
    Connect,
    Help,
    Query
}

public record ParsedCommand(CommandType Type, string RawInput)
{
    /// <summary>Connection string when a full connection string is provided.</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Username for component-based connections.</summary>
    public string? Username { get; init; }

    /// <summary>Password for component-based connections.</summary>
    public string? Password { get; init; }

    /// <summary>Host (host[:port][/service]) for component-based connections.</summary>
    public string? Host { get; init; }
}

public static partial class CommandParser
{
    // Matches: conn user/pass@host  or  connect user/pass@host
    [GeneratedRegex(@"^conn(?:ect)?\s+(\S+)/(\S+)@(\S+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectComponentsRegex();

    // Matches: conn "..." or connect "..."  (connection string in quotes)
    [GeneratedRegex(@"^conn(?:ect)?\s+""(.+)""$", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectStringQuotedRegex();

    // Matches: conn ...  (connection string without quotes, containing '=')
    [GeneratedRegex(@"^conn(?:ect)?\s+(.+=.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectStringRawRegex();

    public static ParsedCommand Parse(string input)
    {
        var trimmed = input.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return new ParsedCommand(CommandType.Empty, trimmed);

        if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("q", StringComparison.OrdinalIgnoreCase))
            return new ParsedCommand(CommandType.Exit, trimmed);

        if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("?", StringComparison.OrdinalIgnoreCase))
            return new ParsedCommand(CommandType.Help, trimmed);

        // conn user/pass@host
        var match = ConnectComponentsRegex().Match(trimmed);
        if (match.Success)
        {
            return new ParsedCommand(CommandType.Connect, trimmed)
            {
                Username = match.Groups[1].Value,
                Password = match.Groups[2].Value,
                Host = match.Groups[3].Value
            };
        }

        // conn "connection string"
        match = ConnectStringQuotedRegex().Match(trimmed);
        if (match.Success)
        {
            return new ParsedCommand(CommandType.Connect, trimmed)
            {
                ConnectionString = match.Groups[1].Value
            };
        }

        // conn Key=Value;...
        match = ConnectStringRawRegex().Match(trimmed);
        if (match.Success)
        {
            return new ParsedCommand(CommandType.Connect, trimmed)
            {
                ConnectionString = match.Groups[1].Value
            };
        }

        // Everything else is a SQL query
        return new ParsedCommand(CommandType.Query, trimmed);
    }
}
