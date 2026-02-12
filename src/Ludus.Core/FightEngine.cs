using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

/// <summary>
/// Движок боя: реализует симуляцию пошагового поединка двух гладиаторов.
/// Весь сайд-эффект через IRng — детерминизм обеспечиваетсяseed.
/// </summary>
public static class FightEngine
{
    /// <summary>
    /// Симулирует бой двух гладиаторов.
    /// Возвращает результат с детальным логом каждого раунда.
    /// </summary>
    /// <param name="g1">Первый гладиатор.</param>
    /// <param name="g2">Второй гладиатор.</param>
    /// <param name="rng">Deterministic RNG. Используется для всех решений в бою.</param>
    /// <returns>Результат боя: победитель, проигравший и лог.</returns>
    public static FightResult SimulateFight(Gladiator g1, Gladiator g2, IRng rng)
    {
        if (!g1.IsAlive) throw new ArgumentException("Первый гладиатор должен быть жив", nameof(g1));
        if (!g2.IsAlive) throw new ArgumentException("Второй гладиатор должен быть жив", nameof(g2));
        if (rng == null) throw new ArgumentNullException(nameof(rng));

        // Создаём копии гладиаторов для боя (иммутабельность)
        var fighter1 = g1;
        var fighter2 = g2;

        var events = new List<FightLog.Event>();
        int round = 0;

        // В бою участвуют живые гладиаторы
        var activeFighters = new List<Gladiator> { fighter1, fighter2 };

        while (activeFighters.Count >= 2)
        {
            round++;

            // Выбираем атакующего и защитника на основе очереди ходов
            // Для простоты: чередуем атакующих (G1 -> G2 -> G1 -> ...)
            var attacker = activeFighters[round % 2 == 0 ? 0 : 1];
            var defender = activeFighters[round % 2 == 0 ? 1 : 0];

            // Используем RNG для определения: попадает или нет
            // Базовый шанс попадания = 50% + (Agility / 20) — чем ловчее, тем чаще попадает
            // Максимальный шанс = 100%
            double hitChance = 0.5 + (defender.Stats.Agility / 20.0);
            hitChance = Math.Min(1.0, hitChance);

            if (rng.NextDouble() < hitChance)
            {
                // Попадание — рассчитываем урон
                // Базовый урон = Strength * 2, вариативность ±20%
                int baseDamage = attacker.Stats.Strength * 2;
                double variance = rng.NextDouble(); // [0.0, 1.0)
                float damageMultiplier = (float)(0.8 + (variance * 0.4)); // [0.8, 1.2]
                int damage = (int)Math.Round(baseDamage * damageMultiplier);

                // Урон может быть уменьшен защитой (Agility даёт частичный блок)
                float blockChance = defender.Stats.Agility / 50.0f; // до 20% блока при Max agility
                if (rng.NextDouble() < blockChance)
                {
                    // Частичный блок — снижаем урон на 50%
                    damage = (int)Math.Round(damage * 0.5f);
                }

                // Наносим урон
                var damagedDefender = defender.TakeDamage(damage);

                events.Add(new FightLog.Event
                {
                    Round = round,
                    AttackerName = attacker.Name,
                    DefenderName = defender.Name,
                    Type = FightLog.EventType.Hit,
                    Value = damage
                });

                // Проверяем, убит ли защитник
                if (!damagedDefender.IsAlive)
                {
                    events.Add(new FightLog.Event
                    {
                        Round = round,
                        AttackerName = attacker.Name,
                        DefenderName = defender.Name,
                        Type = FightLog.EventType.Kill,
                        Value = 0
                    });

                    // Бой окончен возвращаем результат
                    events.Add(new FightLog.Event
                    {
                        Round = round,
                        AttackerName = attacker.Name,
                        DefenderName = defender.Name,
                        Type = FightLog.EventType.FightEnd,
                        Value = 0
                    });

                    return new FightResult(attacker, damagedDefender, new FightLog(events));
                }

                // Обновляем защитника с новым здоровьем
                activeFighters[round % 2 == 0 ? 1 : 0] = damagedDefender;
                fighter1 = activeFighters[0];
                fighter2 = activeFighters[1];
            }
            else
            {
                // Промах
                events.Add(new FightLog.Event
                {
                    Round = round,
                    AttackerName = attacker.Name,
                    DefenderName = defender.Name,
                    Type = FightLog.EventType.Miss,
                    Value = 0
                });
            }
        }

        // Должно быть, бой завершился (в цикле возвращается)
        // Если дошли сюда — ошибка в логике
        throw new InvalidOperationException("Бой завершился некорректно");
    }

    /// <summary>
    /// Симулирует бой двух гладиаторов с фиксированным seed.
    /// </summary>
    public static FightResult SimulateFight(Gladiator g1, Gladiator g2, int seed = 42)
    {
        var rng = new SeededRng(seed);
        return SimulateFight(g1, g2, rng);
    }
}
