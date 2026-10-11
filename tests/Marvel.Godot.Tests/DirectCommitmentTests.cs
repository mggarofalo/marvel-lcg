using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic prompt contracts preserve unanswered targets and opportunity boundaries.</summary>
public sealed class DirectCommitmentTests
{
    [Theory]
    [InlineData(PublicDecisionKind.MinionActivationOrder)]
    [InlineData(PublicDecisionKind.Response)]
    public void NamedReadyOpportunityCanCommitOnlyAfterItsExactActionWasChosen(PublicDecisionKind kind)
    {
        var composer = Composer(kind, new Affordance(1, "Activate", 10, 0, "Activate minion"));
        Assert.False(DirectCommitment.IsReady(composer));
        composer.SelectAffordance(1);
        Assert.True(DirectCommitment.IsReady(composer));
    }

    [Fact]
    public void TargetedResponseStillRequiresItsTargetAndOrdinaryAttackKeepsConfirmation()
    {
        var response = Composer(PublicDecisionKind.Response, new Affordance(1, "Response", 10, 0, "Response",
            new TargetRequest([20, 21], 1, 1)));
        response.SelectAffordance(1);
        Assert.False(DirectCommitment.IsReady(response));
        var attack = Composer(PublicDecisionKind.PlayerAction, new Affordance(1, "Attack", 10, 0, "Attack"));
        attack.SelectAffordance(1);
        Assert.False(DirectCommitment.IsReady(attack));
    }

    [Fact]
    public void PayableReadyResponseStillRequiresPaymentCommitment()
    {
        var composer = Composer(PublicDecisionKind.Response, new Affordance(1, "Response", 10, 0,
            "Response", Costs: [new CostOption(10, "1", Sources: [new(41, "Y")])]));
        composer.SelectAffordance(1);
        composer.ToggleResource(41);
        Assert.True(composer.TryBuild(out _, out _));
        Assert.False(DirectCommitment.IsReady(composer));
    }

    private static DecisionComposer Composer(PublicDecisionKind kind, Affordance offer) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", true, [offer]) { PublicKind = kind });
}
