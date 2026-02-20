using Godot;

namespace Ludus.Game;

/// <summary>
/// Всплывающее число урона (или MISS).
/// </summary>
public partial class DamagePopup : Label
{
	private const float FloatDistance = 60f;
	private const float Duration = 0.8f;

	public static DamagePopup Create(Node parent, Vector2 position, int damage, bool isCrit)
	{
		var popup = new DamagePopup
		{
			Text = isCrit ? $"CRIT! {damage}" : damage.ToString(),
			Position = position,
			HorizontalAlignment = HorizontalAlignment.Center,
			ZIndex = 100
		};

		popup.AddThemeColorOverride("font_color", isCrit ? new Color(1f, 0.2f, 0.1f) : Colors.White);
		popup.AddThemeFontSizeOverride("font_size", isCrit ? 28 : 20);
		popup.AddThemeColorOverride("font_outline_color", Colors.Black);
		popup.AddThemeConstantOverride("outline_size", 3);

		parent.AddChild(popup);
		popup.Animate();
		return popup;
	}

	public static DamagePopup CreateMiss(Node parent, Vector2 position)
	{
		var popup = new DamagePopup
		{
			Text = "MISS",
			Position = position,
			HorizontalAlignment = HorizontalAlignment.Center,
			ZIndex = 100
		};

		popup.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		popup.AddThemeFontSizeOverride("font_size", 18);
		popup.AddThemeColorOverride("font_outline_color", Colors.Black);
		popup.AddThemeConstantOverride("outline_size", 2);

		parent.AddChild(popup);
		popup.Animate();
		return popup;
	}

	private void Animate()
	{
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "position:y", Position.Y - FloatDistance, Duration);
		tween.TweenProperty(this, "modulate:a", 0f, Duration).SetDelay(Duration * 0.4f);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
