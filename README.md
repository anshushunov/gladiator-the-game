# Gladiator Ludus Simulator

Симулятор/песочница про управление лудусом: найм гладиаторов, тренировки, экономика и бои.

## Tech
- Godot 4.x (.NET) + C#
- .NET SDK 8+
- Тесты: xUnit (ядро симуляции отдельно от движка)

## Репозиторий
- `game/` — Godot проект (UI/сцены/визуализация)
- `src/Ludus.Core/` — чистое доменное ядро (симуляция, баланс, бой)
- `src/Ludus.Tests/` — тесты на ядро
- `docs/` — дизайн, ADR, заметки по балансу

## Быстрый старт

### Запуск игры в Godot
1) Установить Godot 4.x .NET и .NET SDK 8+
2) Открыть `game/project.godot` в редакторе Godot
3) Дождаться автоматической сборки C# проекта (внизу статус-бар)
4) Нажать кнопку Play (F5) для запуска

### Запуск тестов ядра
- `dotnet test` — запустить все тесты xUnit в `src/Ludus.Tests/`

## MVP-петля (первая цель)
1) Нанять 3 гладиаторов
2) Назначить тренировки на неделю
3) Провести бой (авторезолв)
4) Получить деньги/травмы/прогресс
5) Повторить

## Name Generator (Issue #14)
- Core API:
  - `INameGenerator.GenerateNext()` -> returns next unique `prefix + cognomen` name, throws `NameGenerationException` when pool is exhausted.
  - `INameGenerator.TryGenerate(out string name)` -> returns `false` when pool is exhausted.
- Determinism:
  - `NameGenerator` uses `SeededRng`; same `seed + input lists` produces the same sequence.
- Validation rules:
  - `prefixes` and `cognomens` must be non-empty.
  - list items are trimmed and cannot be empty.
  - duplicates are rejected with `ValidationException`.
- Scope:
  - implementation is in `src/Ludus.Core/`.
  - behavior is covered by tests in `src/Ludus.Tests/NameGeneratorTests.cs` and `src/Ludus.Tests/NameGeneratorStabilityTests.cs`.

## Combat v2 (Issue #15)
- Core contract:
  - `CombatResolver` in `src/Ludus.Core/CombatResolver.cs` resolves one attack and emits combat events.
  - `CombatModel` in `src/Ludus.Core/CombatModel.cs` defines deterministic formula parameters.
- Formula order:
  - hit roll (attacker agility vs defender agility, clamped by model bounds)
  - base damage with variance
  - base defense reduction (from defender stamina)
  - crit roll (only after successful hit), then crit multiplier
  - final floor policy (`MinDamageAfterDefense`, default is `1`)
- Event stream:
  - `Hit`, `Miss`, `Crit`, `DamageApplied`, `Kill`, `FightEnd`
- Determinism:
  - all random decisions are made through `IRng`/`SeededRng`
  - same inputs + same `seed` produce identical combat logs
- Coverage:
  - combat invariants and regression scenarios are in `src/Ludus.Tests/CombatV2Tests.cs`.
