using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SpatialInteractionProjectionTests
{
    [Fact]
    public void ExplicitCardAnchorRemainsOperableWhenNoLocatedSourceIsProjected()
    {
        var composer = Composer(new Affordance(9, "End Phase", 1, 0, "End Phase"));
        var visible = new AffordancePresentation(
            9, "End Phase", null, "End Phase", "Spider-Man", 1, 0, null, "", [])
        {
            AnchorKind = AffordanceAnchorKind.Card,
        };
        PromptPresentation prompt = Prompt([visible]);

        CardInteractionControlDescriptor action = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt));

        Assert.Equal(1, action.CardId);
        Assert.Equal(CardInteractionIntent.Action, action.Intent);
        Assert.Equal(CardInteractionCue.OfferedAction,
            BoardInteractionCueProjection.From(composer, prompt)[1]);
    }

    [Fact]
    public void AbilityAlternativeCostsStayInTheTaskDockInsteadOfCoveringTheirSource()
    {
        CostOption[] costs = [new(19, "first"), new(19, "second")];
        var composer = Composer(new Affordance(3, "Use", 19, 0, "Visible", Costs: costs));
        composer.SelectAffordance(3);

        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer,
            Prompt([Visible(3, 19)], costs)), control => control.Intent == CardInteractionIntent.Cost);
        Assert.Equal(2, composer.Selected!.CostOptions.Count);
    }

    [Fact]
    public void ReadyDraftKeepsItsCommitmentAwayFromCardFaces()
    {
        var composer = Composer(new Affordance(3, "Use", 19, 0, "Visible"));
        composer.SelectAffordance(3);

        Assert.True(composer.Progress().IsReady);
        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer,
            Prompt([Visible(3, 19)])), control => control.Intent == CardInteractionIntent.Submit);
    }

    [Fact]
    public void SelectedCardPlayRemovesCompetingActionsAndLocalCompositionControls()
    {
        var composer = Composer(
            new Affordance(3, "Play", 19, 0, "Web-Shooter"),
            new Affordance(4, "Use", 20, 0, "Change form"));
        composer.SelectAffordance(3);

        IReadOnlyList<CardInteractionControlDescriptor> controls =
            BoardInteractionControlProjection.From(
                composer, Prompt([Visible(3, 19), Visible(4, 20)]));

        Assert.DoesNotContain(controls, control => control.Intent == CardInteractionIntent.Action);
        Assert.Empty(controls);
    }

    [Fact]
    public void TableDraftSummaryNamesTheSelectedActionAndPaymentProgress()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Web-Shooter",
            Costs: [new CostOption(19, "1", Sources: [new ResourceSource(41, "Y")])]));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(
            [Visible(3, 19) with { Label = "Web-Shooter", Anchor = "Web-Shooter" }],
            composer.Selected!.CostOptions);

        string before = Assert.IsType<string>(TableDraftSummary.From(composer, prompt));
        Assert.Contains("Web-Shooter", before);
        Assert.Contains("0 resources selected", before);
        Assert.DoesNotContain("Choose payment sources", before);
        Assert.DoesNotContain("Payment complete", before);

        composer.ToggleResource(41);

        string ready = Assert.IsType<string>(TableDraftSummary.From(composer, prompt));
        Assert.Contains("1 resource selected", ready);
        Assert.DoesNotContain("Payment complete", ready);
        Assert.DoesNotContain("Confirm below", ready);
    }

    [Fact]
    public void TableDraftSummaryKeepsConsequentialExcessVisible()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible",
            Costs: [new CostOption(19, "1", Sources: [new ResourceSource(41, "YY")])]));
        composer.SelectAffordance(3);
        composer.ToggleResource(41);

        string summary = Assert.IsType<string>(TableDraftSummary.From(composer, Prompt([Visible(3, 19)])));

        Assert.Contains("2 resources selected", summary);
        Assert.Contains("1 excess resource will be lost", summary);
    }

    [Fact]
    public void TypedPaymentDoesNotAdvertiseANonmatchingSourceAsUseful()
    {
        var cost = new CostOption(19, "1", Rule: ["R"], Sources:
            [new ResourceSource(41, "B"), new ResourceSource(42, "R"), new ResourceSource(43, "G")]);
        var composer = Composer(new Affordance(3, "Use", 19, 0, "Tenacity", Costs: [cost]));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt([Visible(3, 19)], [cost]);

        var controls = BoardInteractionControlProjection.From(composer, prompt);
        var mental = Assert.Single(controls, control => control.CardId == 41);
        Assert.Equal("Does not pay this cost", mental.Text);
        Assert.Equal(CardInteractionCue.Unavailable, mental.Cue);
        Assert.All(controls.Where(control => control.CardId is 42 or 43), control =>
            Assert.Equal(CardInteractionCue.LegalGenerator, control.Cue));
        Assert.DoesNotContain(41, BoardInteractionCueProjection.From(composer, prompt).Keys);

        // A draft loaded with an unusable source is still explained and cannot commit.
        composer.ToggleResource(41);
        Assert.False(composer.Progress().Payment.CanCoverCost);
        Assert.Contains("required types", TableDraftSummary.From(composer, prompt));
        Assert.False(composer.TryBuild(out _, out _));

        composer.ToggleResource(42);
        Assert.True(composer.Progress().Payment.CanCoverCost);
        Assert.True(composer.TryBuild(out _, out _));
        Assert.Contains("1 excess resource will be lost", TableDraftSummary.From(composer, prompt));
    }

    [Fact]
    public void TableDraftSummaryDoesNotInventTargetsForAnOfferWithoutTargetSelection()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Web-Shooter"));
        composer.SelectAffordance(3);

        string summary = Assert.IsType<string>(TableDraftSummary.From(composer, Prompt([Visible(3, 19)])));

        Assert.DoesNotContain("No resource payment", summary);
        Assert.DoesNotContain("Choose 0", summary);
        Assert.Null(TableDraftSummary.From(Composer(), Prompt([])));
    }

    [Fact]
    public void TableDraftSummaryNamesSelectedObjectsAndTheirOfferedConsequences()
    {
        // Synthetic contract: the UI copies the supplied consequence without calculating it.
        var request = new TargetRequest([41, 42], 1, 1)
        {
            Details = new Dictionary<int, string> { [42] = "Rhino takes the offered attack." },
        };
        var composer = Composer(new Affordance(3, "Attack", 19, 0, "Attack", Targets: request));
        composer.SelectAffordance(3);
        composer.SelectTargets([42]);
        var world = new WorldDescriptor([], [new AreaDescriptor(1, "VillainArea", -1, -1,
            [new CardDescriptor(42, CardBack.Encounter, true, true, -1,
                new CardFaceDescriptor("01094", "Rhino", "", CardKind.EncounterVillain,
                    new Dictionary<string, long>()))], [])], [], Outcome.Unfinished);

        string summary = Assert.IsType<string>(TableDraftSummary.From(composer, Prompt([Visible(3, 19)]), world));

        Assert.Contains("Rhino: Rhino takes the offered attack.", summary);
        Assert.DoesNotContain("Object 42", summary);
    }

    [Fact]
    public void TableDraftSummaryPreservesOfferedExhaustionAndDescribesOptionalDiscards()
    {
        var attack = Composer(new Affordance(3, "Attack", 19, 0, "Attack"));
        attack.SelectAffordance(3);
        string attackSummary = Assert.IsType<string>(TableDraftSummary.From(attack,
            Prompt([Visible(3, 19) with { Description = "Exhaust Spider-Man to attack." }])));
        Assert.Contains("Exhaust Spider-Man to attack.", attackSummary);
        Assert.DoesNotContain("No resource payment", attackSummary);

        var discard = Composer(new Affordance(4, Game.EndPhaseVerb, 19, 0, Game.EndPhaseVerb,
            Targets: new TargetRequest([41, 42, 43], 0, 3)));
        discard.SelectAffordance(4);
        discard.SelectTargets([42]);
        string discardSummary = Assert.IsType<string>(TableDraftSummary.From(discard, Prompt([Visible(4, 19)])));
        Assert.Contains("1 card staged for discard", discardSummary);
        Assert.Contains("(up to 3)", discardSummary);
        Assert.DoesNotContain("Choose 0", discardSummary);
    }

    [Theory]
    [InlineData(Game.ResolveMulligans, "replacement")]
    [InlineData(Game.EndPhaseVerb, "discard")]
    public void HandChoiceDraftNamesTheSelectionWithoutRepeatingCausalInstructions(string verb, string purpose)
    {
        var request = new TargetRequest([41, 42], 0, 2)
        {
            Details = new Dictionary<int, string> { [42] = "Engine-authored card detail." },
        };
        var composer = Composer(new Affordance(3, verb, 19, 0, verb, Targets: request));
        composer.SelectAffordance(3);
        composer.SelectTargets([42]);
        PromptPresentation prompt = Prompt([Visible(3, 19) with
        {
            Description = "Choose cards to discard. Unselected cards stay in your hand.",
            Consequence = "The engine supplied this explanation to the causal context.",
        }]);

        string summary = Assert.IsType<string>(TableDraftSummary.From(composer, prompt));

        Assert.Contains($"1 card staged for {purpose} (up to 2).", summary);
        Assert.Contains("Engine-authored card detail.", summary);
        Assert.DoesNotContain("Choose cards to discard", summary);
        Assert.DoesNotContain("The engine supplied this explanation", summary);
        if (verb == Game.ResolveMulligans)
            Assert.Contains("Replacements are drawn after", summary);
    }

    [Fact]
    public void RequiredDiscardDraftKeepsMinimumAndMaximumWithoutGenericChoiceInstructions()
    {
        var composer = Composer(new Affordance(3, Game.EndPhaseVerb, 19, 0, Game.EndPhaseVerb,
            Targets: new TargetRequest([41, 42, 43], 1, 3)));
        composer.SelectAffordance(3);
        composer.SelectTargets([42]);

        string summary = Assert.IsType<string>(TableDraftSummary.From(composer, Prompt([Visible(3, 19)])));

        Assert.Equal("1 card staged for discard (1–3 required).", summary);
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static PromptPresentation Prompt(
        IReadOnlyList<AffordancePresentation> affordances,
        IReadOnlyList<CostOption>? costs = null) => new("", "", "", "", "",
        affordances.Select(view => view with { CostOptions = costs ?? [] }).ToArray());

    private static AffordancePresentation Visible(int id, int card) => new(
        id, "Visible", null, "Play", "Visible", card, 0, null, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, card, null, 0),
    };
}
