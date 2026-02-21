using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ludus.Core;

namespace Ludus.Game;

/// <summary>
/// Main scene controller for UI and integration with Ludus.Core.
/// </summary>
public partial class Main : CanvasLayer
{
	private const string PortraitPath = "res://assets/cards/gladiator_portrait_placeholder.svg";
	private const string HireSfxPath = "res://assets/sfx/hire.wav";
	private const string AdvanceDaySfxPath = "res://assets/sfx/advance_day.wav";
	private const string FightSfxPath = "res://assets/sfx/fight.wav";
	private const string FightScenePath = "res://scenes/fight/FightScene.tscn";

	private LudusState _state = LudusState.Empty;

	private Control? _root;
	private GridContainer? _mainGrid;
	private Label? _labelDay;
	private Label? _labelMoney;
	private Label? _labelSeed;
	private ItemList? _listGladiators;

	private OptionButton? _firstFighterSelect;
	private OptionButton? _secondFighterSelect;
	private Label? _selectionStatus;
	private RichTextLabel? _fightLog;
	private Label? _fightStatus;
	private Button? _btnSimulateFight;

	private AudioStreamPlayer? _sfxHire;
	private AudioStreamPlayer? _sfxAdvanceDay;
	private AudioStreamPlayer? _sfxFight;
	private Texture2D? _portraitTexture;

	private Label? _labelSelectedGladiator;
	private OptionButton? _trainingSelect;
	private Button? _btnAssignTraining;
	private Label? _trainingStatus;

	private Guid? _selectedGladiatorId;
	private Guid? _firstFighterId;
	private Guid? _secondFighterId;
	private List<Gladiator> _aliveForSelection = [];

	public override void _Ready()
	{
		_root = GetNode<Control>("Control");
		_mainGrid = GetNode<GridContainer>("Control/MarginContainer/RootVBox/MainGrid");
		_labelDay = GetNode<Label>("Control/MarginContainer/RootVBox/TopStats/DayPanel/DayMargin/DayBox/LabelDay");
		_labelMoney = GetNode<Label>("Control/MarginContainer/RootVBox/TopStats/MoneyPanel/MoneyMargin/MoneyBox/LabelMoney");
		_labelSeed = GetNode<Label>("Control/MarginContainer/RootVBox/TopStats/SeedPanel/SeedMargin/SeedBox/LabelSeed");
		_listGladiators = GetNode<ItemList>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterVBox/ListGladiators");
		_labelSelectedGladiator = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterVBox/TrainingSection/LabelSelectedGladiator");
		_trainingSelect = GetNode<OptionButton>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterVBox/TrainingSection/TrainingRow/TrainingSelect");
		_btnAssignTraining = GetNode<Button>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterVBox/TrainingSection/TrainingRow/btnAssignTraining");
		_trainingStatus = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterVBox/TrainingSection/TrainingStatus");

		_firstFighterSelect = GetNode<OptionButton>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/FighterSelects/FirstPicker/FirstFighterSelect");
		_secondFighterSelect = GetNode<OptionButton>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/FighterSelects/SecondPicker/SecondFighterSelect");
		_selectionStatus = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/SelectionStatus");
		_fightLog = GetNode<RichTextLabel>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/FightLog");
		_fightStatus = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/FightStatus");
		_btnSimulateFight = GetNode<Button>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightVBox/btnSimulateFight");

		var btnNewGame = GetNode<Button>("Control/MarginContainer/RootVBox/ActionRow/btnNewGame");
		var btnHireRandom = GetNode<Button>("Control/MarginContainer/RootVBox/ActionRow/btnHireRandom");
		var btnAdvanceDay = GetNode<Button>("Control/MarginContainer/RootVBox/ActionRow/btnAdvanceDay");

		_sfxHire = GetNode<AudioStreamPlayer>("SfxHire");
		_sfxAdvanceDay = GetNode<AudioStreamPlayer>("SfxAdvanceDay");
		_sfxFight = GetNode<AudioStreamPlayer>("SfxFight");

		_portraitTexture = TryLoad<Texture2D>(PortraitPath);
		if (_sfxHire is not null)
		{
			_sfxHire.Stream = TryLoad<AudioStream>(HireSfxPath);
		}
		if (_sfxAdvanceDay is not null)
		{
			_sfxAdvanceDay.Stream = TryLoad<AudioStream>(AdvanceDaySfxPath);
		}
		if (_sfxFight is not null)
		{
			_sfxFight.Stream = TryLoad<AudioStream>(FightSfxPath);
		}

		btnNewGame.Pressed += OnNewGamePressed;
		btnHireRandom.Pressed += OnHireRandomPressed;
		btnAdvanceDay.Pressed += OnAdvanceDayPressed;
		_btnSimulateFight!.Pressed += OnSimulateFightPressed;
		_listGladiators!.ItemSelected += OnGladiatorSelected;
		_btnAssignTraining!.Pressed += OnAssignTrainingPressed;
		_firstFighterSelect!.ItemSelected += OnFirstFighterSelected;
		_secondFighterSelect!.ItemSelected += OnSecondFighterSelected;
		_root!.Resized += OnRootResized;

		UpdateLayoutByWidth();
		UpdateUI();
	}

