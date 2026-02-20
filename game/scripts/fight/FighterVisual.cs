using Godot;

namespace Ludus.Game;

/// <summary>
/// Визуальное представление одного гладиатора на арене.
/// Содержит спрайт, health bar, имя и анимации.
/// </summary>
public partial class FighterVisual : Node2D
{
	private Sprite2D? _sprite;
	private AnimationPlayer? _animPlayer;
	private HealthBarUI? _healthBar;
	private Label? _nameLabel;
	private Vector2 _basePosition;
	private bool _facingRight;

	public HealthBarUI? HealthBar => _healthBar;

	public void Initialize(string name, int health, int maxHealth, bool facingRight, Texture2D? texture)
	{
		_facingRight = facingRight;

		_sprite = new Sprite2D();
		if (texture is not null)
		{
			_sprite.Texture = texture;
		}
		_sprite.FlipH = !facingRight;
		_sprite.Scale = new Vector2(1.5f, 1.5f);
		AddChild(_sprite);

		_animPlayer = new AnimationPlayer();
		AddChild(_animPlayer);
		SetupAnimations();

		_healthBar = new HealthBarUI
		{
			Position = new Vector2(-62, -140)
		};
		AddChild(_healthBar);
		// HealthBarUI creates its children in _Ready, so we initialize after it's in the tree
		_healthBar.CallDeferred(nameof(HealthBarUI.Initialize), health, maxHealth);

		_nameLabel = new Label
		{
			Text = name,
			Position = new Vector2(-62, -160),
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(124, 0)
		};
		_nameLabel.AddThemeColorOverride("font_color", Colors.White);
		_nameLabel.AddThemeFontSizeOverride("font_size", 14);
		_nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_nameLabel.AddThemeConstantOverride("outline_size", 3);
		AddChild(_nameLabel);

		_basePosition = Position;
	}

	private void SetupAnimations()
	{
		if (_animPlayer is null) return;

		var animLib = new AnimationLibrary();

		// Idle: gentle bob
		var idle = new Animation();
		idle.Length = 1.2f;
		idle.LoopMode = Animation.LoopModeEnum.Linear;
		var idleTrack = idle.AddTrack(Animation.TrackType.Value);
		idle.TrackSetPath(idleTrack, ".:position:y");
		idle.TrackInsertKey(idleTrack, 0f, Position.Y);
		idle.TrackInsertKey(idleTrack, 0.6f, Position.Y - 4f);
		idle.TrackInsertKey(idleTrack, 1.2f, Position.Y);
		idle.TrackSetInterpolationType(idleTrack, Animation.InterpolationType.Cubic);
		animLib.AddAnimation("idle", idle);

		_animPlayer.AddAnimationLibrary("", animLib);
	}

	public void PlayIdle()
	{
		_animPlayer?.Play("idle");
	}

	public void StopIdle()
	{
		_animPlayer?.Stop();
	}

	public Tween PlayAttack()
	{
		StopIdle();
		float direction = _facingRight ? 1f : -1f;
		var tween = CreateTween();
		tween.TweenProperty(this, "position:x", _basePosition.X + 50f * direction, 0.15f)
			.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(this, "position:x", _basePosition.X, 0.1f)
			.SetTrans(Tween.TransitionType.Quad);
		tween.TweenCallback(Callable.From(PlayIdle));
		return tween;
	}

	public Tween PlayHurt()
	{
		StopIdle();
		float direction = _facingRight ? -1f : 1f;

		// Flash red
		if (_sprite is not null)
		{
			_sprite.Modulate = new Color(1f, 0.3f, 0.3f);
		}

		var tween = CreateTween();
		tween.TweenProperty(this, "position:x", _basePosition.X + 15f * direction, 0.08f);
		tween.TweenProperty(this, "position:x", _basePosition.X, 0.12f);
		if (_sprite is not null)
		{
			tween.TweenProperty(_sprite, "modulate", Colors.White, 0.15f);
		}
		tween.TweenCallback(Callable.From(PlayIdle));
		return tween;
	}

	public Tween PlayDeath()
	{
		StopIdle();
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "position:y", Position.Y + 40f, 0.8f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tween.TweenProperty(this, "modulate:a", 0f, 0.8f);
		return tween;
	}

	public Vector2 GetPopupPosition()
	{
		return GlobalPosition + new Vector2(0, -100);
	}
}
