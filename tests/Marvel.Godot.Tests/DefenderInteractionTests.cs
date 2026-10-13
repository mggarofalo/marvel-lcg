using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic defense contracts distinguish required defense from an optional opportunity.</summary>
public sealed class DefenderInteractionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnlyEngineOfferedDefendersGetShieldsAndMandatoryComesFromPrompt(bool optional)
    {
        var source = new Prompt(0, Question.Defender, TimingPriority.Untimed, "test", "Choose defender", optional,
            [new Affordance(1, "Defend", 10, 0, "Defend")]);
        var visible = new AffordancePresentation(1, "Defend", null, "Defend", "Hero", 10, 0, null, "", [])
        {
            Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, 10, null, 0),
        };
        var prompt = new PromptPresentation("", "", "", "", "", [visible]);
        var control = Assert.Single(BoardInteractionControlProjection.From(new DecisionComposer(source), prompt));
        Assert.Equal(10, control.CardId);
        Assert.True(control.IsDefender);
        Assert.Equal(!optional, control.RequiredDefense);
        Assert.False(control.DirectActivation);
    }
}
