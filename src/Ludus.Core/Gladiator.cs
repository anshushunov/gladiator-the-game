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
    /// Текущая назначенная тренировка (null = нет тренировки).
    /// </summary>
    public TrainingType? CurrentTraining { get; }

    /// <summary>
    /// Текущая травма (null = здоров).
    /// </summary>
    public Injury? CurrentInjury { get; }

    /// <summary>
    /// Жив ли гладиатор.
    /// </summary>
    public bool IsAlive => Health > 0;

    /// <summary>
    /// Есть ли травма.
    /// </summary>
    public bool IsInjured => CurrentInjury.HasValue;

    /// <summary>
    /// Может ли участвовать в бою (жив и не травмирован).
    /// </summary>
    public bool CanFight => IsAlive && !IsInjured;

    private const int MinNameLength = 1;
    private const int MaxNameLength = 50;

    public Gladiator(Guid id, string name, Stats stats, int health, int maxHealth,
        TrainingType? currentTraining = null, Injury? currentInjury = null)
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
        CurrentTraining = currentTraining;
        CurrentInjury = currentInjury;
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
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth, CurrentTraining, CurrentInjury);
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
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth, CurrentTraining, CurrentInjury);
    }

    /// <summary>
    /// Назначает тренировку гладиатору. Гладиатор должен быть жив, не травмирован, стат не на максимуме.
    /// </summary>
    public Gladiator AssignTraining(TrainingType type)
    {
        if (!IsAlive)
            throw new InvalidOperationException("Мёртвый гладиатор не может тренироваться");

        if (IsInjured)
            throw new InvalidOperationException("Травмированный гладиатор не может тренироваться");

        int currentStatValue = GetStatValue(type);
        if (currentStatValue >= 10)
            throw new InvalidOperationException(
                $"Стат {type} уже на максимуме (10), тренировка невозможна");

        return new Gladiator(Id, Name, Stats, Health, MaxHealth, type, CurrentInjury);
    }

    /// <summary>
    /// Снимает текущую тренировку.
    /// </summary>
    public Gladiator ClearTraining()
    {
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, null, CurrentInjury);
    }

    /// <summary>
    /// Применяет травму гладиатору. Гладиатор должен быть жив.
    /// </summary>
    public Gladiator ApplyInjury(Injury injury)
    {
        if (!IsAlive)
            throw new InvalidOperationException("Мёртвый гладиатор не может получить травму");

        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, injury);
    }

    /// <summary>
    /// Уменьшает оставшиеся дни восстановления. Снимает травму если дни закончились.
    /// </summary>
    public Gladiator TickRecovery()
    {
        if (!CurrentInjury.HasValue)
            return this;

        var tickedInjury = CurrentInjury.Value.Tick();
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, tickedInjury);
    }

    /// <summary>
    /// Применяет прирост стата от тренировки. Если стат достигает 10 — тренировка автоматически снимается.
    /// При росте Stamina: MaxHealth и Health увеличиваются на 10.
    /// </summary>
    public Gladiator ApplyStatGain(TrainingType type)
    {
        var newStats = type switch
        {
            TrainingType.Strength => Stats with { Strength = Stats.Strength + 1 },
            TrainingType.Agility => Stats with { Agility = Stats.Agility + 1 },
            TrainingType.Stamina => Stats with { Stamina = Stats.Stamina + 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        int newMaxHealth = MaxHealth;
        int newHealth = Health;

        if (type == TrainingType.Stamina)
        {
            newMaxHealth += 10;
            newHealth += 10;
        }

        int newStatValue = type switch
        {
            TrainingType.Strength => newStats.Strength,
            TrainingType.Agility => newStats.Agility,
            TrainingType.Stamina => newStats.Stamina,
            _ => 0
        };

        TrainingType? newTraining = newStatValue >= 10 ? null : CurrentTraining;

        return new Gladiator(Id, Name, newStats, newHealth, newMaxHealth, newTraining, CurrentInjury);
    }

    private int GetStatValue(TrainingType type) => type switch
    {
        TrainingType.Strength => Stats.Strength,
        TrainingType.Agility => Stats.Agility,
        TrainingType.Stamina => Stats.Stamina,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public override string ToString()
    {
        string training = CurrentTraining.HasValue ? $", Training={CurrentTraining.Value}" : "";
        string injury = CurrentInjury.HasValue ? $", Injury={CurrentInjury.Value.Type}({CurrentInjury.Value.RecoveryDaysLeft}d)" : "";
        return $"Gladiator {{ Id={Id}, Name={Name}, Health={Health}/{MaxHealth}, Stats={Stats}{training}{injury} }}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Gladiator other &&
               Id.Equals(other.Id) &&
               Name == other.Name &&
               Stats.Equals(other.Stats) &&
               Health == other.Health &&
               MaxHealth == other.MaxHealth &&
               CurrentTraining == other.CurrentTraining &&
               CurrentInjury == other.CurrentInjury;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Stats, Health, MaxHealth, CurrentTraining, CurrentInjury);
    }
}
