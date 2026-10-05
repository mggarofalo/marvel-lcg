using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ResponseReceiptPresentationTests : EventPresentationTestBase
{
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
        Assert.Contains(receipt, item => item.Summary.Contains("0 damage", StringComparison.Ordinal));
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
