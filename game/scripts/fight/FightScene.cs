using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Ludus.Core;

namespace Ludus.Game;

/// <summary>
/// Контроллер replay-проигрывателя боя.
/// Итерирует FightLog.Events и анимирует каждое событие.
/// </summary>
public partial class FightScene : Node2D
{
	[Signal]
	public delegate void FightFinishedEventHandler();

	private const string ArenaBgPath = "res://assets/backgrounds/arena_bg_01.svg";
	private const string HitSfxPath = "res://assets/sfx/fight.wav";

	private static readonly float[] SpeedOptions = { 1f, 2f, 4f };
	private int _speedIndex;
	private float SpeedMultiplier => SpeedOptions[_speedIndex];

	private FightResult _result;
	private Gladiator _preFightLeft;
	private Gladiator _preFightRight;

	// Scene nodes
	private Node2D? _arenaFloor;
	private FighterVisual? _leftFighter;
	private FighterVisual? _rightFighter;
	private Label? _roundLabel;
	private RichTextLabel? _eventLabel;
	private Button? _speedButton;
	private Button? _skipButton;
	private Button? _continueButton;
	private Node2D? _popupContainer;

	private AudioStreamPlayer? _sfxHit;
	private AudioStreamPlayer? _sfxMiss;
	private AudioStreamPlayer? _sfxCrit;

	private bool _skipRequested;
	private bool _playbackDone;

	// Track current HP during replay
	private int _leftHp;
	private int _rightHp;

	public override void _Ready()
	{
		BuildScene();
	}

	public void Initialize(FightResult result, Gladiator preFightLeft, Gladiator preFightRight)
	{
		_result = result;
		_preFightLeft = preFightLeft;
		_preFightRight = preFightRight;

		_leftHp = preFightLeft.Health;
		_rightHp = preFightRight.Health;

		SetupFighters();
		_ = PlaybackLoop();
	}

