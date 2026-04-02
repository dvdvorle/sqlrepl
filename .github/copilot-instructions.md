# GitHub Copilot Instructions

## Project
SqlRepl — Interactive Oracle SQL REPL (.NET 10, C#)

## Stack
- Typin 3.1.0 (interactive CLI framework)
- Spectre.Console 0.54.1-alpha.0.86 (terminal UI — alpha, has quirks)
- Oracle.ManagedDataAccess.Core (database driver)
- Microsoft.Data.Sqlite (command history)
- FuzzySharp (connection name matching)
- TextCopy (clipboard)
- xUnit + NSubstitute (testing)

## Workflow
- TDD: write failing test first, then implement
- Run tests: `dotnet test SqlRepl.Tests`
- Install as global tool: `dotnet pack SqlRepl && dotnet tool update --global --add-source SqlRepl/bin/Release SqlRepl`

## Important notes
- `IQueryExecutor` is the mockable interface for `QueryExecutor`
- Spectre alpha: don't pass null Style to Markup constructor, no `[nobr]` tag
- SQLite connections need `ClearAllPools()` on dispose in tests
- `NonBreakingText` is the custom IRenderable for table cells — don't use Spectre's built-in wrapping
- Config: `appsettings.json` + `SQLREPL_*` env vars via Microsoft.Extensions.Configuration
- User data in `~/.sqlrepl/` (connections.json, history.db)
