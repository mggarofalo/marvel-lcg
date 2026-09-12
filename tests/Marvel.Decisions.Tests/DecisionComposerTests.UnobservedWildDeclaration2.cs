using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Decisions.Tests;
public sealed class DecisionComposerUnobservedWildDeclarationTests : DecisionComposerTestBase
{
    [Fact]
    public void UnobservedWildDeclarationIsSentWithoutAskingThePlayer()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Ordinary card cost", Costs: [new CostOption(20, "1", Sources: [new ResourceSource(41, "G")]), ])));
        composer.SelectAffordance(7);
        composer.ToggleResource(41);
        Assert.True(composer.UsesAutomaticResourceAllocation);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([new ResourceAllocation(41, 0, "G")], decision!.Allocations);
    }

    [Fact]
    public void ACostRequirementForcesTheAutomaticWildDeclaration()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Required resource", Costs: [new CostOption(20, "1", Rule: ["R"], Sources: [new ResourceSource(41, "G")]), ])));
        composer.SelectAffordance(7);
        composer.ToggleResource(41);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([new ResourceAllocation(41, 0, "R")], decision!.Allocations);
    }

    [Fact]
    public void TargetSpecificCostsMustMatchTheSelectedTarget()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Variable price", new TargetRequest([11, 12], 1, 1), [new CostOption(11, "0"), new CostOption(12, "0")])));
        composer.SelectAffordance(7);
        composer.SelectTargets([12]);
        composer.SelectCost(0);
        Assert.False(composer.TryBuild(out _, out string? mismatch));
        Assert.Contains("associated", mismatch);
        composer.SelectCost(1);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal(7, decision!.Affordance);
    }

    [Fact]
    public void AnAnchorScopedPriceAppliesWithoutMakingTheAnchorATarget()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(8, "Play", 20, 0, "Play support", Costs: [new CostOption(20, "0")])));
        composer.SelectAffordance(8);
        Assert.True(composer.CostApplies(composer.Selected!.CostOptions[0]));
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Empty(decision!.Targets);
    }

    [Fact]
    public void ACommittedCoreCardPlayUsesItsAnchorScopedPrice()
    {
        OpenedGame opened = DatasetGameFactory.Load(RepositoryPaths.Root).Create(new GameSpecification("rhino", ["spider_man"], null, 7));
        Game game = opened.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        Affordance? cardPlay = null;
        for (int step = 0; step < 20 && cardPlay is null; step++)
        {
            cardPlay = game.Pending!.Affordances.FirstOrDefault(option => option.CostOptions.Any(cost => cost.Target == option.AnchorId));
            if (cardPlay is null)
            {
                game.Resolve(policy.Answer(game));
            }
        }

        cardPlay = Assert.IsType<Affordance>(cardPlay);
        var composer = new DecisionComposer(game.Pending!);
        composer.SelectAffordance(cardPlay.Id);
        Assert.Contains(cardPlay.CostOptions, composer.CostApplies);
    }

    [Fact]
    public void APlayerChoosesHowWildIconsAreDeclared()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(9, "Play", 20, 0, "Relentless Assault", Costs: [new CostOption(20, "1", Sources: [new ResourceSource(40, "G")], DeclarationSensitive: true), ])));
        composer.SelectAffordance(9);
        composer.ToggleResource(40);
        Assert.False(composer.TryBuild(out _, out _));
        composer.AssignResource(40, 0, 0, Resources.Physical);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([new ResourceAllocation(40, 0, "R")], decision!.Allocations);
    }

    [Fact]
    public void GroupedSelectionsAcceptOnlyAnExactOfferedOrderedGroup()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(5, "Resolve", 10, 0, "Choose a group", new TargetRequest([11, 12, 13], 3, 3, Groups: [[11, 12], [11, 13]]))));
        composer.SelectAffordance(5);
        composer.SelectTargets([11]);
        Assert.False(composer.TryBuild(out _, out _));
        composer.SelectTargets([12, 11]);
        Assert.False(composer.TryBuild(out _, out _));
        composer.SelectTargets([11, 12]);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([11, 12], decision!.Targets);
    }

    [Fact]
    public void RepeatedSelectionsRespectPerTargetCapacities()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(6, "Allocate", 10, 0, "Deal indirect damage", new TargetRequest([11, 12], 3, 3, AllowRepeated: true, MaximumOccurrences: new Dictionary<int, int> { [11] = 1, [12] = 2, }))));
        composer.SelectAffordance(6);
        composer.SelectTargets([11, 11, 12]);
        Assert.False(composer.TryBuild(out _, out _));
        composer.SelectTargets([12, 11, 12]);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([12, 11, 12], decision!.Targets);
    }

    [Fact]
    public void FlatTargetProgressReportsTheOfferedRangeAndCurrentValidity()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(4, "Order", 10, 0, "Order enemies", new TargetRequest([11, 12, 13], 2, 3))));
        composer.SelectAffordance(4);
        composer.SelectTargets([12]);
        DecisionProgressPresentation incomplete = composer.Progress();
        Assert.Equal(new TargetSelectionProgress(TargetSelectionMode.Ordinary, 1, 2, 3, false), incomplete.Targets);
        Assert.False(incomplete.IsReady);
        composer.AddTarget(11);
        DecisionProgressPresentation complete = composer.Progress();
        Assert.Equal(2, complete.Targets.Selected);
        Assert.True(complete.Targets.IsSatisfied);
        Assert.True(complete.IsReady);
    }

    [Fact]
    public void GroupedTargetProgressIsZeroOrOneByExactOfferedGroup()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(5, "Resolve", 10, 0, "Choose a group", new TargetRequest([11, 12, 13], 3, 3, Groups: [[11, 12], [11, 13]]))));
        composer.SelectAffordance(5);
        composer.SelectTargets([11]);
        Assert.Equal(new TargetSelectionProgress(TargetSelectionMode.Grouped, 0, 1, 1, false), composer.Progress().Targets);
        composer.SelectTargets([11, 13]);
        Assert.Equal(new TargetSelectionProgress(TargetSelectionMode.Grouped, 1, 1, 1, true), composer.Progress().Targets);
    }

    [Fact]
    public void RepeatedTargetProgressCountsAllocationsAndUsesTheirCapacities()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(6, "Allocate", 10, 0, "Deal indirect damage", new TargetRequest([11, 12], 3, 3, AllowRepeated: true, MaximumOccurrences: new Dictionary<int, int> { [11] = 1, [12] = 2, }))));
        composer.SelectAffordance(6);
        composer.SelectTargets([11, 11, 12]);
        TargetSelectionProgress overCapacity = composer.Progress().Targets;
        Assert.Equal(TargetSelectionMode.Repeated, overCapacity.Mode);
        Assert.Equal(3, overCapacity.Selected);
        Assert.Equal(3, overCapacity.Minimum);
        Assert.Equal(3, overCapacity.Maximum);
        Assert.False(overCapacity.IsSatisfied);
        composer.SelectTargets([12, 11, 12]);
        Assert.True(composer.Progress().Targets.IsSatisfied);
    }

    [Fact]
    public void PaymentProgressCountsXDefinitionsGeneratorsAndComponentAssignments()
    {
        var cost = new CostOption(Target: 11, Cost: "X", Sources: [new ResourceSource(40, "YY"), new ResourceSource(41, "G"), ], Variables: [new VariableRequest("X", 1, 2)], Components: [new ResourceCost("1", ["Y"]), new ResourceCost("1")]);
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Paid action", new TargetRequest([11], 1, 1), [cost])));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.ToggleResource(40);
        composer.Define("X", 2);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        DecisionProgressPresentation partial = composer.Progress();
        Assert.Equal(CostSelectionState.Selected, partial.Payment.CostState);
        Assert.Equal(0, partial.Payment.SelectedCost);
        Assert.Equal(1, partial.Payment.CostOptions);
        Assert.Equal(1, partial.Payment.SelectedGenerators);
        Assert.Equal(2, partial.Payment.GeneratedIcons);
        Assert.Equal(1, partial.Payment.AssignedIcons);
        Assert.Equal(0, partial.Payment.ExcessIcons);
        Assert.Equal(1, partial.Payment.DefinedVariables);
        Assert.Equal(1, partial.Payment.RequestedVariables);
        Assert.False(partial.Payment.IsSatisfied);
        Assert.False(partial.IsReady);
        Assert.Equal("Assign generated icons to satisfy every offered cost component.", partial.Error);
        composer.AssignResource(40, 1, 1, Resources.Energy);
        DecisionProgressPresentation complete = composer.Progress();
        Assert.Equal(2, complete.Payment.AssignedIcons);
        Assert.Equal(0, complete.Payment.ExcessIcons);
        Assert.True(complete.Payment.IsSatisfied);
        Assert.True(complete.IsReady);
        Assert.Null(complete.Error);
    }

    [Fact]
    public void ACompleteOverpaymentReportsOnlyTheIconsThatWillBeLost()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Discounted card", Costs: [new CostOption(20, "0", Sources: [new ResourceSource(40, "YY")]), ])));
        composer.SelectAffordance(7);
        composer.ToggleResource(40);
        PaymentProgress payment = composer.Progress().Payment;
        Assert.True(payment.IsSatisfied);
        Assert.Equal(2, payment.GeneratedIcons);
        Assert.Equal(0, payment.AssignedIcons);
        Assert.Equal(2, payment.ExcessIcons);
    }
}
