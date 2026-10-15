using Godot;
using Marvel.Rules.Play;

namespace Marvel.Godot;

/// <summary>A nonmodal result card that preserves access to the final table.</summary>
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
        OutcomePresentation outcome = OutcomePresentation.From(main.CurrentGame!.World!.Outcome,
            main.CurrentGame.World.LossReason);
        var frame = new PanelContainer
        {
            Name = "TerminalOutcome", ZIndex = 20,
            CustomMinimumSize = new Vector2(520, 0),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        main.AddChild(frame);
        frame.SetMeta("outcome", (int)terminal);
        var stack = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.Stack,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
        };
        frame.AddChild(stack);
        stack.AddChild(Copy("OutcomeHeading", outcome.Heading, 28));
        stack.AddChild(Copy("OutcomeResult", outcome.Result, 18));
        if (outcome.Detail.Length > 0) stack.AddChild(Copy("OutcomeCause", outcome.Detail, 15));
        var actions = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.WideRow };
        stack.AddChild(actions);
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
        float width = Math.Min(520, main.GetViewportRect().Size.X - 48);
        frame.Size = new Vector2(width, frame.GetCombinedMinimumSize().Y);
        frame.Position = new Vector2((main.GetViewportRect().Size.X - width) / 2, 86);
    }

    private static Label Copy(string name, string text, int size)
    {
        var label = new Label
        {
            Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(480, 0),
            ThemeTypeVariation = GodotThemeVariations.Body,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }
}