	public void OnNewGamePressed()
	{
		_state = LudusState.NewGame(LudusState.DefaultSeed);
		_selectedGladiatorId = null;
		_firstFighterId = null;
		_secondFighterId = null;
		UpdateUI();
	}

	public void OnHireRandomPressed()
	{
		_state = _state.HireRandomGladiator();
		TryPlay(_sfxHire);
		UpdateUI();
	}

	public void OnAdvanceDayPressed()
	{
		_state = _state.AdvanceDay();
		TryPlay(_sfxAdvanceDay);
		UpdateUI();
	}

	private void OnFirstFighterSelected(long index)
	{
		if (index < 0 || index >= _aliveForSelection.Count)
		{
			_firstFighterId = null;
			UpdateUI();
			return;
		}

		_firstFighterId = _aliveForSelection[(int)index].Id;
		if (_firstFighterId == _secondFighterId)
		{
			_secondFighterId = null;
		}

		UpdateUI();
	}

	private void OnSecondFighterSelected(long index)
	{
		if (index < 0 || index >= _aliveForSelection.Count)
		{
			_secondFighterId = null;
			UpdateUI();
			return;
		}

		_secondFighterId = _aliveForSelection[(int)index].Id;
		if (_secondFighterId == _firstFighterId)
		{
			_firstFighterId = null;
		}

		UpdateUI();
	}

	public void OnSimulateFightPressed()
	{
		if (!_firstFighterId.HasValue || !_secondFighterId.HasValue)
		{
			_fightStatus!.Text = "Select two different fighters.";
			return;
		}

		if (_firstFighterId.Value == _secondFighterId.Value)
		{
			_fightStatus!.Text = "Fighters must be different.";
			return;
		}

		// Capture pre-fight state before resolving
		var preFightFirst = _state.GetGladiator(_firstFighterId.Value);
		var preFightSecond = _state.GetGladiator(_secondFighterId.Value);

		var (updatedState, result) = _state.ResolveFight(_firstFighterId.Value, _secondFighterId.Value);
		_state = updatedState;
		_firstFighterId = null;
		_secondFighterId = null;

		// Try to launch visual fight scene
		var scene = TryLoad<PackedScene>(FightScenePath);
		if (scene is not null)
		{
			var fightScene = scene.Instantiate<FightScene>();
			AddChild(fightScene);
			_root!.Visible = false;
			fightScene.FightFinished += () =>
			{
				fightScene.QueueFree();
				_root!.Visible = true;
				UpdateUI();
			};
			fightScene.Initialize(result, preFightFirst, preFightSecond);
			return;
		}

		// Fallback: text log
		TryPlay(_sfxFight);
		_fightLog!.Text = result.Log.ToString();
		_fightLog.ScrollToLine(0);
		_fightStatus!.Text = $"Winner: {result.Winner.Name} (HP {result.Winner.Health}/{result.Winner.MaxHealth})";

		UpdateUI();
	}

	private void OnGladiatorSelected(long index)
	{
		if (index < 0 || index >= _state.Gladiators.Count)
		{
			_selectedGladiatorId = null;
			UpdateUI();
			return;
		}

		_selectedGladiatorId = _state.Gladiators[(int)index].Id;
		UpdateUI();
	}

