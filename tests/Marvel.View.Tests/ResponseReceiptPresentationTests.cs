using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ResponseReceiptPresentationTests : EventPresentationTestBase
{
    // Synthetic contract: hand-built engine strings prove the view carries them
    // into headings, costs and receipts. TeamBuildingExerciseActualTests proves
    // the engine authors them; this is not a played scenario.
    [Fact]
    public void MatchingPlayPromptAndReceiptsKeepReadableEngineMeaning()
    {
        WorldDescriptor world = World() with { Areas =
        [new AreaDescriptor(1, "SupportsArea", 0, -1, [Readable(8, "Team-Building Exercise")], []),
         new AreaDescriptor(2, "HandsArea", 0, -1, [Readable(9, "Army of Ants")], [])] };
        var action = new Affordance(3, "12024", 8, 0, "12024")
        { DisplayLabel = "Play a matching card", CommitLabel = "Play a matching card with Team-Building Exercise" };
        var turn = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "test", "test", true, [action]);
        Assert.Contains("Play a matching card with Team-Building Exercise", DecisionReceiptContext.From(turn, world, 3, [], [])!.Commitment);
        var play = new Affordance(9, "Choose", 9, 0, "03019", Costs: [new CostOption(0, "0")],
            Description: "This price already includes Team-Building Exercise's reduction of 1 (a cost can't go below 0).") { PlaysCard = true };
        var child = new Prompt(0, Question.Element, TimingPriority.Untimed, "test", "12024", false, [play])
        { DisplayQuestion = "Choose a matching card to play", ContextCardIds = [8], Description = "Team-Building Exercise was exhausted." };
        var presentation = PromptPresentation.From(child, world);
        Assert.Equal("Choose a matching card to play", presentation.Heading);
        Assert.Contains("Team-Building Exercise was exhausted", presentation.Resolution);
        Assert.Contains("Cost 0", Assert.Single(presentation.Affordances).Costs[0]);
        Assert.Contains("Play Army of Ants", DecisionReceiptContext.From(child, world, 9, [], [])!.Commitment);
    }

    [Fact]
    public void WindowEventReceiptNamesItsSourceAndCleanupDoesNotExposeInternalArea()
    {
        WorldDescriptor world = World() with { Areas =
        [new AreaDescriptor(1, "DiscardPile", 0, -1, [Readable(8, "Great Responsibility")], [])] };
        var offer = new Affordance(3, "Replace", 8, 0, "Great Responsibility")
        {
            PlaysCard = true, AnchorKind = AffordanceAnchorKind.Card,
            CommitLabel = "Spider-Man takes 1 damage instead of 1 threat",
        };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "test", true, [offer]) { PublicKind = PublicDecisionKind.Interrupt };
        DecisionReceiptContext accepted = DecisionReceiptContext.From(prompt, world, 3, [], [])!;
        Assert.Equal("Great Responsibility: Spider-Man takes 1 damage instead of 1 threat.", accepted.Commitment);
        GameEvent[] events = [Move(8, "RevealingArea", "DiscardPile", "Discard")];
        var batch = EventCuePlanner.Plan(events, world, Outcome.Unfinished);
        Assert.Contains(batch.History, entry => entry.Summary == "Peter Parker discarded Great Responsibility.");
        Assert.DoesNotContain(batch.History, entry => entry.Summary.Contains("revealing", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(BasicPowers.AttackVerb, "attacked")]
    [InlineData(BasicPowers.ThwartVerb, "thwarted")]
    public void BasicOccurrenceHeadlineNamesItsSourceAndTargetOnce(string verb, string wording)
    {
        WorldDescriptor world = World() with { Areas =
        [new AreaDescriptor(1, "HeroArea", 0, -1,
            [Readable(7, "Spider-Man"), Readable(9, "Rhino")], [])] };
        var offer = new Affordance(3, verb, 7, 0, verb)
        { AnchorKind = AffordanceAnchorKind.Card };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "test", true, [offer]);

        DecisionReceiptContext context = DecisionReceiptContext.From(prompt, world, 3, [9], [])!;

        Assert.Equal($"Spider-Man {wording} Rhino", context.OccurrenceTitle);
        Assert.Equal(7, context.SourceCardId);
        Assert.Equal([9], context.TargetCardIds);
        Assert.Null(DecisionReceiptContext.From(prompt, world, 3, [], [])!.OccurrenceTitle);
    }

    [Fact]
    public void ACurrentNestedCancellationNamesItsPlayPaymentAndScopeWithoutAnEarlierDiscard()
    {
        // Synthetic authorized response, isolated from the enclosing phase's
        // earlier discard. A latest response is not that whole journal unit.
        WorldDescriptor world = World() with { Areas =
        [
            new AreaDescriptor(1, "DiscardPile", 0, -1,
                [Readable(8, "Enhanced Spider-Sense"), Readable(10, "Great Responsibility"),
                 Readable(11, "Spider-Tracer")], []),
            new AreaDescriptor(2, "EncounterDiscardPile", -1, -1, [Readable(9, "Assault")], []),
        ] };
        GameEvent[] events =
        [
            Move(10, "HandsArea", "DiscardPile", "Discard") with { Trigger = "Play" },
            Move(8, "HandsArea", "RevealingArea", "Play"),
            new WhenRevealedCanceled(9, 8),
            Move(8, "RevealingArea", "DiscardPile", "Discard"),
            Move(9, "RevealingArea", "EncounterDiscardPile", "Discard"),
        ];
        EventBatchPresentation batch = EventCuePlanner.Plan(events, world, Outcome.Unfinished);
        var accepted = new DecisionReceiptContext(
            "Spider-Man: Play Enhanced Spider-Sense. Payment sources: Great Responsibility.", [8, 10]);
        IReadOnlyList<EventPresentation> receipt = ResponseReceiptPresenter.Present(events, world, batch, accepted);
        Assert.Equal(accepted.Commitment, receipt[0].Summary);
        Assert.Contains(receipt, item => item.Summary.Contains("discarded Great Responsibility", StringComparison.Ordinal));
        Assert.Contains(receipt, item => item.Summary == "Enhanced Spider-Sense canceled Assault's When Revealed effects.");
        Assert.DoesNotContain(receipt, item => item.Summary.Contains("discarded Enhanced Spider-Sense", StringComparison.Ordinal));
        Assert.DoesNotContain(receipt, item => item.Summary.Contains("Spider-Tracer", StringComparison.Ordinal));
    }

    [Fact]
    public void AChosenEventlessResourceSourceIsNamedInTheAuthorizedSubmittedContext()
    {
        // Synthetic offered resource relation uses public source names;
        // it does not infer a resource ability from printed card text.
        WorldDescriptor world = World() with { Areas =
        [
            new AreaDescriptor(1, "HandsArea", 0, -1, [Readable(7, "Web-Shooter")], []),
            new AreaDescriptor(2, "HeroArea", 0, -1, [Readable(9, "Peter Parker")], []),
        ] };
        var offer = new Affordance(3, CardPlay.Verb, 7, 0, "Play",
            Costs: [new CostOption(0, "1", Sources: [new ResourceSource(9, "W")])]);
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "test", "test", true, [offer]);
        DecisionReceiptContext context = Assert.IsType<DecisionReceiptContext>(
            DecisionReceiptContext.From(prompt, world, 3, [], [9]));
        Assert.Contains("Play Web-Shooter", context.Commitment);
        Assert.Equal(CardPlay.Verb, context.ActionVerb);
        Assert.Contains("Payment sources: Peter Parker", context.Commitment);
        Assert.Equal([7, 9], context.Anchors);
        Assert.Null(DecisionReceiptContext.From(prompt, world, 99, [], []));
        Assert.DoesNotContain("Payment sources", DecisionReceiptContext.From(prompt, world, 3, [], [12])!.Commitment);
    }

    [Fact]
    public void CompletedZeroDamageAndReturnedCardsRemainCurrentResults()
    {
        WorldDescriptor world = NarrativeWorld();
        GameEvent[] events =
        [
            new AttackCompleted(9, 7, 7) { DamageDealt = 0,
                Subjects = new Dictionary<int, string> { [9] = "Rhino", [7] = "Spider-Man" } },
            Move(14, "PlayerDeck", "HandsArea", "Add_To_Hand"),
        ];
        IReadOnlyList<EventPresentation> receipt = ResponseReceiptPresenter.Present(events, world,
            EventCuePlanner.Plan(events, world, Outcome.Unfinished));
        Assert.Contains(receipt, item => item.Summary.Contains("Attack damage before prevention: 0", StringComparison.Ordinal));
        Assert.Contains(receipt, item => item.Motion == EventMotionKind.HandGain
            && item.Summary.Contains("Swinging Web Kick", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void SuccessfulCancellationRetainsOnlyReadableSubjects(int? sourceSeat, bool expected)
    {
        // Synthetic visibility fixture isolates the new emitted event.
        var cards = Marvel.Content.CardCatalog.Parse(File.ReadAllText(
            Marvel.Tests.RepositoryPaths.Dataset("cards", "cards.json")));
        var state = new World(cards, players: 2);
        state.CreateSeat("Spider-Man");
        state.CreateSeat("Captain Marvel");
        Card assault = state.CreateCard("01187", state.AreaOf(DeckType.RevealingArea));
        Card? source = sourceSeat is { } seat ? state.CreateCard("01006", state.Seats[seat].Hand) : null;
        var fact = new WhenRevealedCanceled(assault.ObjectId, source?.ObjectId)
        { Subjects = new Dictionary<int, string> { [assault.ObjectId] = "Assault", [999] = "Secret" } };
        var scope = new RestrictedVisibilityPolicy(0).Authorize(null, state.Players);
        VisibleResult visible = WorldProjection.For(state, null, [fact], scope);
        Assert.Equal(expected, visible.Events.Count == 1);
        if (expected)
        {
            var retained = Assert.IsType<WhenRevealedCanceled>(Assert.Single(visible.Events));
            Assert.DoesNotContain(999, retained.Subjects!.Keys);
            EventPresentation presentation = Assert.Single(EventPresenter.PresentNarrative(visible.Events, visible.World));
            Assert.Contains("When Revealed effects", presentation.Summary);
            if (source is null) Assert.DoesNotContain("Enhanced Spider-Sense", presentation.Summary);
        }
    }

    [Theory]
    [InlineData("BOOST_CONST", "Boost")]
    [InlineData("BoostConst", "Boost")]
    [InlineData("QUICKSTRIKE", "Quickstrike")]
    public void LiveFieldNamesDescribeTheirMeaning(string key, string expected) =>
        Assert.Equal(expected, BoardFieldNames.Display(key));
}
