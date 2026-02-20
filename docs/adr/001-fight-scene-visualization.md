# ADR-001: Визуализация боя — FightScene

## Статус
Принято

## Контекст
Бой вычисляется мгновенно через `LudusState.ResolveFight()`, результат отображается стеной текста в `RichTextLabel`. Пользователю сложно следить за ходом боя. Нужна визуальная арена с анимациями, health-барами и эффектами.

## Решение
Создать отдельную сцену `FightScene.tscn` — **replay-проигрыватель** `FightLog.Events`.

### Принципы
- **Core не меняется.** Бой вычисляется мгновенно как и раньше. `FightScene` только воспроизводит лог.
- **Fallback.** Если `FightScene.tscn` не загрузился — показываем текстовый лог как раньше.
- **Скорость.** Поддержка 1x / 2x / 4x и кнопки Skip.

### Архитектура
```
Main.cs
  ├── ResolveFight() — вычисление (Core)
  └── FightScene — replay-проигрыватель (Godot)
        ├── FighterVisual — визуал бойца (спрайт, анимации, health bar)
        ├── HealthBarUI — анимированная полоска HP
        └── DamagePopup — всплывающие числа урона
```

### Поток данных
1. `Main.cs` захватывает pre-fight состояние гладиаторов
2. Вызывает `ResolveFight()` — получает `FightResult` с `FightLog`
3. Создаёт `FightScene`, передаёт `FightResult` + pre-fight гладиаторов
4. `FightScene` итерирует `FightLog.Events`, анимируя каждое событие
5. По завершении эмитирует `FightFinished` — `Main.cs` возвращает UI

### Новые файлы
- `game/scenes/fight/FightScene.tscn` — сцена арены
- `game/scripts/fight/FightScene.cs` — контроллер replay
- `game/scripts/fight/FighterVisual.cs` — визуал бойца
- `game/scripts/fight/HealthBarUI.cs` — health bar
- `game/scripts/fight/DamagePopup.cs` — всплывающие числа
- `game/assets/fight/fighter_placeholder_left.svg` — placeholder левый
- `game/assets/fight/fighter_placeholder_right.svg` — placeholder правый

## Последствия
- Бой становится зрелищным и понятным
- Core-слой остаётся чистым и тестируемым
- При добавлении реальных спрайтов — только замена текстур/анимаций в `FighterVisual`
