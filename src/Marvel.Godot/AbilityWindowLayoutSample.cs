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
        frame.AddHead(AbilityWindowHead.Compose(title, cause,
            [new Wording(TableWording.For(TableFact.AnsweringSeat).Icon, "Player 1", "Decision for Player 1"),
                TableWording.For(TableFact.DecisionOptional), TableWording.For(TableFact.DecisionCause) with { Label = "Stampede" }],
            "", owner.decisions.ReadingScale));
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
