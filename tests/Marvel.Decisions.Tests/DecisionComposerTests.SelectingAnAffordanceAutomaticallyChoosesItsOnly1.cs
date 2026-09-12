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
public sealed class DecisionComposerSelectingAnAffordanceAutomaticallyChoosesItsOnlyTests : DecisionComposerTestBase
{
    [Fact]
    public void SelectingAnAffordanceAutomaticallyChoosesItsOnlyRequiredTarget()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Play", new TargetRequest([11], 1, 1))));
        composer.SelectAffordance(7);
        Assert.True(composer.UsesAutomaticTargetSelection);
        Assert.Equal([11], composer.Targets);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([11], decision!.Targets);
    }

    [Fact]
    public void PresentationUsesOnlyAuthorizedFacesAndOpaqueFallbacks()
    {
        var prompt = Prompt(cancellable: true, new Affordance(4, "Attack", 10, 0, "Basic attack", new TargetRequest([11, 99], 1, 1)));
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [new AreaDescriptor(2, "HeroArea", 0, -1, [Card(10, "Spider-Man"), new CardDescriptor(11, CardBack.Encounter, false, true, -1, null)], [])], [], Outcome.Unfinished);
        PromptPresentation view = PromptPresentation.From(prompt, world);
        AffordancePresentation option = Assert.Single(view.Affordances);
        Assert.Equal("Spider-Man", option.Anchor);
        Assert.Equal("Choose 1 targets from 2", option.Targets);
        Assert.DoesNotContain("99", option.Targets);
        Assert.DoesNotContain("Spider-Man", PromptPresentation.Describe(11, world));
        Assert.Equal("Object 99", PromptPresentation.Describe(99, world));
        Assert.Equal("You may pass.", view.Requirement);
        Assert.DoesNotContain("Untimed", view.Context);
    }

    [Fact]
    public void PresentationCarriesEngineCombatContextAndTargetDetails()
    {
        var prompt = Prompt(cancellable: true, new Affordance(4, "Attack", 10, 0, "Attack", new TargetRequest([11], 1, 1) { Details = new Dictionary<int, string> { [11] = "8/14 HP · Retaliate 1" }, }))with
        {
            Description = "Rhino attacks Spider-Man for 5 damage.",
        };
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [new AreaDescriptor(2, "HeroArea", 0, -1, [Card(10, "Spider-Man"), Card(11, "Rhino")], [])], [], Outcome.Unfinished);
        PromptPresentation view = PromptPresentation.From(prompt, world);
        Assert.DoesNotContain(prompt.Description, view.Context);
        Assert.Equal(prompt.Description, view.Resolution);
        Assert.Equal("8/14 HP · Retaliate 1", prompt.Affordances[0].Targets!.Details![11]);
    }

    [Fact]
    public void PresentationCarriesEngineAuthoredActionDescriptionUnchanged()
    {
        var prompt = Prompt(cancellable: false, new Affordance(4, "Choose", 10, 0, "draw", Description: "Draw 3 cards"));
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished);
        AffordancePresentation option = Assert.Single(PromptPresentation.From(prompt, world).Affordances);
        Assert.Equal("draw", option.Label);
        Assert.Equal("Draw 3 cards", option.Description);
    }

    [Fact]
    public void PresentationTurnsACardChoiceIntoAPlayerQuestionAndKeepsWireDataDiagnostic()
    {
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "CardRevealed", "01092: choose a card", false, [new Affordance(4, "Choose", 10, 0, "01001a")]);
        var source = new CardDescriptor(9, CardBack.Player, true, false, -1, new CardFaceDescriptor("01092", "Helicarrier", "", CardKind.Support, new Dictionary<string, long>(StringComparer.Ordinal)));
        var identity = new CardDescriptor(10, CardBack.Player, true, true, -1, new CardFaceDescriptor("01001a", "Spider-Man", "Peter Parker", CardKind.Hero, new Dictionary<string, long>(StringComparer.Ordinal)));
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [new AreaDescriptor(2, "SupportsArea", 0, -1, [source, identity], [])], [], Outcome.Unfinished);
        PromptPresentation view = PromptPresentation.From(prompt, world);
        Assert.Equal("Choose a player for Helicarrier", view.Heading);
        Assert.Equal("From Helicarrier", view.Context);
        Assert.DoesNotContain("01092", view.Heading);
        Assert.Contains("Element", view.Diagnostic);
        Assert.Contains("01092: choose a card", view.Diagnostic);
    }

    [Fact]
    public void PresentationExplainsWhenACardsCurrentCostDiffersFromPrintedCost()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn", "\n--- Peter Parker's Turn (1) ---", true, [new Affordance(4, "Play", 10, 0, "Play", Costs: [new CostOption(10, "0")])]);
        var card = new CardDescriptor(10, CardBack.Player, true, true, -1, new CardFaceDescriptor("01006", "Web-Shooter", "", CardKind.Upgrade, new Dictionary<string, long>(StringComparer.Ordinal)) { Cost = "1", });
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [new AreaDescriptor(2, "HandsArea", 0, -1, [card], [])], [], Outcome.Unfinished);
        PromptPresentation view = PromptPresentation.From(prompt, world);
        Assert.Equal("Peter Parker's turn", view.Heading);
        Assert.Equal("Current cost 0; printed cost 1.", Assert.Single(view.Affordances).Consequence);
    }

    [Theory]
    [InlineData(TimingPriority.Interrupt, "Choose an interrupt")]
    [InlineData(TimingPriority.Response, "Choose a response")]
    public void PresentationNamesTheAbilityTimingThePlayerCanUse(TimingPriority timing, string expected)
    {
        var prompt = new Prompt(0, Question.Opportunity, timing, "CardRevealed", "Ability window", true, [new Affordance(1, "Use", 10, 0, "Use ability")]);
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [], [], Outcome.Unfinished);
        Assert.Equal(expected, PromptPresentation.From(prompt, world).Heading);
    }

    [Fact]
    public void PresentationNamesTheEndPhaseDiscardInsteadOfCallingItAPlayerTurn()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "End Turn", "Peter Parker End Phase", false, [new Affordance(1, "End Phase", 10, 0, "End Phase")]);
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [], [], Outcome.Unfinished);
        Assert.Equal("Choose end-of-phase discards", PromptPresentation.From(prompt, world).Heading);
    }

    [Fact]
    public void PresentationReplacesAttachmentAndChoiceCardCodesWithReadableNames()
    {
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "ChooseAttachmentTarget", "Spider-Man chooses where 01185 attaches", false, [new Affordance(1, "Choose", 10, 0, "01031")]);
        var source = new CardDescriptor(9, CardBack.Encounter, true, true, -1, new CardFaceDescriptor("01185", "Biomechanical Upgrades", "", CardKind.Attachment, new Dictionary<string, long>(StringComparer.Ordinal)));
        var target = new CardDescriptor(10, CardBack.Player, true, true, -1, new CardFaceDescriptor("01031", "Repulsor Blast", "", CardKind.Event, new Dictionary<string, long>(StringComparer.Ordinal)));
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Peter Parker", false)], [new AreaDescriptor(2, "RevealingArea", -1, -1, [source, target], [])], [], Outcome.Unfinished);
        PromptPresentation view = PromptPresentation.From(prompt, world);
        Assert.Equal("Choose where to attach Biomechanical Upgrades", view.Heading);
        Assert.DoesNotContain("01185", view.Heading);
        Assert.Equal("Choose", Assert.Single(view.Affordances).Label);
    }

    [Fact]
    public void FreeSelectionsKeepPlayerOrderAndRejectIncompleteTargets()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(4, "Order", 10, 0, "Order enemies", new TargetRequest([11, 12], 2, 2))));
        composer.SelectAffordance(4);
        composer.SelectTargets([12]);
        Assert.False(composer.TryBuild(out _, out _));
        composer.AddTarget(11);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([12, 11], decision!.Targets);
        Assert.Null(decision.Resources);
    }

    [Fact]
    public void ChoosingAnotherSingleTargetReplacesThePriorTarget()
    {
        var composer = new DecisionComposer(Prompt(cancellable: true, new Affordance(4, "Attack", 10, 0, "Attack", new TargetRequest([11, 12], 1, 1))));
        composer.SelectAffordance(4);
        composer.AddTarget(11);
        composer.AddTarget(12);
        Assert.Equal([12], composer.Targets);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([12], decision!.Targets);
    }

    [Fact]
    public void PaymentKeepsChosenGeneratorsVariablesAndComponentAllocation()
    {
        var cost = new CostOption(Target: 11, Cost: "X", Sources: [new ResourceSource(40, "YY"), new ResourceSource(41, "G"), ], Variables: [new VariableRequest("X", 1, 2)], Components: [new ResourceCost("1", ["Y"]), new ResourceCost("1")]);
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Paid action", new TargetRequest([11], 1, 1), [cost])));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.ToggleResource(40);
        composer.Define("X", 2);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        composer.AssignResource(40, 1, 1, Resources.Energy);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([40], decision!.Resources);
        Assert.Equal(2, decision.Values!["X"]);
        Assert.Equal([new ResourceAllocation(40, 0, "Y"), new ResourceAllocation(40, 1, "Y")], decision.Allocations);
    }

    [Fact]
    public void OneComponentPaymentAutomaticallyUsesPrintedNonWildResources()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Paid action", Costs: [new CostOption(20, "1", Sources: [new ResourceSource(40, "YY"), new ResourceSource(41, "G"), ], DeclarationSensitive: true), ])));
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
    public void AutomaticResourceAllocationTracksVariableCostChanges()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Variable-cost action", Costs: [new CostOption(20, "X", Sources: [new ResourceSource(40, "YY")], Variables: [new VariableRequest("X", 1, 2)]), ])));
        composer.SelectAffordance(7);
        composer.Define("X", 1);
        composer.ToggleResource(40);
        Assert.Single(composer.Assignments);
        composer.Define("X", 2);
        Assert.Equal(2, composer.Assignments.Count);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal([new ResourceAllocation(40, 0, "YY")], decision!.Allocations);
        composer.Define("X", 1);
        Assert.Single(composer.Assignments);
    }
}