	private void OnAssignTrainingPressed()
	{
		if (!_selectedGladiatorId.HasValue)
		{
			return;
		}

		var selectedIndex = _trainingSelect!.Selected;
		if (selectedIndex == 0)
		{
			_state = _state.ClearTraining(_selectedGladiatorId.Value);
		}
		else
		{
			var trainingType = selectedIndex switch
			{
				1 => TrainingType.Strength,
				2 => TrainingType.Agility,
				3 => TrainingType.Stamina,
				_ => TrainingType.Strength
			};
			_state = _state.AssignTraining(_selectedGladiatorId.Value, trainingType);
		}

		UpdateUI();
	}

	private void OnRootResized()
	{
		UpdateLayoutByWidth();
	}

	private void UpdateLayoutByWidth()
	{
		if (_root is null || _mainGrid is null)
		{
			return;
		}

		var width = _root.Size.X;
		_mainGrid.Columns = width < 980 ? 1 : 2;
	}

	private void UpdateUI()
	{
		UpdateLayoutByWidth();
		SyncSelectionWithAliveRoster();

		_labelDay!.Text = $"Day {_state.Day}";
		_labelMoney!.Text = $"Money {_state.Money}";
		_labelSeed!.Text = $"Seed {_state.Seed}";

		_listGladiators!.Clear();
		if (_state.Gladiators.Count == 0)
		{
			_listGladiators.AddItem("No gladiators hired yet.", _portraitTexture);
		}
		else
		{
			foreach (var g in _state.Gladiators)
			{
				var stateText = !g.IsAlive
					? "OUT"
					: g.IsInjured
						? $"INJ:{g.CurrentInjury!.Value.RecoveryDaysLeft}d"
						: g.CurrentTraining.HasValue
							? $"TRN:{TrainingAbbrev(g.CurrentTraining.Value)}"
							: "FIT";
				var row = $"{g.Name} [{stateText}]  HP {g.Health}/{g.MaxHealth}  STR {g.Stats.Strength}  AGI {g.Stats.Agility}  STA {g.Stats.Stamina}  MOR:{g.Morale} FAT:{g.Fatigue}  W:{g.Contract.Terms.DailyWage}/d  CNT:{g.Contract.DaysRemaining}d  OVD:{g.Contract.OverdueDays}";
				_listGladiators.AddItem(row, _portraitTexture);
			}
		}

		UpdateTrainingPanel();
		RebuildFightSelectors();

		var firstText = NameById(_firstFighterId) ?? "not selected";
		var secondText = NameById(_secondFighterId) ?? "not selected";
		_selectionStatus!.Text = $"Selection: {firstText} vs {secondText}";

		var canFight = _aliveForSelection.Count >= 2 && _firstFighterId.HasValue && _secondFighterId.HasValue;
		_btnSimulateFight!.Disabled = !canFight;

		if (_aliveForSelection.Count < 2)
		{
			_fightStatus!.Text = "Need at least two alive gladiators.";
		}
	}

	private void SyncSelectionWithAliveRoster()
	{
		_aliveForSelection = _state.Gladiators.Where(g => g.CanFight).ToList();

		if (_selectedGladiatorId.HasValue && !_state.Gladiators.Any(g => g.Id == _selectedGladiatorId.Value))
		{
			_selectedGladiatorId = null;
		}

		if (_firstFighterId.HasValue && !_aliveForSelection.Any(g => g.Id == _firstFighterId.Value))
		{
			_firstFighterId = null;
		}

		if (_secondFighterId.HasValue && !_aliveForSelection.Any(g => g.Id == _secondFighterId.Value))
		{
			_secondFighterId = null;
		}

		if (_firstFighterId == _secondFighterId)
		{
			_secondFighterId = null;
		}
	}

	private void RebuildFightSelectors()
	{
		_firstFighterSelect!.Clear();
		_secondFighterSelect!.Clear();

		foreach (var g in _aliveForSelection)
		{
			var item = $"{g.Name} ({g.Health}/{g.MaxHealth} HP, MOR:{g.Morale} FAT:{g.Fatigue}, W:{g.Contract.Terms.DailyWage}/d, CNT:{g.Contract.DaysRemaining}d, OVD:{g.Contract.OverdueDays})";
			_firstFighterSelect.AddItem(item);
			_secondFighterSelect.AddItem(item);
		}

		SelectById(_firstFighterSelect, _firstFighterId);
		SelectById(_secondFighterSelect, _secondFighterId);
	}

