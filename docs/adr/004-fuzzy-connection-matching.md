# ADR-004: Fuzzy connection matching with FuzzySharp

## Status
Accepted

## Context
Users want to connect to saved connections by typing partial names (e.g., "dev" or "sc34").

## Decision
Use FuzzySharp for fuzzy matching with a two-tier algorithm:
1. Substring containment → score 100
2. `Fuzz.Ratio` fallback → threshold 70

## Consequences
- `Fuzz.PartialRatio` with threshold 60 was too lenient (e.g., "so1" matched "ow1"), so it was replaced
- Multiple matches show a Spectre.Console `SelectionPrompt` for interactive picking
- Single exact match connects directly
