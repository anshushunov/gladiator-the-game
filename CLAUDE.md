# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Gladiator Ludus Simulator — a Roman arena management game built with Godot 4.6 (.NET/C#) and .NET 8. The project uses **Russian** for documentation, comments, and issue tracking; code identifiers are in English.

## Build & Test Commands

```bash
# Run all tests (xUnit)
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~CombatV2Tests"

# Build the full solution (Core + Tests + Godot project)
dotnet build Ludus.sln

# Build only the domain library
dotnet build src/Ludus.Core
```

The Godot editor is required to run the game itself (`game/` project). The main scene is `game/scenes/Main.tscn`.

## Architecture

**Two-layer separation (strict rule):**

- **`src/Ludus.Core/`** — Pure domain simulation. Immutable records/structs, deterministic logic, no Godot dependency. All simulation and fight logic lives here.
- **`game/`** — Godot UI/presentation layer only. Adapts `LudusState` to visual nodes. No game logic here.

**State management:** `LudusState` is an immutable record. All mutations return a new instance via `with { }` (Redux-like). `Main.cs` holds the single `_state` reference and calls `UpdateUI()` after each change.

**Determinism:** All randomness flows through `SeededRng` (wraps `System.Random`). The seed is stored in `LudusState` and updated after each random operation. Same seed = same game.

**Fight pipeline:** `LudusState.ResolveFight()` → `FightEngine.SimulateFight()` → `CombatResolver.ResolveAttack()` per round → returns `FightResult` with `FightLog`.

## Compiler Settings

`Directory.Build.props` enforces across all projects:
- `TreatWarningsAsErrors: true` — all warnings are errors
- `Nullable: enable` — strict null checking
- `WarningLevel: 4`

## Workflow Rules (from AGENTS.md)

- **1 Issue = 1 PR.** No mixing refactoring and features in one PR.
- **Branch naming:** `issue/<number>-<slug>` from latest `main`.
- **PR must:** build, have tests (if touching `Ludus.Core`), link issue via `Closes #N` or `Fixes #N`.
- **PR creation:** push branch first (`git push -u origin <branch>`), then `gh pr create`. Use single `--body` with `\n` for newlines.
- **PR template** (Russian): Что сделано / Почему / Как проверить / Чеклист.
- **No structural changes** without an ADR in `docs/adr/`.
- **No dead code or TODO without an Issue.**

## Key Domain Types

| Type | Kind | Role |
|------|------|------|
| `LudusState` | record | Central game state (gladiators, money, day, seed) |
| `Gladiator` | struct | Entity with Stats, Health, Name |
| `Stats` | record | Strength / Agility / Stamina (1–10) |
| `FightEngine` | sealed class | Round-based fight orchestrator |
| `CombatResolver` | sealed class | Hit/crit/damage math |
| `CombatModel` | record | Balance parameters |
| `SeededRng` | sealed class | Deterministic RNG via `IRng` |
| `NameGenerator` | sealed class | Deterministic name pool (prefix × cognomen) |

## Godot Conventions

- `Main.cs` attaches to the root `CanvasLayer` in `Main.tscn`.
- Asset loading uses a `TryLoad<T>()` wrapper — missing assets produce warnings, not crashes.
- SFX players are children of the scene root; played on hire/advance-day/fight actions.
- Node paths are resolved via `GetNode<T>("path")` in `_Ready()`.
