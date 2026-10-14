using Godot;

namespace Marvel.Godot;

/// <summary>Animates a control's surface edge while its text and hit geometry stay fixed.</summary>
internal sealed class ButtonSurfaceFeedback
{
    private readonly Button button;
    private readonly Control owner;
    private readonly bool ownsCursor;
    private bool hovered;
    private bool pressed;
    private float strength;
    private Tween? tween;

    internal ButtonSurfaceFeedback(Button button, Control owner)
    {
        this.button = button;
        this.owner = owner;
        ownsCursor = button.MouseDefaultCursorShape is Control.CursorShape.Arrow or Control.CursorShape.PointingHand;
        button.MouseEntered += () => { hovered = true; Refresh(); };
        button.MouseExited += () => { hovered = false; Refresh(); };
        button.FocusEntered += Refresh;
        button.FocusExited += Refresh;
        button.ButtonDown += () => { pressed = true; Refresh(0.06); };
        button.ButtonUp += () => { pressed = false; Refresh(0.14); };
        button.Ready += Refresh;
        button.TreeExiting += Stop;
        button.Draw += Draw;
        Refresh();
    }

    private void Refresh() => Refresh(0.1);

    private void Refresh(double duration)
    {
        TooltipPolicy.RemoveRestatement(button);
        UpdateCursor();
        float target = TargetStrength();
        Stop();
        if (!MotionEnabled() || !button.IsInsideTree()) { SetStrength(target); return; }
        tween = button.CreateTween();
        tween.TweenMethod(Callable.From<float>(SetStrength), strength, target, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    private float TargetStrength()
    {
        if (button.Disabled || (!hovered && !button.HasFocus())) return 0;
        return pressed ? 0.8f : 0.65f;
    }

    private bool MotionEnabled() => owner.FindChild("Motion", true, false) is not BaseButton toggle || toggle.ButtonPressed;

    private void UpdateCursor()
    {
        if (ownsCursor) button.MouseDefaultCursorShape = button.Disabled
            ? Control.CursorShape.Arrow : Control.CursorShape.PointingHand;
    }

    private void SetStrength(float value)
    {
        strength = value;
        button.QueueRedraw();
    }

    private void Stop() => tween?.Kill();

    private void Draw()
    {
        UpdateCursor();
        if (strength <= 0 || button.Disabled || button.HasFocus()) return;
        Color color = ClientTheme.ToGodot(VisualSystem.Palette.Text) with { A = strength * 0.22f };
        button.DrawLine(new Vector2(6, 1), new Vector2(Math.Max(6, button.Size.X - 6), 1), color, 1, true);
    }
}
