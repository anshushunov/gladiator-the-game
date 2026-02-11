namespace Ludus.Core;

/// <summary>
/// Статистика гладиатора: сила, выносливость, ловкость.
/// </summary>
public readonly record struct Stats
{
    private const int MinStatValue = 1;
    private const int MaxStatValue = 10;

    public int Strength { get; init; }
    public int Agility { get; init; }
    public int Stamina { get; init; }

    public Stats(int strength, int agility, int stamina)
    {
        if (strength < MinStatValue || strength > MaxStatValue)
            throw new ArgumentOutOfRangeException(
                nameof(strength),
                $"Значение силы должно быть в диапазоне [{MinStatValue}, {MaxStatValue}]");
        if (agility < MinStatValue || agility > MaxStatValue)
            throw new ArgumentOutOfRangeException(
                nameof(agility),
                $"Значение ловкости должно быть в диапазоне [{MinStatValue}, {MaxStatValue}]");
        if (stamina < MinStatValue || stamina > MaxStatValue)
            throw new ArgumentOutOfRangeException(
                nameof(stamina),
                $"Значение выносливости должно быть в диапазоне [{MinStatValue}, {MaxStatValue}]");

        Strength = strength;
        Agility = agility;
        Stamina = stamina;
    }

    public static Stats Default => new(5, 5, 5);

    public static Stats Random(System.Random rand)
    {
        return new Stats(
            rand.Next(MinStatValue, MaxStatValue + 1),
            rand.Next(MinStatValue, MaxStatValue + 1),
            rand.Next(MinStatValue, MaxStatValue + 1)
        );
    }
}
