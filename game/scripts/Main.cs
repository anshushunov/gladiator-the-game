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
	private Label? _fightLog;
	private Label? _fightStatus;
	private Button? _btnSimulateFight;
	private Label? _labelFightSelection;

	private int? _firstFighterIndex;
	private int? _secondFighterIndex;

	public override void _Ready()
	{
		_labelDay = GetNode<Label>("/root/Main/Control/VBoxContainer/StatsHBox/LabelDay");
		_labelMoney = GetNode<Label>("/root/Main/Control/VBoxContainer/StatsHBox/LabelMoney");
		_labelSeed = GetNode<Label>("/root/Main/Control/VBoxContainer/StatsHBox/LabelSeed");
		_listGladiators = GetNode<Label>("/root/Main/Control/VBoxContainer/ListGladiators");
		_fightLog = GetNode<Label>("/root/Main/Control/VBoxContainer/FightLog");
		_fightStatus = GetNode<Label>("/root/Main/Control/VBoxContainer/FightStatus");
		_btnSimulateFight = GetNode<Button>("/root/Main/Control/VBoxContainer/btnSimulateFight");
		_labelFightSelection = GetNode<Label>("/root/Main/Control/VBoxContainer/LabelFightSelection");

		// Подключаем кнопки
		var btnNewGame = GetNode<Button>("/root/Main/Control/VBoxContainer/HireHBox/btnNewGame");
		var btnHireRandom = GetNode<Button>("/root/Main/Control/VBoxContainer/HireHBox/btnHireRandom");
		var btnAdvanceDay = GetNode<Button>("/root/Main/Control/VBoxContainer/HireHBox/btnAdvanceDay");
		var btnSelectFirst = GetNode<Button>("/root/Main/Control/VBoxContainer/FightSelectionHBox/btnSelectFirst");
		var btnSelectSecond = GetNode<Button>("/root/Main/Control/VBoxContainer/FightSelectionHBox/btnSelectSecond");

		btnNewGame.Pressed += OnNewGamePressed;
		btnHireRandom.Pressed += OnHireRandomPressed;
		btnAdvanceDay.Pressed += OnAdvanceDayPressed;
		btnSelectFirst.Pressed += OnSelectFirstFighterPressed;
		btnSelectSecond.Pressed += OnSelectSecondFighterPressed;
		_btnSimulateFight.Pressed += OnSimulateFightPressed;

		UpdateUI();
	}

	/// <summary>
	/// Создаёт новую игру с фиксированным seed.
	/// </summary>
	public void OnNewGamePressed()
	{
		_state = LudusState.NewGame(LudusState.DefaultSeed);
		_firstFighterIndex = null;
		_secondFighterIndex = null;
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
	/// Выбирает первого гладиатора для боя по индексу в списке.
	/// </summary>
	public void OnSelectFirstFighterPressed()
	{
		var alive = _state.AliveGladiators;
		if (alive.Count >= 1)
		{
			_firstFighterIndex = 0;
			_secondFighterIndex = null;
		}
		UpdateUI();
	}

	/// <summary>
	/// Выбирает второго гладиатора для боя по индексу в списке.
	/// </summary>
	public void OnSelectSecondFighterPressed()
	{
		var alive = _state.AliveGladiators;
		if (alive.Count >= 2)
		{
			_secondFighterIndex = 1;
		}
		UpdateUI();
	}

	/// <summary>
	/// Запускает симуляцию боя между двумя выбранными гладиаторами.
	/// </summary>
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

		var g1 = alive[_firstFighterIndex.Value];
		var g2 = alive[_secondFighterIndex.Value];

		if (g1.Id == g2.Id)
		{
			_fightStatus?.SetText("Ошибка: гладиаторы не могут быть одинаковыми");
			return;
		}

		// Запускаем симуляцию
		var result = FightEngine.SimulateFight(g1, g2, 42);

		// Отображаем лог
		var logText = $"=== Fight Log ===\n{result.Log}";
		_fightLog?.SetText(logText);
		
		// Отображаем победителя
		_fightStatus?.SetText($"Победитель: {result.Winner.Name} (HP: {result.Winner.Health}/{result.Winner.MaxHealth})");
	}

	/// <summary>
	/// Обновляет UI на основе текущего состояния.
	/// </summary>
	private void UpdateUI()
	{
		_labelDay?.SetText($"Day: {_state.Day}");
		_labelMoney?.SetText($"Money: {_state.Money}");
		_labelSeed?.SetText($"Seed: {_state.Seed}");

		var gladiatorText = string.Join("\n", _state.Gladiators.Select((g, i) =>
			$"{i}: {g.Name} | HP: {g.Health}/{g.MaxHealth} | Str: {g.Stats.Strength}, Agi: {g.Stats.Agility}, Sta: {g.Stats.Stamina}"
		));

		_listGladiators?.SetText(gladiatorText);

		// Отображаем статус выбора гладиаторов для боя
		string firstText = _firstFighterIndex.HasValue ? $"First: {_state.AliveGladiators[_firstFighterIndex.Value].Name}" : "First: не выбран";
		string secondText = _secondFighterIndex.HasValue ? $"Second: {_state.AliveGladiators[_secondFighterIndex.Value].Name}" : "Second: не выбран";
		_labelFightSelection?.SetText($"Выбор: [{firstText}] vs [{secondText}]");
	}
}
