using Godot;
using Ludus.Core;

namespace Ludus.Game;

/// <summary>
/// Главный скрипт сцены. Управляет UI и интеграцией с Ludus.Core.
/// </summary>
public partial class Main : CanvasLayer
{
    private LudusState _state = LudusState.Empty;
    private Label? _labelDay;
    private Label? _labelMoney;
    private Label? _labelSeed;
    private Label? _listGladiators;

    public override void _Ready()
    {
        _labelDay = GetNode<Label>("/root/Main/VBoxContainer/LabelDay");
        _labelMoney = GetNode<Label>("/root/Main/VBoxContainer/LabelMoney");
        _labelSeed = GetNode<Label>("/root/Main/VBoxContainer/LabelSeed");
        _listGladiators = GetNode<Label>("/root/Main/VBoxContainer/ListGladiators");
        UpdateUI();
    }

    /// <summary>
    /// Создаёт новую игру с фиксированным seed.
    /// </summary>
    public void OnNewGamePressed()
    {
        _state = LudusState.NewGame(LudusState.DefaultSeed);
        UpdateUI();
    }

    /// <summary>
    /// Нанимает случайного гладиатора с RNG-статами.
    /// </summary>
    public void OnHireRandomPressed()
    {
        _state = _state.HireRandomGladiator();
        UpdateUI();
    }

    /// <summary>
    /// Переходит к следующему дню.
    /// </summary>
    public void OnAdvanceDayPressed()
    {
        _state = _state.AdvanceDay();
        UpdateUI();
    }

    /// <summary>
    /// Обновляет UI на основе текущего состояния.
    /// </summary>
    private void UpdateUI()
    {
        _labelDay?.SetText($"Day: {_state.Day}");
        _labelMoney?.SetText($"Money: {_state.Money}");
        _labelSeed?.SetText($"Seed: {_state.Seed}");

        var gladiatorText = string.Join("\n", _state.Gladiators.Select(g =>
            $"{g.Name} | HP: {g.Health}/{g.MaxHealth} | Str: {g.Stats.Strength}, Agi: {g.Stats.Agility}, Sta: {g.Stats.Stamina}"
        ));

        _listGladiators?.SetText(gladiatorText);
    }
}
