namespace Ludus.Core;

/// <summary>
/// Gladiator domain entity.
/// </summary>
public readonly struct Gladiator
{
    public Guid Id { get; }
    public string Name { get; }
    public Stats Stats { get; }
    public int Health { get; }
    public int MaxHealth { get; }
    public TrainingType? CurrentTraining { get; }
    public Injury? CurrentInjury { get; }
    public int Morale { get; }
    public int Fatigue { get; }
    public ContractState Contract { get; }

    public bool IsAlive => Health > 0;
    public bool IsInjured => CurrentInjury.HasValue;
    public bool CanFight => IsAlive && !IsInjured;

    private const int MinNameLength = 1;
    private const int MaxNameLength = 50;

    public Gladiator(
        Guid id,
        string name,
        Stats stats,
        int health,
        int maxHealth,
        TrainingType? currentTraining = null,
        Injury? currentInjury = null,
        int morale = ConditionModel.DefaultMorale,
        int fatigue = ConditionModel.DefaultFatigue,
        ContractState? contract = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < MinNameLength || name.Length > MaxNameLength)
            throw new ValidationException(
                $"Name must be non-empty and between {MinNameLength} and {MaxNameLength} characters");

        if (health < 0 || health > maxHealth)
            throw new ValidationException($"Health must be in range [0, {maxHealth}]");

        if (morale < ConditionModel.MinMorale || morale > ConditionModel.MaxMorale)
            throw new ValidationException(
                $"Morale must be in range [{ConditionModel.MinMorale}, {ConditionModel.MaxMorale}]");

        if (fatigue < ConditionModel.MinFatigue || fatigue > ConditionModel.MaxFatigue)
            throw new ValidationException(
                $"Fatigue must be in range [{ConditionModel.MinFatigue}, {ConditionModel.MaxFatigue}]");

        var contractValue = contract ?? ContractState.Default;
        contractValue.Validate();

        Id = id;
        Name = name.Trim();
        Stats = stats;
        Health = health;
        MaxHealth = maxHealth;
        CurrentTraining = currentTraining;
        CurrentInjury = currentInjury;
        Morale = morale;
        Fatigue = fatigue;
        Contract = contractValue;
    }

    public static Gladiator Create(string name, Stats stats)
    {
        int maxHealth = stats.Stamina * 10;
        int startingHealth = maxHealth;
        return new Gladiator(Guid.NewGuid(), name, stats, startingHealth, maxHealth);
    }

    public Gladiator TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative", nameof(damage));
        if (!IsAlive)
            throw new InvalidOperationException("Dead gladiator cannot take damage");

        int newHealth = Math.Max(0, Health - damage);
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth, CurrentTraining, CurrentInjury, Morale, Fatigue, Contract);
    }

    public Gladiator RestoreHealth(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
        if (!IsAlive)
            throw new InvalidOperationException("Dead gladiator cannot be healed");

        int newHealth = Math.Min(MaxHealth, Health + amount);
        return new Gladiator(Id, Name, Stats, newHealth, MaxHealth, CurrentTraining, CurrentInjury, Morale, Fatigue, Contract);
    }

    public Gladiator AssignTraining(TrainingType type)
    {
        if (!IsAlive)
            throw new InvalidOperationException("Dead gladiator cannot train");
        if (IsInjured)
            throw new InvalidOperationException("Injured gladiator cannot train");

        int currentStatValue = GetStatValue(type);
        if (currentStatValue >= 10)
            throw new InvalidOperationException($"Stat {type} is already maxed (10)");

        return new Gladiator(Id, Name, Stats, Health, MaxHealth, type, CurrentInjury, Morale, Fatigue, Contract);
    }

    public Gladiator ClearTraining()
    {
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, null, CurrentInjury, Morale, Fatigue, Contract);
    }

    public Gladiator ApplyInjury(Injury injury)
    {
        if (!IsAlive)
            throw new InvalidOperationException("Dead gladiator cannot be injured");

        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, injury, Morale, Fatigue, Contract);
    }

    public Gladiator TickRecovery()
    {
        if (!CurrentInjury.HasValue)
            return this;

        var tickedInjury = CurrentInjury.Value.Tick();
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, tickedInjury, Morale, Fatigue, Contract);
    }

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
        return new Gladiator(Id, Name, newStats, newHealth, newMaxHealth, newTraining, CurrentInjury, Morale, Fatigue, Contract);
    }

    public Gladiator WithMorale(int morale)
    {
        morale = Math.Clamp(morale, ConditionModel.MinMorale, ConditionModel.MaxMorale);
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, CurrentInjury, morale, Fatigue, Contract);
    }

    public Gladiator WithFatigue(int fatigue)
    {
        fatigue = Math.Clamp(fatigue, ConditionModel.MinFatigue, ConditionModel.MaxFatigue);
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, CurrentInjury, Morale, fatigue, Contract);
    }

    public Gladiator WithContract(ContractState contract)
    {
        contract.Validate();
        return new Gladiator(Id, Name, Stats, Health, MaxHealth, CurrentTraining, CurrentInjury, Morale, Fatigue, contract);
    }

    public Gladiator TickContractDay()
    {
        return WithContract(Contract.TickDay());
    }

    public Gladiator RenewContractIfNeeded()
    {
        return WithContract(Contract.RenewIfNeeded());
    }

    public Gladiator MarkContractOverdue()
    {
        return WithContract(Contract.MarkOverdueDay());
    }

    public Gladiator ClearContractOverdue()
    {
        return WithContract(Contract.ClearOverdueIfPaid());
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
        string contract = $", Wage={Contract.Terms.DailyWage}/d, Contract={Contract.DaysRemaining}d, Overdue={Contract.OverdueDays}";
        return $"Gladiator {{ Id={Id}, Name={Name}, Health={Health}/{MaxHealth}, Stats={Stats}, Morale={Morale}, Fatigue={Fatigue}{training}{injury}{contract} }}";
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
               CurrentInjury == other.CurrentInjury &&
               Morale == other.Morale &&
               Fatigue == other.Fatigue &&
               Contract == other.Contract;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(Stats);
        hash.Add(Health);
        hash.Add(MaxHealth);
        hash.Add(CurrentTraining);
        hash.Add(CurrentInjury);
        hash.Add(Morale);
        hash.Add(Fatigue);
        hash.Add(Contract);
        return hash.ToHashCode();
    }
}
