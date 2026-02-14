using System.Linq;
using Godot;
using Ludus.Core;

namespace Ludus.Game;

/// <summary>
/// Главный скрипт сцены. Управляет UI и интеграцией с Ludus.Core.
/// Предоставляет выбор двух гладиаторов и запуск симуляции боя.
/// </summary>
public partial class Main : CanvasLayer
{
	private LudusState _state = LudusState.Empty;
	private Label? _labelDay;
	private Label? _labelMoney;
	private Label? _labelSeed;
	private Label? _listGladiators;

	// UI для боя
	private RichTextLabel? _fightLog;
	private Label? _fightStatus;
	private Button? _btnSimulateFight;
	private Label? _labelFightSelection;

	private int? _firstFighterIndex;
	private int? _secondFighterIndex;

	public override void _Ready()
	{
		_labelDay = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/StatsHBox/LabelDay");
		_labelMoney = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/StatsHBox/LabelMoney");
		_labelSeed = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/StatsHBox/LabelSeed");
		_listGladiators = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/ListGladiators");
		_fightLog = GetNode<RichTextLabel>("/root/Main/Control/CenterContainer/VBoxContainer/FightLog");
		_fightStatus = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/FightStatus");
		_btnSimulateFight = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/btnSimulateFight");
		_labelFightSelection = GetNode<Label>("/root/Main/Control/CenterContainer/VBoxContainer/LabelFightSelection");

		var btnNewGame = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/HireHBox/btnNewGame");
		var btnHireRandom = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/HireHBox/btnHireRandom");
		var btnAdvanceDay = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/HireHBox/btnAdvanceDay");
		var btnSelectFirst = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/FightSelectionHBox/btnSelectFirst");
		var btnSelectSecond = GetNode<Button>("/root/Main/Control/CenterContainer/VBoxContainer/FightSelectionHBox/btnSelectSecond");

		btnNewGame.Pressed += OnNewGamePressed;
		btnHireRandom.Pressed += OnHireRandomPressed;
		btnAdvanceDay.Pressed += OnAdvanceDayPressed;
		btnSelectFirst.Pressed += OnSelectFirstFighterPressed;
		btnSelectSecond.Pressed += OnSelectSecondFighterPressed;
		_btnSimulateFight!.Pressed += OnSimulateFightPressed;

		UpdateUI();
	}

	public void OnNewGamePressed()
	{
		_state = LudusState.NewGame(LudusState.DefaultSeed);
		_firstFighterIndex = null;
		_secondFighterIndex = null;
		UpdateUI();
	}

	public void OnHireRandomPressed()
	{
		_state = _state.HireRandomGladiator();
		UpdateUI();
	}

	public void OnAdvanceDayPressed()
	{
		_state = _state.AdvanceDay();
		UpdateUI();
	}

	public void OnSelectFirstFighterPressed()
	{
		var alive = _state.AliveGladiators;
		if (alive.Count >= 1)
		{
			_firstFighterIndex = 0;
			if (_secondFighterIndex == _firstFighterIndex)
			{
				_secondFighterIndex = null;
			}
		}
		UpdateUI();
	}

	public void OnSelectSecondFighterPressed()
	{
		var alive = _state.AliveGladiators;
		if (alive.Count >= 2)
		{
			_secondFighterIndex = 1;
		}
		UpdateUI();
	}

	public void OnSimulateFightPressed()
	{
		if (!_firstFighterIndex.HasValue || !_secondFighterIndex.HasValue)
		{
			_fightStatus?.SetText("Ошибка: выберите двух гладиаторов");
			return;
		}

		var alive = _state.AliveGladiators;
		if (_firstFighterIndex.Value >= alive.Count || _secondFighterIndex.Value >= alive.Count)
		{
			_fightStatus?.SetText("Ошибка: неверные индексы гладиаторов");
			return;
		}

		var first = alive[_firstFighterIndex.Value];
		var second = alive[_secondFighterIndex.Value];

		if (first.Id == second.Id)
		{
			_fightStatus?.SetText("Ошибка: гладиаторы не могут быть одинаковыми");
			return;
		}

		var (updatedState, result) = _state.ResolveFight(first.Id, second.Id);
		_state = updatedState;
		_firstFighterIndex = null;
		_secondFighterIndex = null;

		if (_fightLog is not null)
		{
			_fightLog.Text = result.Log.ToString();
			_fightLog.ScrollToLine(0);
		}
		_fightStatus?.SetText($"Победитель: {result.Winner.Name} (HP: {result.Winner.Health}/{result.Winner.MaxHealth})");

		UpdateUI();
	}

	private void UpdateUI()
	{
		NormalizeSelection();

		_labelDay?.SetText($"Day: {_state.Day}");
		_labelMoney?.SetText($"Money: {_state.Money}");
		_labelSeed?.SetText($"Seed: {_state.Seed}");

		var gladiatorText = string.Join("\n", _state.Gladiators.Select((g, i) =>
			$"{i}: {g.Name} | HP: {g.Health}/{g.MaxHealth} | Str: {g.Stats.Strength}, Agi: {g.Stats.Agility}, Sta: {g.Stats.Stamina}"));
		_listGladiators?.SetText(gladiatorText);

		var alive = _state.AliveGladiators;
		string firstText = _firstFighterIndex.HasValue ? $"First: {alive[_firstFighterIndex.Value].Name}" : "First: не выбран";
		string secondText = _secondFighterIndex.HasValue ? $"Second: {alive[_secondFighterIndex.Value].Name}" : "Second: не выбран";
		_labelFightSelection?.SetText($"Выбор: [{firstText}] vs [{secondText}]");
		_btnSimulateFight?.SetDisabled(!(_firstFighterIndex.HasValue && _secondFighterIndex.HasValue));
	}

	private void NormalizeSelection()
	{
		int aliveCount = _state.AliveGladiators.Count;

		if (_firstFighterIndex.HasValue && _firstFighterIndex.Value >= aliveCount)
		{
			_firstFighterIndex = null;
		}

		if (_secondFighterIndex.HasValue && _secondFighterIndex.Value >= aliveCount)
		{
			_secondFighterIndex = null;
		}

		if (_firstFighterIndex.HasValue && _secondFighterIndex.HasValue &&
			_firstFighterIndex.Value == _secondFighterIndex.Value)
		{
			_secondFighterIndex = null;
		}
	}
}

