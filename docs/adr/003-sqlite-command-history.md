# ADR-003: SQLite for command history with trigram FTS5

## Status
Accepted

## Context
Command history needs to persist across sessions, support substring search, and deduplicate repeated commands while tracking execution metadata.

## Decision
Use SQLite (via Microsoft.Data.Sqlite) with three tables:
- `commands` — deduplicated, UNIQUE constraint on command text
- `executions` — one row per execution with connection name and timestamp
- `commands_fts` — contentless FTS5 virtual table with `tokenize='trigram'` for substring matching

## Consequences
- Trigram tokenizer enables partial-word search (e.g., "fina" matches "finafspraak")
- Queries under 3 characters fall back to `LIKE '%query%'`
- Contentless FTS5 tables cannot DELETE rows — FTS inserts only happen for new commands
- `SqliteConnection.ClearAllPools()` must be called in Dispose to avoid WAL file locks in tests
- History stored at `~/.sqlrepl/history.db`
