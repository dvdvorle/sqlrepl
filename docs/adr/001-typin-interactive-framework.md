# ADR-001: Typin for interactive CLI framework

## Status
Accepted

## Context
The REPL needs an interactive command-line framework with tab autocompletion, command routing, keyboard shortcuts, and dependency injection.

## Decision
Use [Typin](https://github.com/adambajguz/Typin) in interactive mode. Commands are classes decorated with `[Command]` attributes, and Typin handles parsing, routing, and DI.

## Consequences
- Commands are cleanly separated into individual classes
- Autocompletion works out of the box for command names
- Keyboard shortcuts (Ctrl+R) are added via `ShortcutDefinition`
- Custom prompt text requires hooking into `InteractiveModeOptions`
- The default (unnamed) command catches all SQL input via `SqlCommand`
