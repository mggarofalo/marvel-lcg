using Godot;
using Marvel.Rules.Play;

namespace Marvel.Godot;

/// <summary>A nonmodal action that leads from the final table to its history.</summary>
/// <remarks>
/// The table's own context states the result once, as its heading and reason chip; this surface
/// carries only the next step, so the result is never stated twice.
/// </remarks>
internal static class TerminalOutcomeSurface
{
    internal static void Show(Main main)
    {
        Outcome terminal = main.CurrentGame!.World!.Outcome;
        if (main.GetNodeOrNull<Control>("TerminalOutcome") is { } prior)
        {
            if (prior.GetMeta("outcome", -1).AsInt32() == (int)terminal) return;
            main.RemoveChild(prior);
            prior.QueueFree();
        }
        var frame = new PanelContainer
        {
            Name = "TerminalOutcome", ZIndex = 20,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        main.AddChild(frame);
        frame.SetMeta("outcome", (int)terminal);
        var actions = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.WideRow };
        frame.AddChild(actions);
        var review = new Button
        {
            Name = "ReviewGame", Text = "Review game", CustomMinimumSize = new Vector2(0, 44),
            ThemeTypeVariation = GodotThemeVariations.PrimaryButton,
        };
        actions.AddChild(review);
        review.Pressed += () =>
        {
            frame.Hide();
            TableHistoryDrawer.Open(main);
            TableHistoryDrawer.SelectHistory(main);
        };
        // This surface continues to the retained final table through Review game.
        Fit(main, frame);
        Callable.From(() => Fit(main, frame)).CallDeferred();
        Action resized = () => { if (GodotObject.IsInstanceValid(frame)) Fit(main, frame); };
        main.Resized += resized;
        frame.TreeExiting += () => main.Resized -= resized;
    }

    internal static void ResetForLiveGame(Main main)
    {
        if (main.CurrentGame?.World?.Outcome != Outcome.Unfinished
            || main.GetNodeOrNull<Control>("TerminalOutcome") is not { } frame) return;
        main.RemoveChild(frame);
        frame.QueueFree();
    }

    private static void Fit(Main main, Control frame)
    {
        Vector2 size = frame.GetCombinedMinimumSize();
        frame.Size = size;
        frame.Position = new Vector2((main.GetViewportRect().Size.X - size.X) / 2, 86);
    }
}
