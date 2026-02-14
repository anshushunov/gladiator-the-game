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

	private LudusState _state = LudusState.Empty;
	private Label? _labelDay;
	private Label? _labelMoney;
	private Label? _labelSeed;
	private ItemList? _listGladiators;
	private RichTextLabel? _fightLog;
	private Label? _fightStatus;
	private Button? _btnSimulateFight;
	private Label? _labelFightSelection;

	private AudioStreamPlayer? _sfxHire;
	private AudioStreamPlayer? _sfxAdvanceDay;
	private AudioStreamPlayer? _sfxFight;

	private Texture2D? _portraitTexture;

	private int? _firstFighterIndex;
	private int? _secondFighterIndex;

	public override void _Ready()
	{
		_labelDay = GetNode<Label>("Control/MarginContainer/RootVBox/StatsHBox/DayBlock/LabelDay");
		_labelMoney = GetNode<Label>("Control/MarginContainer/RootVBox/StatsHBox/MoneyBlock/LabelMoney");
		_labelSeed = GetNode<Label>("Control/MarginContainer/RootVBox/StatsHBox/SeedBlock/LabelSeed");
		_listGladiators = GetNode<ItemList>("Control/MarginContainer/RootVBox/MainGrid/RosterPanel/RosterMargin/RosterVBox/ListGladiators");
		_fightLog = GetNode<RichTextLabel>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/FightLog");
		_fightStatus = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/FightStatus");
		_btnSimulateFight = GetNode<Button>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/btnSimulateFight");
		_labelFightSelection = GetNode<Label>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/LabelFightSelection");

		var btnNewGame = GetNode<Button>("Control/MarginContainer/RootVBox/ActionsHBox/btnNewGame");
		var btnHireRandom = GetNode<Button>("Control/MarginContainer/RootVBox/ActionsHBox/btnHireRandom");
		var btnAdvanceDay = GetNode<Button>("Control/MarginContainer/RootVBox/ActionsHBox/btnAdvanceDay");
		var btnSelectFirst = GetNode<Button>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/FightSelectionHBox/btnSelectFirst");
		var btnSelectSecond = GetNode<Button>("Control/MarginContainer/RootVBox/MainGrid/FightPanel/FightMargin/FightVBox/FightSelectionHBox/btnSelectSecond");

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
		TryPlay(_sfxHire);
		UpdateUI();
	}

	public void OnAdvanceDayPressed()
	{
		_state = _state.AdvanceDay();
		TryPlay(_sfxAdvanceDay);
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
			if (_secondFighterIndex == _firstFighterIndex)
			{
				_firstFighterIndex = null;
			}
		}

		UpdateUI();
	}

	public void OnSimulateFightPressed()
	{
		if (!_firstFighterIndex.HasValue || !_secondFighterIndex.HasValue)
		{
			_fightStatus?.SetText("Select two different fighters.");
			return;
		}

		var alive = _state.AliveGladiators;
		if (_firstFighterIndex.Value >= alive.Count || _secondFighterIndex.Value >= alive.Count)
		{
			_fightStatus?.SetText("Selection is out of date. Re-select fighters.");
			return;
		}

		var first = alive[_firstFighterIndex.Value];
		var second = alive[_secondFighterIndex.Value];

		if (first.Id == second.Id)
		{
			_fightStatus?.SetText("Fighters must be different.");
			return;
		}

		var (updatedState, result) = _state.ResolveFight(first.Id, second.Id);
		_state = updatedState;
		_firstFighterIndex = null;
		_secondFighterIndex = null;
		TryPlay(_sfxFight);

		if (_fightLog is not null)
		{
			_fightLog.Text = result.Log.ToString();
			_fightLog.ScrollToLine(0);
		}

		_fightStatus?.SetText($"Winner: {result.Winner.Name} (HP: {result.Winner.Health}/{result.Winner.MaxHealth})");
		UpdateUI();
	}

	private void UpdateUI()
	{
		NormalizeSelection();

		_labelDay?.SetText($"Day: {_state.Day}");
		_labelMoney?.SetText($"Money: {_state.Money}");
		_labelSeed?.SetText($"Seed: {_state.Seed}");

		_listGladiators?.Clear();
		if (_listGladiators is not null)
		{
			if (_state.Gladiators.Count == 0)
			{
				_listGladiators.AddItem("No gladiators hired yet.", _portraitTexture);
			}
			else
			{
				foreach (var g in _state.Gladiators)
				{
					var stateText = g.IsAlive ? "FIT" : "INJURED";
					var row = $"{g.Name} [{stateText}] HP {g.Health}/{g.MaxHealth} | STR {g.Stats.Strength} AGI {g.Stats.Agility} STA {g.Stats.Stamina}";
					_listGladiators.AddItem(row, _portraitTexture);
				}
			}
		}

		var alive = _state.AliveGladiators;
		var firstText = _firstFighterIndex.HasValue ? $"First: {alive[_firstFighterIndex.Value].Name}" : "First: not selected";
		var secondText = _secondFighterIndex.HasValue ? $"Second: {alive[_secondFighterIndex.Value].Name}" : "Second: not selected";
		_labelFightSelection?.SetText($"Selection: [{firstText}] vs [{secondText}]");
		_btnSimulateFight?.SetDisabled(!(_firstFighterIndex.HasValue && _secondFighterIndex.HasValue));

		if (alive.Count < 2)
		{
			_fightStatus?.SetText("Need at least two alive gladiators.");
		}
	}

	private void NormalizeSelection()
	{
		var aliveCount = _state.AliveGladiators.Count;

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