	private void SelectById(OptionButton select, Guid? fighterId)
	{
		if (!fighterId.HasValue)
		{
			select.Select(-1);
			return;
		}

		for (var i = 0; i < _aliveForSelection.Count; i++)
		{
			if (_aliveForSelection[i].Id == fighterId.Value)
			{
				select.Select(i);
				return;
			}
		}

		select.Select(-1);
	}

	private string? NameById(Guid? fighterId)
	{
		if (!fighterId.HasValue)
		{
			return null;
		}

		var fighter = _aliveForSelection.FirstOrDefault(g => g.Id == fighterId.Value);
		return fighter.Id == Guid.Empty ? null : fighter.Name;
	}

	private void UpdateTrainingPanel()
	{
		_trainingSelect!.Clear();
		_trainingSelect.AddItem("None");
		_trainingSelect.AddItem("Strength");
		_trainingSelect.AddItem("Agility");
		_trainingSelect.AddItem("Stamina");

		if (!_selectedGladiatorId.HasValue)
		{
			_labelSelectedGladiator!.Text = "Select a gladiator";
			_trainingSelect.Select(0);
			_trainingStatus!.Text = "";
			_btnAssignTraining!.Disabled = true;
			return;
		}

		var gladiator = _state.Gladiators.FirstOrDefault(g => g.Id == _selectedGladiatorId.Value);
		if (gladiator.Id == Guid.Empty)
		{
			_selectedGladiatorId = null;
			_labelSelectedGladiator!.Text = "Select a gladiator";
			_trainingSelect.Select(0);
			_trainingStatus!.Text = "";
			_btnAssignTraining!.Disabled = true;
			return;
		}

		_labelSelectedGladiator!.Text = $"{gladiator.Name}  STR {gladiator.Stats.Strength}  AGI {gladiator.Stats.Agility}  STA {gladiator.Stats.Stamina}  MOR:{gladiator.Morale} FAT:{gladiator.Fatigue}  W:{gladiator.Contract.Terms.DailyWage}/d  CNT:{gladiator.Contract.DaysRemaining}d  OVD:{gladiator.Contract.OverdueDays}";

		if (gladiator.IsInjured)
		{
			_trainingSelect.Disabled = true;
			_trainingSelect.Select(0);
			_trainingStatus!.Text = $"Injured: {gladiator.CurrentInjury!.Value.Type} ({gladiator.CurrentInjury!.Value.RecoveryDaysLeft}d left)";
			_btnAssignTraining!.Disabled = true;
			return;
		}

		_trainingSelect.Disabled = false;

		if (gladiator.CurrentTraining.HasValue)
		{
			var idx = gladiator.CurrentTraining.Value switch
			{
				TrainingType.Strength => 1,
				TrainingType.Agility => 2,
				TrainingType.Stamina => 3,
				_ => 0
			};
			_trainingSelect.Select(idx);
			_trainingStatus!.Text = $"Training: {gladiator.CurrentTraining.Value}";
		}
		else
		{
			_trainingSelect.Select(0);
			_trainingStatus!.Text = "";
		}

		_btnAssignTraining!.Disabled = !gladiator.IsAlive || gladiator.IsInjured;
	}

	private static string TrainingAbbrev(TrainingType type) => type switch
	{
		TrainingType.Strength => "STR",
		TrainingType.Agility => "AGI",
		TrainingType.Stamina => "STA",
		_ => "???"
	};

	private static T? TryLoad<T>(string path) where T : Resource
	{
		var resource = ResourceLoader.Load(path);
		if (resource is T typed)
		{
			return typed;
		}

		GD.PushWarning($"Failed to load resource as {typeof(T).Name}: {path}");
		return null;
	}

	private static void TryPlay(AudioStreamPlayer? player)
	{
		if (player?.Stream is not null)
		{
			player.Play();
		}
	}
}
