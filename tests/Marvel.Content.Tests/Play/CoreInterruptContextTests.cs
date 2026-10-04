using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Core interrupt offers explain the still-pending occurrence.</summary>
public sealed class CoreInterruptContextTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:reveal.step.1")]
    [Rule("rr:cancel.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnhancedSpiderSenseSeesOnlyTheCurrentRevealBeforeItsEffects(bool cancel)
    {
        // "Turn the encounter card faceup." Cancelling its When Revealed
        // effects does not cancel its revelation (rr:cancel.4).
        var world = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var sense = world.CreateCard(AuthoredCards.EnhancedSpiderSense, world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var dealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        var current = world.CreateCard(AuthoredCards.ImTough, dealt);
        var next = world.CreateCard("01109", dealt);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: current.ObjectId, Seat: 0));
        var events = new List<GameEvent>();

        var prompt = Sequence.Work(world, Cards, runner, events)!;

        Assert.NotNull(prompt);
        Assert.True(current.FaceUp);
        Assert.Equal(DeckType.RevealingArea, current.Area.Type);
        Assert.False(next.FaceUp);
        Assert.Same(dealt, next.Area);
        Assert.Equal([current.ObjectId], prompt.ContextCardIds);
        Assert.Equal($"{Cards.Title(current.FaceId)} was revealed — interrupt?", prompt.DisplayQuestion);
        Assert.Contains($"Peter revealed {Cards.Title(current.FaceId)}", prompt.Description);
        Assert.Contains("When Revealed effects have not resolved", prompt.Description);
        Assert.DoesNotContain(Cards.Title(next.FaceId), prompt.Description);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal([current.ObjectId], Assert.Single(events.OfType<CardsFlipped>()).Cards);
        Assert.Single(new Resolution(world, prompt, events).Information,
            signal => signal.Kind == InformationKind.Reveal);

        var repeatedEvents = new List<GameEvent>();
        var repeated = Sequence.Work(world, Cards, runner, repeatedEvents)!;
        Assert.Equal(prompt.DisplayQuestion, repeated.DisplayQuestion);
        Assert.Empty(repeatedEvents);
        Assert.Empty(new Resolution(world, repeated, repeatedEvents).Information);

        var offer = Assert.Single(prompt.Affordances, choice => choice.AnchorId == sense.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt,
            cancel ? Decision.Take(offer.Id, [], [payment.ObjectId]) : Decision.Decline, events);
        Assert.False(world.IsOver, world.Result.ToString());
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.False(world.IsOver, world.Result.ToString());

        Assert.Equal(!cancel, Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(DeckType.EncounterDiscardPile, current.Area.Type);
        Assert.Equal(cancel ? DeckType.DiscardPile : DeckType.HandsArea, sense.Area.Type);
        Assert.False(next.FaceUp);
        Assert.Single(events.OfType<CardsFlipped>());
    }

    [Rule("rr:prevent.2")]
    [Fact]
    public void GreatResponsibilityExplainsVillainPhaseThreatBeforePlacement()
    {
        // "Reduce the amount of threat being assigned before it is placed."
        // The scheme's current tokens are distinct from the pending amount.
        var world = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01061", world.Seats[0].Hand);
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        var prompt = Sequence.Work(world, Cards, runner, [])!;

        Assert.NotNull(prompt);
        Assert.Equal("Interrupt 1 threat being placed on The Break-In!?", prompt.DisplayQuestion);
        Assert.Contains("Step 1 of the villain phase would place 1 threat on The Break-In!", prompt.Description);
        Assert.Contains("currently has 0 threat", prompt.Description);
        Assert.Equal([scheme.ObjectId], prompt.ContextCardIds);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:prevent.2")]
    [Theory]
    [InlineData(ThreatCause.VillainPhase, "Step 1 of the villain phase")]
    [InlineData(ThreatCause.EnemyScheme, "An enemy's scheme activation")]
    [InlineData(ThreatCause.Incite, "Incite")]
    [InlineData(ThreatCause.CardAbility, "A card ability")]
    public void ThreatContextKeepsAssignedRemainingCurrentAndCauseDistinct(ThreatCause cause, string meaning)
    {
        var world = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01061", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var placement = new ThreatPlacement(scheme.ObjectId, scheme.ObjectId, 4, cause, "test", 0);
        placement.Prevent(2);
        var occurrence = Occurrence.ForThreat(1, [Steps.ThreatWouldBePlaced], world, Cards, placement);

        var prompt = Offering.Work(world, runner, occurrence, WindowKind.Interrupt, [])!;

        Assert.NotNull(prompt);
        Assert.Equal("Interrupt 2 threat being placed on The Break-In!?", prompt.DisplayQuestion);
        Assert.Contains($"{meaning} would place 2 threat", prompt.Description);
        Assert.Contains("currently has 3 threat", prompt.Description);
        Assert.Contains("4 threat was assigned; 2 remains after prevention", prompt.Description);
    }

    [Rule("rr:reveal.4.1")]
    [Fact]
    public void NamedObligationOccurrenceBelongsToTheNamedPlayerAtItsFirstWindow()
    {
        // "That player is considered to be revealing it." Ownership is
        // already correct in the interrupt window, before placement applies.
        var world = new World(Cards, players: 2);
        var peter = world.CreateSeat("Peter");
        peter.IdentityCard = world.CreateCard("01001a,01001b", peter.Hero);
        var jennifer = world.CreateSeat("Jennifer");
        jennifer.IdentityCard = world.CreateCard("01019a,01019b", jennifer.Hero);
        var obligation = world.CreateCard("01165",
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: obligation.ObjectId, Seat: 1));

        var occurrence = world.Agenda.Begin(world, Cards);

        Assert.Equal(0, occurrence.Player);
        Assert.Equal(obligation.ObjectId, occurrence.Subject);
        Assert.Same(occurrence, world.Agenda.Begin(world, Cards));
    }

    private static World Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("Peter");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        seat.IdentityCard.TurnTo("01001a");
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01110", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }
}
