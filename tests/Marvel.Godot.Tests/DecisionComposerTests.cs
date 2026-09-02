using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionComposerTests
{
    [Fact]
    public void SelectingAnAffordanceAutomaticallyChoosesItsOnlyRequiredTarget()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                7,
                "Play",
                20,
                0,
                "Play",
                new TargetRequest([11], 1, 1))));

        composer.SelectAffordance(7);

        Assert.True(composer.UsesAutomaticTargetSelection);
        Assert.Equal([11], composer.Targets);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([11], decision!.Targets);
    }

    [Fact]
    public void PresentationUsesOnlyAuthorizedFacesAndOpaqueFallbacks()
    {
        var prompt = Prompt(
            cancellable: true,
            new Affordance(4, "Attack", 10, 0, "Basic attack",
                new TargetRequest([11, 99], 1, 1)));
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Peter Parker", false)],
            [new AreaDescriptor(2, "HeroArea", 0, -1,
                [Card(10, "Spider-Man"), new CardDescriptor(11, CardBack.Encounter,
                    false, true, -1, null)], [])],
            [], Outcome.Unfinished);

        PromptPresentation view = PromptPresentation.From(prompt, world);

        AffordancePresentation option = Assert.Single(view.Affordances);
        Assert.Equal("Spider-Man", option.Anchor);
        Assert.Equal("Choose 1 targets from 2", option.Targets);
        Assert.DoesNotContain("99", option.Targets);
        Assert.DoesNotContain("Spider-Man", PromptPresentation.Describe(11, world));
        Assert.Equal("Object 99", PromptPresentation.Describe(99, world));
        Assert.Equal("OPTIONAL · PASS AVAILABLE", view.Requirement);
    }

    [Fact]
    public void FreeSelectionsKeepPlayerOrderAndRejectIncompleteTargets()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(4, "Order", 10, 0, "Order enemies",
                new TargetRequest([11, 12], 2, 2))));
        composer.SelectAffordance(4);
        composer.SelectTargets([12]);

        Assert.False(composer.TryBuild(out _, out _));

        composer.AddTarget(11);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([12, 11], decision!.Targets);
        Assert.Null(decision.Resources);
    }

    [Fact]
    public void PaymentKeepsChosenGeneratorsVariablesAndComponentAllocation()
    {
        var cost = new CostOption(
            Target: 11,
            Cost: "X",
            Sources:
            [
                new ResourceSource(40, "YY"),
                new ResourceSource(41, "G"),
            ],
            Variables: [new VariableRequest("X", 1, 2)],
            Components: [new ResourceCost("1", ["Y"]), new ResourceCost("1")]);
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Paid action",
                new TargetRequest([11], 1, 1), [cost])));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.ToggleResource(40);
        composer.Define("X", 2);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        composer.AssignResource(40, 1, 1, Resources.Energy);

        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([40], decision!.Resources);
        Assert.Equal(2, decision.Values!["X"]);
        Assert.Equal(
            [new ResourceAllocation(40, 0, "Y"), new ResourceAllocation(40, 1, "Y")],
            decision.Allocations);
    }

    [Fact]
    public void OneComponentPaymentAutomaticallyUsesPrintedNonWildResources()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                7,
                "Play",
                20,
                0,
                "Paid action",
                Costs:
                [
                    new CostOption(
                        20,
                        "1",
                        Sources:
                        [
                            new ResourceSource(40, "YY"),
                            new ResourceSource(41, "G"),
                        ],
                        DeclarationSensitive: true),
                ])));
        composer.SelectAffordance(7);

        composer.ToggleResource(40);

        Assert.True(composer.UsesAutomaticResourceAllocation);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([40], decision!.Resources);
        Assert.Equal([new ResourceAllocation(40, 0, "Y")], decision.Allocations);

        composer.ToggleResource(41);

        Assert.False(composer.UsesAutomaticResourceAllocation);
        Assert.False(composer.TryBuild(out _, out _));
        Assert.Empty(composer.Assignments);
    }

    [Fact]
    public void UnobservedWildDeclarationIsSentWithoutAskingThePlayer()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                7,
                "Play",
                20,
                0,
                "Ordinary card cost",
                Costs:
                [
                    new CostOption(
                        20,
                        "1",
                        Sources: [new ResourceSource(41, "G")]),
                ])));
        composer.SelectAffordance(7);

        composer.ToggleResource(41);

        Assert.True(composer.UsesAutomaticResourceAllocation);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([new ResourceAllocation(41, 0, "G")], decision!.Allocations);
    }

    [Fact]
    public void ACostRequirementForcesTheAutomaticWildDeclaration()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                7,
                "Play",
                20,
                0,
                "Required resource",
                Costs:
                [
                    new CostOption(
                        20,
                        "1",
                        Rule: ["R"],
                        Sources: [new ResourceSource(41, "G")]),
                ])));
        composer.SelectAffordance(7);

        composer.ToggleResource(41);

        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([new ResourceAllocation(41, 0, "R")], decision!.Allocations);
    }

    [Fact]
    public void TargetSpecificCostsMustMatchTheSelectedTarget()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Variable price",
                new TargetRequest([11, 12], 1, 1),
                [new CostOption(11, "0"), new CostOption(12, "0")])));
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                8, "Play", 20, 0, "Play support",
                Costs: [new CostOption(20, "0")])));
        composer.SelectAffordance(8);

        Assert.True(composer.CostApplies(composer.Selected!.CostOptions[0]));
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Empty(decision!.Targets);
    }

    [Fact]
    public void ACommittedCoreCardPlayUsesItsAnchorScopedPrice()
    {
        OpenedGame opened = DatasetGameFactory.Load(RepositoryPaths.Root).Create(
            new GameSpecification("rhino", ["spider_man"], null, 7));
        Game game = opened.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        Affordance? cardPlay = null;
        for (int step = 0; step < 20 && cardPlay is null; step++)
        {
            cardPlay = game.Pending!.Affordances.FirstOrDefault(option =>
                option.CostOptions.Any(cost => cost.Target == option.AnchorId));
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(
                9, "Play", 20, 0, "Relentless Assault",
                Costs:
                [
                    new CostOption(
                        20,
                        "1",
                        Sources: [new ResourceSource(40, "G")],
                        DeclarationSensitive: true),
                ])));
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(5, "Resolve", 10, 0, "Choose a group",
                new TargetRequest(
                    [11, 12, 13], 3, 3,
                    Groups: [[11, 12], [11, 13]]))));
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(6, "Allocate", 10, 0, "Deal indirect damage",
                new TargetRequest(
                    [11, 12], 3, 3,
                    AllowRepeated: true,
                    MaximumOccurrences: new Dictionary<int, int>
                    {
                        [11] = 1,
                        [12] = 2,
                    }))));
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(4, "Order", 10, 0, "Order enemies",
                new TargetRequest([11, 12, 13], 2, 3))));
        composer.SelectAffordance(4);
        composer.SelectTargets([12]);

        DecisionProgressPresentation incomplete = composer.Progress();

        Assert.Equal(
            new TargetSelectionProgress(
                TargetSelectionMode.Ordinary, 1, 2, 3, false),
            incomplete.Targets);
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
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(5, "Resolve", 10, 0, "Choose a group",
                new TargetRequest(
                    [11, 12, 13], 3, 3,
                    Groups: [[11, 12], [11, 13]]))));
        composer.SelectAffordance(5);
        composer.SelectTargets([11]);

        Assert.Equal(
            new TargetSelectionProgress(
                TargetSelectionMode.Grouped, 0, 1, 1, false),
            composer.Progress().Targets);

        composer.SelectTargets([11, 13]);

        Assert.Equal(
            new TargetSelectionProgress(
                TargetSelectionMode.Grouped, 1, 1, 1, true),
            composer.Progress().Targets);
    }

    [Fact]
    public void RepeatedTargetProgressCountsAllocationsAndUsesTheirCapacities()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(6, "Allocate", 10, 0, "Deal indirect damage",
                new TargetRequest(
                    [11, 12], 3, 3,
                    AllowRepeated: true,
                    MaximumOccurrences: new Dictionary<int, int>
                    {
                        [11] = 1,
                        [12] = 2,
                    }))));
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
        var cost = new CostOption(
            Target: 11,
            Cost: "X",
            Sources:
            [
                new ResourceSource(40, "YY"),
                new ResourceSource(41, "G"),
            ],
            Variables: [new VariableRequest("X", 1, 2)],
            Components: [new ResourceCost("1", ["Y"]), new ResourceCost("1")]);
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Paid action",
                new TargetRequest([11], 1, 1), [cost])));
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
        Assert.Equal(1, partial.Payment.DefinedVariables);
        Assert.Equal(1, partial.Payment.RequestedVariables);
        Assert.False(partial.Payment.IsSatisfied);
        Assert.False(partial.IsReady);
        Assert.Equal(
            "Assign generated icons to satisfy every offered cost component.",
            partial.Error);

        composer.AssignResource(40, 1, 1, Resources.Energy);
        DecisionProgressPresentation complete = composer.Progress();

        Assert.Equal(2, complete.Payment.AssignedIcons);
        Assert.True(complete.Payment.IsSatisfied);
        Assert.True(complete.IsReady);
        Assert.Null(complete.Error);
    }

    [Fact]
    public void ProgressResetsPaymentWhenTheAffordanceOrPricedTargetChanges()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Variable price",
                new TargetRequest([11, 12], 1, 1),
                [
                    new CostOption(11, "1", Sources: [new ResourceSource(40, "Y")]),
                    new CostOption(12, "0"),
                ]),
            new Affordance(8, "Thwart", 21, 0, "Free action")));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.SelectCost(0);
        composer.ToggleResource(40);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        Assert.True(composer.Progress().Payment.IsSatisfied);

        composer.SelectTargets([12]);
        PaymentProgress changedTarget = composer.Progress().Payment;

        Assert.Equal(CostSelectionState.Required, changedTarget.CostState);
        Assert.Null(changedTarget.SelectedCost);
        Assert.Equal(0, changedTarget.SelectedGenerators);
        Assert.Equal(0, changedTarget.AssignedIcons);

        composer.SelectAffordance(8);
        DecisionProgressPresentation changedAffordance = composer.Progress();

        Assert.Equal(TargetSelectionMode.None, changedAffordance.Targets.Mode);
        Assert.Equal(CostSelectionState.NotRequired, changedAffordance.Payment.CostState);
        Assert.True(changedAffordance.IsReady);
    }

    [Fact]
    public void ChangingATargetClearsEvenAnAutomaticallySelectedSinglePayment()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Paid action",
                new TargetRequest([11, 12], 1, 1),
                [new CostOption(
                    0,
                    "1",
                    Sources: [new ResourceSource(40, "Y")])])));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.ToggleResource(40);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        Assert.True(composer.Progress().Payment.IsSatisfied);

        composer.SelectTargets([12]);
        PaymentProgress changedTarget = composer.Progress().Payment;

        Assert.Equal(CostSelectionState.Selected, changedTarget.CostState);
        Assert.Equal(0, changedTarget.SelectedCost);
        Assert.Equal(0, changedTarget.SelectedGenerators);
        Assert.Equal(0, changedTarget.AssignedIcons);
        Assert.False(changedTarget.IsSatisfied);
    }

    [Fact]
    public void ProgressDoesNotRetainVisibleOrConcealedObjectIdentities()
    {
        const int visibleTarget = 987654;
        const int concealedGenerator = 765432;
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Paid action",
                new TargetRequest([visibleTarget], 1, 1),
                [new CostOption(
                    visibleTarget,
                    "1",
                    Sources: [new ResourceSource(concealedGenerator, "Y")])])));
        composer.SelectAffordance(7);
        composer.SelectTargets([visibleTarget]);
        composer.ToggleResource(concealedGenerator);
        composer.AssignResource(concealedGenerator, 0, 0, Resources.Energy);

        string progress = composer.Progress().ToString();

        Assert.DoesNotContain(visibleTarget.ToString(), progress);
        Assert.DoesNotContain(concealedGenerator.ToString(), progress);
    }

    [Fact]
    public void IllegalAndForcedOptionsCannotCreateAuthority()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(3, "Play", 9, 0, "Too expensive", Illegal: "Need 3")));

        Assert.False(composer.TryDecline(out _, out _));
        composer.SelectAffordance(3);
        Assert.False(composer.TryBuild(out _, out string? error));
        Assert.Equal("Need 3", error);
    }

    [Fact]
    public void ADeclineIsBuiltOnlyFromTheCurrentCancellablePrompt()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: true,
            new Affordance(3, "Respond", 9, 0, "Optional response")));

        Assert.True(composer.TryDecline(out EngineDecision? decision, out _));
        Assert.Equal(EngineDecision.Decline, decision);
    }

    private static Prompt Prompt(bool cancellable, params Affordance[] affordances) =>
        new(
            0, Question.Order, TimingPriority.Untimed, "test", "Choose now",
            cancellable, affordances);

    private static CardDescriptor Card(int id, string title) =>
        new(id, CardBack.Player, true, true, -1,
            new CardFaceDescriptor("face", title, "", CardKind.Hero,
                new Dictionary<string, long>(StringComparer.Ordinal)));

}
