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
    [InlineData(PublicDecisionKind.Choice)]
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

    [Fact]
    public void ZeroResourceEventCommitsOnItsCurrentWindowChoice()
    {
        var composer = Composer(PublicDecisionKind.Interrupt, new Affordance(1, "Replace", 10, 0,
            "Take damage instead of threat", Costs: [new CostOption(10, "0", Sources: [new(41, "Y")])])
            { PlaysCard = true });
        Assert.False(DirectCommitment.IsReady(composer));
        composer.SelectAffordance(1);
        Assert.True(composer.TryBuild(out _, out _));
        Assert.True(DirectCommitment.IsReady(composer));
        Assert.Empty(composer.Resources);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FreeEventDoesNotChooseCostAlternatives(bool separateOptions)
    {
        CostOption[] costs = separateOptions ? [new(10, "0"), new(10, "1")]
            : [new(10, "0", OrCost: "1")];
        var composer = Composer(PublicDecisionKind.Interrupt,
            new Affordance(1, "Replace", 10, 0, "Replace", Costs: costs) { PlaysCard = true });
        composer.SelectAffordance(1);
        Assert.False(DirectCommitment.IsReady(composer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FreeEventDoesNotChooseAmongTargets(int minimum)
    {
        var composer = Composer(PublicDecisionKind.Interrupt,
            new Affordance(1, "Replace", 10, 0, "Replace", new TargetRequest([20, 21], minimum, 1),
                Costs: [new CostOption(10, "0")]) { PlaysCard = true });
        composer.SelectAffordance(1);
        Assert.False(DirectCommitment.IsReady(composer));
    }

    [Fact]
    public void FreeEventDoesNotChooseAnObservedVariableOrPaymentDeclaration()
    {
        foreach (CostOption cost in new CostOption[]
            { new(10, "X", Variables: [new("X", 0, 3)]), new(10, "0", DeclarationSensitive: true) })
        {
            var composer = Composer(PublicDecisionKind.Interrupt,
                new Affordance(1, "Replace", 10, 0, "Replace", Costs: [cost]) { PlaysCard = true });
            composer.SelectAffordance(1);
            Assert.False(DirectCommitment.IsReady(composer));
        }
    }

    private static DecisionComposer Composer(PublicDecisionKind kind, Affordance offer) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", true, [offer]) { PublicKind = kind });
}
