namespace Ludus.Core;

/// <summary>
/// Гладиатор — сущность, участвующая в симуляции боя.
/// Имеет уникальный идентификатор, имя и статистику.
/// </summary>
public readonly struct Gladiator
{
    /// <summary>
    /// Уникальный идентификатор гладиатора.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Имя гладиатора.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Статистика гладиатора.
    /// </summary>
    public Stats Stats { get; }

    /// <summary>
    /// Текущее здоровье гладиатора (не входит в Stats — меняется в бою).
    /// </summary>
    public int Health { get; }

    /// <summary>
    /// Максимальное здоровье гладиатора (вычисляется на основе Stats).
    /// </summary>
    public int MaxHealth { get; }

    /// <summary>
    /// Жив ли гладиатор.
    /// </summary>
    public bool IsAlive => Health > 0;

    private const int MinNameLength = 1;
    private const int MaxNameLength = 50;

    public Gladiator(Guid id, string name, Stats stats, int health, int maxHealth)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < MinNameLength || name.Length > MaxNameLength)
            throw new ValidationException(
                $"Имя должно быть непустой строкой длиной от {MinNameLength} до {MaxNameLength} символов");

        if (health < 0 || health > maxHealth)
            throw new ValidationException(
                $"Здоровье должно быть в диапазоне [0, {maxHealth}]");

        Id = id;
        Name = name.Trim();
        Stats = stats;
        Health = health;
        MaxHealth = maxHealth;
    }

    /// <summary>
    /// Создаёт нового гладиатора без проверок (для тестов).
    /// </summary>
    public static Gladiator Create(string name, Stats stats)
    {
        // Максимальное здоровье = Stamina * 10 (базовое правило)
        int maxHealth = stats.Stamina * 10;
        int startingHealth = maxHealth;

        return new Gladiator(Guid.NewGuid(), name, stats, startingHealth, maxHealth);
    }

    /// <summary>
    /// Применяет урон гладиатору. Возвращает новый экземпляр с обновлённым здоровьем.
    /// </summary>
    public Gladiator TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Урон не может быть отрицательным", nameof(damage));
        if (!IsAlive)
            throw new InvalidOperationException("Мёртвый гладиатор не может получать урон");

        int newHealth = Math.Max(0, Health - damage);
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth);
    }

    /// <summary>
    /// Восстанавливает здоровье гладиатора. Возвращает новый экземпляр.
    /// </summary>
    public Gladiator RestoreHealth(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Восстановление не может быть отрицательным", nameof(amount));
        if (!IsAlive)
            throw new InvalidOperationException("Мёртвый гладиатор не может восстанавливать здоровье");

        int newHealth = Math.Min(MaxHealth, Health + amount);
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth);
    }

    public override string ToString()
    {
        return $"Gladiator {{ Id={Id}, Name={Name}, Health={Health}/{MaxHealth}, Stats={Stats} }}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Gladiator other &&
               Id.Equals(other.Id) &&
               Name == other.Name &&
               Stats.Equals(other.Stats) &&
               Health == other.Health &&
               MaxHealth == other.MaxHealth;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Stats, Health, MaxHealth);
    }
}
