# ADR-005: Multiline SQL with semicolon termination

## Status
Accepted

## Context
Users paste multi-line SQL from editors. The REPL needs to accumulate lines and execute when the statement is complete.

## Decision
`SqlBuffer` accumulates lines until one ends with `;`. The semicolon is stripped before execution. A `... >` continuation prompt indicates buffering mode.

## Consequences
- `SqlBuffer` is registered as a singleton so state persists across Typin command invocations
- Single-line queries ending with `;` execute immediately
- Lines without trailing `;` are buffered
- The prompt handler checks `SqlBuffer.IsBuffering` to switch prompt text
