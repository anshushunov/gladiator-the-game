using Godot;

namespace Ludus.Game;

/// <summary>
/// Анимированная полоска здоровья гладиатора.
/// </summary>
public partial class HealthBarUI : Control
{
	private const float BarWidth = 160f;
	private const float BarHeight = 16f;

	private ColorRect? _bgRect;
	private ColorRect? _fillRect;
	private Label? _hpLabel;
	private int _maxHealth;
	private int _currentHealth;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(BarWidth + 4, BarHeight + 20);

		_bgRect = new ColorRect
		{
			Color = new Color(0.15f, 0.12f, 0.1f),
			Position = new Vector2(2, 2),
			Size = new Vector2(BarWidth, BarHeight)
		};
		AddChild(_bgRect);

		_fillRect = new ColorRect
		{
			Color = new Color(0.2f, 0.8f, 0.2f),
			Position = new Vector2(2, 2),
			Size = new Vector2(BarWidth, BarHeight)
		};
		AddChild(_fillRect);

		_hpLabel = new Label
		{
			Position = new Vector2(0, BarHeight + 4),
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(BarWidth + 4, 0)
		};
		_hpLabel.AddThemeColorOverride("font_color", Colors.White);
		_hpLabel.AddThemeFontSizeOverride("font_size", 14);
		AddChild(_hpLabel);
	}

	public void Initialize(int health, int maxHealth)
	{
		_maxHealth = maxHealth;
		_currentHealth = health;
		SnapTo(health);
	}

	public void SnapTo(int newHealth)
	{
		_currentHealth = newHealth;
		if (_fillRect is null || _hpLabel is null) return;

		float ratio = _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;
		_fillRect.Size = new Vector2(BarWidth * ratio, BarHeight);
		_fillRect.Color = GetBarColor(ratio);
		_hpLabel.Text = $"{_currentHealth}/{_maxHealth}";
	}

	public Tween AnimateTo(int newHealth, float duration = 0.4f)
	{
		int oldHealth = _currentHealth;
		_currentHealth = newHealth;

		var tween = CreateTween();
		tween.TweenMethod(
			Callable.From<int>(hp =>
			{
				if (_fillRect is null || _hpLabel is null) return;
				float ratio = _maxHealth > 0 ? (float)hp / _maxHealth : 0f;
				_fillRect.Size = new Vector2(BarWidth * ratio, BarHeight);
				_fillRect.Color = GetBarColor(ratio);
				_hpLabel.Text = $"{hp}/{_maxHealth}";
			}),
			oldHealth,
			newHealth,
			duration
		);
		return tween;
	}

	private static Color GetBarColor(float ratio) => ratio switch
	{
		> 0.5f => new Color(0.2f, 0.8f, 0.2f),
		> 0.25f => new Color(0.9f, 0.8f, 0.1f),
		_ => new Color(0.9f, 0.2f, 0.1f)
	};
}
