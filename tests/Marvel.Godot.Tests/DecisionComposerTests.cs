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
                        20, "1", Sources: [new ResourceSource(40, "G")]),
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