	private void BuildScene()
	{
		// Background layer
		var bgLayer = new CanvasLayer { Layer = 0 };
		AddChild(bgLayer);

		var bgTexture = TryLoad<Texture2D>(ArenaBgPath);
		if (bgTexture is not null)
		{
			var bgArena = new TextureRect
			{
				Texture = bgTexture,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				AnchorRight = 1, AnchorBottom = 1,
				LayoutDirection = Control.LayoutDirectionEnum.Ltr
			};
			bgArena.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			bgLayer.AddChild(bgArena);
		}

		var bgOverlay = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.3f)
		};
		bgOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bgLayer.AddChild(bgOverlay);

		// Arena floor (shakes)
		_arenaFloor = new Node2D();
		AddChild(_arenaFloor);

		// UI layer
		var uiLayer = new CanvasLayer { Layer = 1 };
		AddChild(uiLayer);

		_roundLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Position = new Vector2(760, 30),
			CustomMinimumSize = new Vector2(400, 50),
			Modulate = Colors.White
		};
		_roundLabel.AddThemeFontSizeOverride("font_size", 36);
		_roundLabel.AddThemeColorOverride("font_color", Colors.White);
		_roundLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_roundLabel.AddThemeConstantOverride("outline_size", 5);
		uiLayer.AddChild(_roundLabel);

		_eventLabel = new RichTextLabel
		{
			Position = new Vector2(560, 950),
			Size = new Vector2(800, 100),
			BbcodeEnabled = true,
			ScrollFollowing = true
		};
		_eventLabel.AddThemeFontSizeOverride("normal_font_size", 18);
		uiLayer.AddChild(_eventLabel);

		// Control panel — centered below the arena
		var controlPanel = new HBoxContainer
		{
			Position = new Vector2(760, 1020),
			CustomMinimumSize = new Vector2(400, 44)
		};
		controlPanel.AddThemeConstantOverride("separation", 16);
		uiLayer.AddChild(controlPanel);

		_speedButton = new Button { Text = "Speed: 1x", CustomMinimumSize = new Vector2(120, 36) };
		_speedButton.Pressed += OnSpeedPressed;
		controlPanel.AddChild(_speedButton);

		_skipButton = new Button { Text = "Skip", CustomMinimumSize = new Vector2(100, 36) };
		_skipButton.Pressed += OnSkipPressed;
		controlPanel.AddChild(_skipButton);

		_continueButton = new Button
		{
			Text = "Continue",
			CustomMinimumSize = new Vector2(120, 36),
			Visible = false
		};
		_continueButton.Pressed += OnContinuePressed;
		controlPanel.AddChild(_continueButton);

		// Popup container
		_popupContainer = new Node2D();
		AddChild(_popupContainer);

		// SFX players
		_sfxHit = new AudioStreamPlayer { VolumeDb = -6f };
		_sfxHit.Stream = TryLoad<AudioStream>(HitSfxPath);
		AddChild(_sfxHit);

		_sfxMiss = new AudioStreamPlayer { VolumeDb = -10f };
		_sfxMiss.Stream = TryLoad<AudioStream>(HitSfxPath);
		AddChild(_sfxMiss);

		_sfxCrit = new AudioStreamPlayer { VolumeDb = -3f };
		_sfxCrit.Stream = TryLoad<AudioStream>(HitSfxPath);
		AddChild(_sfxCrit);
	}

	private void SetupFighters()
	{
		// Arena ellipse center is at (960, 860) — position fighters on the arena floor
		_leftFighter = new FighterVisual
		{
			Position = new Vector2(620, 720)
		};
		_arenaFloor!.AddChild(_leftFighter);
		_leftFighter.Initialize(_preFightLeft.Name, _preFightLeft.Health, _preFightLeft.MaxHealth, true);
		_leftFighter.PlayIdle();

		_rightFighter = new FighterVisual
		{
			Position = new Vector2(1300, 720)
		};
		_arenaFloor.AddChild(_rightFighter);
		_rightFighter.Initialize(_preFightRight.Name, _preFightRight.Health, _preFightRight.MaxHealth, false);
		_rightFighter.PlayIdle();
	}

	private async Task PlaybackLoop()
	{
		var events = _result.Log.Events;
		int currentRound = 0;

		for (int i = 0; i < events.Count; i++)
		{
			if (_skipRequested)
			{
				ApplyRemainingEvents(events, i);
				break;
			}

			var evt = events[i];

			// Round announcement
			if (evt.Round != currentRound)
			{
				currentRound = evt.Round;
				await PlayRoundAnnouncement(currentRound);
				if (_skipRequested) { ApplyRemainingEvents(events, i); break; }
			}

			await PlayEvent(evt);
		}

		await ShowFightEnd();
	}

	private async Task PlayRoundAnnouncement(int round)
	{
		if (_roundLabel is null) return;
		_roundLabel.Text = $"Round {round}";
		_roundLabel.Modulate = new Color(1, 1, 1, 0);

		var tween = CreateTween();
		tween.TweenProperty(_roundLabel, "modulate:a", 1f, 0.15f / SpeedMultiplier);
		tween.TweenInterval(0.2f / SpeedMultiplier);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private async Task PlayEvent(FightLog.Event evt)
	{
		bool isLeftAttacker = evt.AttackerName == _preFightLeft.Name;
		var attacker = isLeftAttacker ? _leftFighter : _rightFighter;
		var defender = isLeftAttacker ? _rightFighter : _leftFighter;

		switch (evt.Type)
		{
			case FightLog.EventType.Miss:
				AppendEventText($"[color=gray]{evt.AttackerName} misses {evt.DefenderName}[/color]");
				TryPlay(_sfxMiss);
				if (attacker is not null)
				{
					var atkTween = attacker.PlayAttack();
					await ToSignal(atkTween, Tween.SignalName.Finished);
				}
				if (defender is not null && _popupContainer is not null)
				{
					DamagePopup.CreateMiss(_popupContainer, defender.GetPopupPosition());
				}
				await WaitSeconds(0.2f / SpeedMultiplier);
				break;

			case FightLog.EventType.Hit:
				AppendEventText($"{evt.AttackerName} hits {evt.DefenderName}");
				TryPlay(_sfxHit);
				if (attacker is not null)
				{
					var atkTween = attacker.PlayAttack();
					await ToSignal(atkTween, Tween.SignalName.Finished);
				}
				if (defender is not null)
				{
					var hurtTween = defender.PlayHurt();
					await ToSignal(hurtTween, Tween.SignalName.Finished);
				}
				ScreenShake(3f);
				break;

			case FightLog.EventType.Crit:
				AppendEventText($"[color=red][b]CRITICAL HIT![/b][/color] {evt.AttackerName} → {evt.DefenderName}");
				TryPlay(_sfxCrit);
				ScreenShake(8f);
				await WaitSeconds(0.2f / SpeedMultiplier);
				break;

			case FightLog.EventType.DamageApplied:
				int damage = (int)evt.Value;
				bool isLeftDefender = evt.DefenderName == _preFightLeft.Name;
				bool wasCrit = IsPreviousEventCrit(evt);

				if (isLeftDefender)
				{
					_leftHp = System.Math.Max(0, _leftHp - damage);
					_leftFighter?.HealthBar?.AnimateTo(_leftHp, 0.3f / SpeedMultiplier);
				}
				else
				{
					_rightHp = System.Math.Max(0, _rightHp - damage);
					_rightFighter?.HealthBar?.AnimateTo(_rightHp, 0.3f / SpeedMultiplier);
				}

				AppendEventText($"{evt.DefenderName} takes [b]{damage}[/b] damage");

				if (defender is not null && _popupContainer is not null)
				{
					DamagePopup.Create(_popupContainer, defender.GetPopupPosition(), damage, wasCrit);
				}
				await WaitSeconds(0.4f / SpeedMultiplier);
				break;

			case FightLog.EventType.Kill:
				AppendEventText($"[color=red][b]{evt.DefenderName} is KILLED by {evt.AttackerName}![/b][/color]");
				ScreenShake(12f);
				if (defender is not null)
				{
					var deathTween = defender.PlayDeath();
					await ToSignal(deathTween, Tween.SignalName.Finished);
				}
				break;

			case FightLog.EventType.FightEnd:
				// Handled in ShowFightEnd
				break;
		}
	}

	private bool IsPreviousEventCrit(FightLog.Event currentEvt)
	{
		var events = _result.Log.Events;
		for (int i = 0; i < events.Count; i++)
		{
			if (events[i].Round == currentEvt.Round &&
				events[i].Type == FightLog.EventType.DamageApplied &&
				events[i].DefenderName == currentEvt.DefenderName)
			{
				// Check if there's a Crit event in this round for this attacker-defender pair
				for (int j = 0; j < i; j++)
				{
					if (events[j].Round == currentEvt.Round &&
						events[j].Type == FightLog.EventType.Crit &&
						events[j].AttackerName == currentEvt.AttackerName)
					{
						return true;
					}
				}
				break;
			}
		}
		return false;
	}

	private void ApplyRemainingEvents(IReadOnlyList<FightLog.Event> events, int fromIndex)
	{
		for (int i = fromIndex; i < events.Count; i++)
		{
			var evt = events[i];
			if (evt.Type == FightLog.EventType.DamageApplied)
			{
				int damage = (int)evt.Value;
				bool isLeftDefender = evt.DefenderName == _preFightLeft.Name;
				if (isLeftDefender)
				{
					_leftHp = System.Math.Max(0, _leftHp - damage);
				}
				else
				{
					_rightHp = System.Math.Max(0, _rightHp - damage);
				}
			}
		}

		_leftFighter?.HealthBar?.SnapTo(_leftHp);
		_rightFighter?.HealthBar?.SnapTo(_rightHp);

		// Show death of loser
		bool loserIsLeft = _result.Loser.Name == _preFightLeft.Name;
		var loserVisual = loserIsLeft ? _leftFighter : _rightFighter;
		loserVisual?.PlayDeath();
	}

	private async Task ShowFightEnd()
	{
		_playbackDone = true;
		_skipButton!.Visible = false;
		_speedButton!.Visible = false;
		_continueButton!.Visible = true;

		AppendEventText($"\n[color=gold][b]Winner: {_result.Winner.Name}![/b][/color]");

		if (_roundLabel is not null)
		{
			_roundLabel.Text = $"Winner: {_result.Winner.Name}!";
			_roundLabel.Modulate = new Color(1, 0.84f, 0, 1);
		}

		await WaitSeconds(0.3f);
	}

	private void ScreenShake(float intensity)
	{
		if (_arenaFloor is null) return;
		var originalPos = Vector2.Zero;
		var tween = CreateTween();
		tween.TweenProperty(_arenaFloor, "position",
			originalPos + new Vector2((float)GD.RandRange(-intensity, intensity), (float)GD.RandRange(-intensity, intensity)),
			0.05f);
		tween.TweenProperty(_arenaFloor, "position",
			originalPos + new Vector2((float)GD.RandRange(-intensity * 0.5f, intensity * 0.5f), (float)GD.RandRange(-intensity * 0.5f, intensity * 0.5f)),
			0.05f);
		tween.TweenProperty(_arenaFloor, "position", originalPos, 0.05f);
	}

	private void AppendEventText(string bbcode)
	{
		_eventLabel?.AppendText(bbcode + "\n");
	}

	private async Task WaitSeconds(float seconds)
	{
		if (_skipRequested) return;
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void OnSpeedPressed()
	{
		_speedIndex = (_speedIndex + 1) % SpeedOptions.Length;
		_speedButton!.Text = $"Speed: {SpeedOptions[_speedIndex]}x";
	}

	private void OnSkipPressed()
	{
		_skipRequested = true;
	}

	private void OnContinuePressed()
	{
		EmitSignal(SignalName.FightFinished);
	}

	private static T? TryLoad<T>(string path) where T : Resource
	{
		var resource = ResourceLoader.Load(path);
		if (resource is T typed) return typed;
		GD.PushWarning($"Failed to load resource as {typeof(T).Name}: {path}");
		return null;
	}

	private static void TryPlay(AudioStreamPlayer? player)
	{
		if (player?.Stream is not null) player.Play();
	}
}
