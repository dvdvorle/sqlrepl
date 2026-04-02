# ADR-006: Configuration via appsettings.json and environment variables

## Status
Accepted

## Context
Date formatting and pagination page size should be configurable without recompilation.

## Decision
Use `Microsoft.Extensions.Configuration` with JSON file + environment variable providers. Settings class `ReplSettings` loads from `appsettings.json` with `SQLREPL_` prefixed env var overrides.

## Consequences
- `appsettings.json` ships with the tool (CopyToOutputDirectory)
- Environment variables take precedence: `SQLREPL_DateFormat`, `SQLREPL_DateOnlyFormat`, `SQLREPL_PageSize`
- Dates at midnight (00:00:00) use `DateOnlyFormat`; others use `DateFormat`
