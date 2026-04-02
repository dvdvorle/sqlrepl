# SqlRepl

An interactive Oracle SQL REPL built with C#, [Typin](https://github.com/adambajguz/Typin) for interactive mode with autocompletion, and [Spectre.Console](https://spectreconsole.net/) for rich terminal output.

## Features

- **Tab autocompletion** — Autocomplete commands (`conn`, `connect`) via Typin interactive mode
- **Command history** — Navigate previous commands with Up/Down arrows
- **Rich result tables** — Query results displayed as formatted Spectre.Console tables with NULL highlighting
- **Oracle connectivity** — Connect via TNS alias, host/port/service, or full connection string
- **Elapsed time** — Execution duration shown after every query
- **Connection-aware prompt** — Shows the current data source or `disconnected`

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Access to an Oracle database

### Run

```bash
dotnet run --project SqlRepl
```

### Test

```bash
dotnet test
```

## Usage

### Connecting

```
conn scott/tiger@myalias
conn scott/tiger@myhost:1521/ORCL
conn "User Id=scott;Password=tiger;Data Source=myhost:1521/ORCL"
conn dev                            # use a saved connection
connect scott/tiger@myhost
```

### Saved Connections

Save connections for reuse across sessions (stored in `~/.sqlrepl/connections.json`):

```
conn save dev scott/tiger@myhost
conn save prod "User Id=admin;Password=secret;Data Source=prodhost:1521/PROD"
conn list
conn delete dev
conn dev                            # connect using saved name
```

### Executing queries

Type SQL directly at the prompt:

```
myhost:1521/ORCL > SELECT * FROM employees WHERE rownum <= 5
myhost:1521/ORCL > INSERT INTO logs (msg) VALUES ('hello')
```

### Commands

| Command                | Description                              |
|------------------------|------------------------------------------|
| `conn` / `connect`     | Connect to an Oracle database            |
| `conn save <name>`     | Save a connection for later use          |
| `conn list`            | List saved connections                   |
| `conn delete <name>`   | Delete a saved connection                |
| `help`                 | Show available commands                  |
| `exit` / `quit` / `q`  | Exit the REPL                            |
| Tab / Shift+Tab        | Autocomplete commands                    |
| Up / Down              | Navigate command history                 |

## Project Structure

```
SqlRepl/
├── Program.cs                — Entry point, Typin app builder
├── Commands/
│   ├── ConnectCommand.cs     — [Command("conn")] handler
│   ├── ConnectLongCommand.cs — [Command("connect")] alias
│   ├── ConnectHelper.cs      — Shared connection logic
│   ├── ConnSaveCommand.cs    — [Command("conn save")] save connections
│   ├── ConnListCommand.cs    — [Command("conn list")] list saved connections
│   ├── ConnDeleteCommand.cs  — [Command("conn delete")] remove connections
│   ├── HelpCommand.cs        — [Command("help")] usage info
│   ├── ExitCommand.cs        — [Command("exit/quit/q")] exit REPL
│   └── SqlCommand.cs         — Default command for SQL queries
├── ConnectionManager.cs      — Oracle connection lifecycle
├── ConnectionStore.cs        — JSON-based persistent connection storage
├── QueryExecutor.cs          — SQL execution, returns DataTable or row count
├── ResultRenderer.cs         — Spectre.Console table rendering
├── CommandParser.cs          — Legacy input parsing (used by tests)
└── Repl.cs                   — Legacy REPL loop (replaced by Typin)

SqlRepl.Tests/
├── CommandParserTests.cs
├── ConnectCommandTests.cs
├── SqlCommandTests.cs
├── ConnectionManagerTests.cs
├── ConnectionManagerBuildConnectionStringTests.cs
├── ResultRendererTests.cs
└── ReplHelpTests.cs
```
