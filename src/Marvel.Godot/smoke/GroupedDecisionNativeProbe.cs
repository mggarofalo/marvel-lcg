using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Synthetic renderer contract; no Core card or played game is represented.</summary>
public sealed partial class GroupedDecisionNativeProbe : Control
{
    private readonly DecisionPanel panel = new() { Name = "SyntheticDecision", Position = new(40, 40), Size = new(1500, 900) };
    private Decision? submitted;
    private int submissions;

    public override void _Ready()
    {
        AddChild(panel);
        panel.Submitted += decision => { submitted = decision; submissions++; };
        var offer = new Affordance(7, "Synthetic grouped selection", 10, 0, "Choose one ordered group",
            new TargetRequest([11, 12, 13], 3, 3, Groups: [[12, 11], [11, 13]]));
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Synthetic component", "Choose an ordered group; this is not a Core game.", false, [offer]);
        panel.Render(prompt, new WorldDescriptor([], [], [], Outcome.Unfinished), 0);
    }

    public string Selection() => string.Join(',', panel.composer?.Targets ?? []);
    public string SubmittedTargets() => string.Join(',', submitted?.Targets ?? []);
    public int Submissions() => submissions;
    public void SetReadingScale(int percent) => panel.SetInterfaceScale((InterfaceScale)percent);
}
