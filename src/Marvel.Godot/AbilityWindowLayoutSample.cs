using Godot;

namespace Marvel.Godot;

/// <summary>Native layout specimen for externally supplied cause text and fixed table anchors; no game is played.</summary>
public sealed partial class AbilityWindowLayoutSample : Node
{
    private AbilityModalFrame? frame;

    public void Present(Main owner, string title, string cause, string choice, int currentChoices)
    {
        if (frame is null)
        {
            AddAnchor(owner, "HandDropArea", new Rect2(0, 788, 1920, 200));
            AddAnchor(owner, "EventTimeline", new Rect2(1165, 417, 693, 49));
            TreeExiting += Clear;
        }
        frame?.Dispose();
        SetMeta("choice_committed", false);
        frame = new AbilityModalFrame(owner, "AbilityWindow", owner.decisions.Theme, currentChoices);
        frame.AddText("AbilityWindowHeading", title, GodotThemeVariations.Heading);
        frame.AddText("AbilityWindowAnswerer", "Player 1", GodotThemeVariations.Caption);
        frame.AddText("AbilityWindowCause", cause, GodotThemeVariations.Body);
        frame.AddButton("WindowChoiceSample", choice, () => SetMeta("choice_committed", true));
        if (currentChoices == 2)
            frame.AddButton("WindowChoiceSecond", "Spider-Man takes no damage", () => SetMeta("choice_committed", true));
        frame.AddButton("WindowPass", "Pass this opportunity", () => SetMeta("pass_committed", true));
    }

    private static void AddAnchor(Main owner, string name, Rect2 bounds)
    {
        var anchor = new Control { Name = name, Position = bounds.Position, Size = bounds.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        owner.AddChild(anchor);
    }

    private void Clear() => frame?.Dispose();
}
