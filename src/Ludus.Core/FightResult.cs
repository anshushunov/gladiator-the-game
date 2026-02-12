namespace Ludus.Core;

/// <summary>
/// Результат боя двух гладиаторов.
/// </summary>
public readonly record struct FightResult
{
    /// <summary>
    /// Гладиатор-побитель.
    /// </summary>
    public Gladiator Winner { get; init; }

    /// <summary>
    /// Гладиатор-побеждённый.
    /// </summary>
    public Gladiator Loser { get; init; }

    /// <summary>
    /// Лог боя пошагово.
    /// </summary>
    public FightLog Log { get; init; }

    /// <summary>
    /// Создаёт результат боя.
    /// </summary>
    public FightResult(Gladiator winner, Gladiator loser, FightLog log)
    {
        Winner = winner;
        Loser = loser;
        Log = log;
    }
}
