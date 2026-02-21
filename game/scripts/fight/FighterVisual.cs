using System;
using Godot;

namespace Ludus.Game;

/// <summary>
/// Визуальное представление одного гладиатора на арене.
/// Sprite2D + ручное управление кадрами через Tween.
/// Idle = статичный первый кадр. Анимации проигрываются только при событиях боя.
/// </summary>
public partial class FighterVisual : Node2D
{
	private const string IdlePath = "res://assets/fight/sprites/gladiator1_idle.png";
	private const string AttackPath = "res://assets/fight/sprites/gladitor1_attack.png";
	private const string HitPath = "res://assets/fight/sprites/gladiator1_hit.png";
	private const string DeathPath = "res://assets/fight/sprites/gladiator1_death.png";

	private const int FrameCount = 6;

	private Sprite2D? _sprite;
	private HealthBarUI? _healthBar;
	private Label? _nameLabel;
	private Vector2 _basePosition;
	private bool _facingRight;

	private ImageTexture[]? _idleFrames;
	private ImageTexture[]? _attackFrames;
	private ImageTexture[]? _hurtFrames;
	private ImageTexture[]? _deathFrames;

	public HealthBarUI? HealthBar => _healthBar;

	public void Initialize(string name, int health, int maxHealth, bool facingRight)
	{
		_facingRight = facingRight;

		_idleFrames = LoadFrames(IdlePath);
		_attackFrames = LoadFrames(AttackPath);
		_hurtFrames = LoadFrames(HitPath);
		_deathFrames = LoadFrames(DeathPath);

		_sprite = new Sprite2D
		{
			FlipH = !facingRight, // sprites face right by default; flip for left-facing fighter
			Scale = new Vector2(0.45f, 0.45f)
		};
		ShowFrame(_idleFrames, 0);
		AddChild(_sprite);

		_healthBar = new HealthBarUI
		{
			Position = new Vector2(-80, -230)
		};
		AddChild(_healthBar);
		_healthBar.CallDeferred(nameof(HealthBarUI.Initialize), health, maxHealth);

		_nameLabel = new Label
		{
			Text = name,
			Position = new Vector2(-80, -255),
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(160, 0)
		};
		_nameLabel.AddThemeColorOverride("font_color", Colors.White);
		_nameLabel.AddThemeFontSizeOverride("font_size", 18);
		_nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_nameLabel.AddThemeConstantOverride("outline_size", 4);
		AddChild(_nameLabel);

		_basePosition = Position;
	}

	private void ShowFrame(ImageTexture[]? frames, int index)
	{
		if (_sprite is null || frames is null || frames.Length == 0) return;
		_sprite.Texture = frames[Math.Clamp(index, 0, frames.Length - 1)];
	}

	public void PlayIdle()
	{
		ShowFrame(_idleFrames, 0);
	}

	public Tween PlayAttack()
	{
		float direction = _facingRight ? 1f : -1f;
		var tween = CreateTween();

		// Wind-up: frame 0
		tween.TweenCallback(Callable.From(() => ShowFrame(_attackFrames, 0)));
		tween.TweenProperty(this, "position:x", _basePosition.X + 30f * direction, 0.12f);

		// Lunge: frame 2
		tween.TweenCallback(Callable.From(() => ShowFrame(_attackFrames, 2)));
		tween.TweenProperty(this, "position:x", _basePosition.X + 80f * direction, 0.12f)
			.SetTrans(Tween.TransitionType.Back);

		// Impact: frame 4 + hold
		tween.TweenCallback(Callable.From(() => ShowFrame(_attackFrames, 4)));
		tween.TweenInterval(0.15f);

		// Follow-through: frame 5 + return
		tween.TweenCallback(Callable.From(() => ShowFrame(_attackFrames, 5)));
		tween.TweenProperty(this, "position:x", _basePosition.X, 0.15f)
			.SetTrans(Tween.TransitionType.Quad);

		// Back to idle
		tween.TweenCallback(Callable.From(() => ShowFrame(_idleFrames, 0)));

		return tween;
	}

	public Tween PlayHurt()
	{
		float direction = _facingRight ? -1f : 1f;

		if (_sprite is not null)
		{
			_sprite.Modulate = new Color(1f, 0.3f, 0.3f);
		}

		var tween = CreateTween();

		// Impact: frame 0 + knockback
		tween.TweenCallback(Callable.From(() => ShowFrame(_hurtFrames, 0)));
		tween.TweenProperty(this, "position:x", _basePosition.X + 30f * direction, 0.1f);

		// Stagger: frame 2 + hold
		tween.TweenCallback(Callable.From(() => ShowFrame(_hurtFrames, 2)));
		tween.TweenInterval(0.15f);

		// Recoil: frame 4 + hold
		tween.TweenCallback(Callable.From(() => ShowFrame(_hurtFrames, 4)));
		tween.TweenInterval(0.15f);

		// Recovery: frame 5 + return
		tween.TweenCallback(Callable.From(() => ShowFrame(_hurtFrames, 5)));
		tween.TweenProperty(this, "position:x", _basePosition.X, 0.15f);

		// Remove red tint
		if (_sprite is not null)
		{
			tween.TweenProperty(_sprite, "modulate", Colors.White, 0.15f);
		}

		// Back to idle
		tween.TweenCallback(Callable.From(() => ShowFrame(_idleFrames, 0)));

		return tween;
	}

	public Tween PlayDeath()
	{
		int maxFrame = (_deathFrames?.Length ?? 1) - 1;

		var tween = CreateTween();
		tween.TweenMethod(Callable.From<int>(f => ShowFrame(_deathFrames, f)),
			0, maxFrame, 1.2f);
		tween.TweenProperty(this, "modulate:a", 0.4f, 0.3f);

		return tween;
	}

	public Vector2 GetPopupPosition()
	{
		return GlobalPosition + new Vector2(0, -150);
	}

	private static ImageTexture[]? LoadFrames(string path)
	{
		var image = LoadImage(path);
		if (image is null) return null;

		int fw = image.GetWidth() / FrameCount;
		int fh = image.GetHeight();

		var frames = new ImageTexture[FrameCount];
		for (int i = 0; i < FrameCount; i++)
		{
			var frameImage = image.GetRegion(new Rect2I(i * fw, 0, fw, fh));
			frames[i] = ImageTexture.CreateFromImage(frameImage);
		}
		return frames;
	}

	private static Image? LoadImage(string resPath)
	{
		var resource = ResourceLoader.Load<Texture2D>(resPath);
		if (resource is not null)
		{
			return resource.GetImage();
		}

		var globalPath = ProjectSettings.GlobalizePath(resPath);
		var image = new Image();
		var error = image.Load(globalPath);
		if (error != Error.Ok)
		{
			GD.PushWarning($"FighterVisual: failed to load {resPath} (error: {error})");
			return null;
		}
		return image;
	}
}
