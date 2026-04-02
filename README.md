# SqlRepl

An interactive Oracle SQL REPL built with C#, [Typin](https://github.com/adambajguz/Typin) for interactive mode with autocompletion, and [Spectre.Console](https://spectreconsole.net/) for rich terminal output.

## Install

```bash
dotnet pack SqlRepl
dotnet tool install --global --add-source SqlRepl/bin/Release SqlRepl
```

Then run from anywhere:

```bash
sqlrepl
```

## Features

- **Rich result tables** — Query results rendered as Spectre.Console tables with rounded borders, lowercase headers, and non-breaking cell text
- **Multiline SQL** — Paste or type multi-line queries; terminated by a trailing `;`
- **Pagination** — Large result sets paginated (default 50 rows/page, configurable)
- **NULL handling** — NULL values shown as grey dash (—); all-null columns auto-hidden with summary
- **Narrow screen safety** — Cell text truncated with ellipsis instead of crashing on small terminals
- **Oracle connectivity** — Connect via TNS alias, host/port/service, or full connection string
- **Saved connections** — Persist named connections in `~/.sqlrepl/connections.json` with fuzzy matching
- **Table inspector** — `info <table>` shows columns, types, nullability, comments, and foreign keys
- **Command history** — SQLite-backed with deduplication, execution counts, and trigram full-text search
- **Clipboard integration** — History search results copied to clipboard with trailing `;`
- **Tab autocompletion** — Commands and saved connection names
- **Date formatting** — Configurable via `appsettings.json` or environment variables
- **Connection-aware prompt** — Shows current data source or `disconnected`

## Usage

### Connecting

```
conn scott/tiger@myalias
conn scott/tiger@myhost:1521/ORCL
conn "User Id=scott;Password=tiger;Data Source=myhost:1521/ORCL"
conn dev                            # fuzzy-match a saved connection
```

### Saved connections

```
conn save dev scott/tiger@myhost
conn list
conn delete dev
```

### Executing queries

```
myhost > SELECT * FROM employees WHERE rownum <= 5;
myhost > INSERT INTO logs (msg) VALUES ('hello');
```

Multiline (paste or type, semicolon terminates):

```
myhost > SELECT e.name, d.department
  ... >   FROM employees e
  ... >   JOIN departments d ON e.dept_id = d.id;
```

### Table info

```
myhost > info FINAFSPRAAK
```

Shows column details (id, name, type, nullable, comments) and foreign keys in formatted tables. Handles multi-schema databases by picking one schema automatically.

### History

```
history                     # show recent commands with execution counts
history search <term>       # trigram substring search, pick result → clipboard
Ctrl+R                      # interactive history search → clipboard
```

### Commands

| Command                | Description                              |
|------------------------|------------------------------------------|
| `conn` / `connect`     | Connect to an Oracle database            |
| `conn save <name>`     | Save a connection for later use          |
| `conn list`            | List saved connections                   |
| `conn delete <name>`   | Delete a saved connection                |
| `info <table>`         | Show columns, types, and foreign keys    |
| `history`              | Show recent command history              |
| `history search <term>`| Search history (trigram substring match)  |
| `help`                 | Show available commands                  |
| `exit` / `quit` / `q`  | Exit the REPL                            |
| Ctrl+R                 | Interactive history search               |
| Tab / Shift+Tab        | Autocomplete commands                    |
| Up / Down              | Navigate command history                 |

### Configuration

Settings in `appsettings.json` (or override with `SQLREPL_` prefixed env vars):

| Setting          | Default               | Env var                  |
|------------------|-----------------------|--------------------------|
| `DateFormat`     | `yyyy-MM-dd HH:mm:ss` | `SQLREPL_DateFormat`     |
| `DateOnlyFormat` | `yyyy-MM-dd`          | `SQLREPL_DateOnlyFormat` |
| `PageSize`       | `50`                  | `SQLREPL_PageSize`       |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Access to an Oracle database

## Development

```bash
dotnet run --project SqlRepl          # run from source
dotnet test SqlRepl.Tests             # run tests (125+)
```

## License

Licensed under the [EUPL v1.2](LICENSE).

## Project structure

```
SqlRepl/
├── Program.cs                — Entry point, Typin builder, DI, Ctrl+R shortcut
├── Commands/
│   ├── SqlCommand.cs         — Default command: SQL execution with multiline buffer
│   ├── ConnectCommand.cs     — conn handler with fuzzy matching
│   ├── ConnectLongCommand.cs — connect alias
│   ├── ConnectHelper.cs      — Shared connection logic
│   ├── ConnSaveCommand.cs    — conn save
│   ├── ConnListCommand.cs    — conn list
│   ├── ConnDeleteCommand.cs  — conn delete
│   ├── InfoCommand.cs        — Table metadata inspector
│   ├── HistoryCommand.cs     — history (recent commands)
│   ├── HistorySearchCommand.cs — history search with clipboard
│   ├── HelpCommand.cs        — help
│   └── ExitCommand.cs        — exit/quit/q
├── ConnectionManager.cs      — Oracle connection lifecycle
├── ConnectionStore.cs        — JSON persistence + fuzzy search
├── QueryExecutor.cs          — SQL execution (IQueryExecutor interface)
├── ResultRenderer.cs         — Spectre.Console tables, pagination, NonBreakingText
├── CommandHistory.cs         — SQLite history with dedup + trigram FTS5
├── SqlBuffer.cs              — Multiline SQL accumulator (semicolon termination)
├── ReplSettings.cs           — Configuration (dates, page size)
└── appsettings.json          — Default settings
```
