# ADR-002: Spectre.Console for result rendering

## Status
Accepted

## Context
SQL results need to be displayed as formatted tables with features like column alignment, NULL styling, and pagination.

## Decision
Use Spectre.Console (v0.54.1-alpha.0.86) for table rendering. A custom `NonBreakingText : IRenderable` prevents cell text from wrapping at whitespace boundaries, truncating with ellipsis instead.

## Consequences
- Rich table output with rounded borders and colored headers
- `NonBreakingText` handles narrow terminals gracefully (maxWidth guards for 0, 1, small values)
- The alpha version has quirks: `Markup` constructor crashes with null `Style` parameter, `[nobr]` tag doesn't exist, `TableColumn` doesn't implement `IOverflowable`
- `TestConsole` from Spectre.Console.Testing doesn't support ANSI capabilities checks — feature detection must be wrapped in try/catch
