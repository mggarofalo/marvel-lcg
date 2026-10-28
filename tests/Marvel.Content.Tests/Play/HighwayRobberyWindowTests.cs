using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HighwayRobberyWindowTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void CapturedEventCannotOfferItsPrintedInterrupt(bool eliminateOwner)
    {
        // "Facedown cards attached to in-play cards are out of play."
        // Highway Robbery: "Each player places a random card from their hand facedown here."
        var (world, captured, scheme) = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Assert.Single(runner.Waiting(world, Threat(world), WindowKind.Interrupt));

        runner.ExecuteReveal(world, scheme, 0);
        Assert.False(captured.FaceUp);
        Assert.Equal(scheme.ObjectId, captured.Area.Host);
        if (eliminateOwner)
        {
            Elimination.Eliminate(world, Cards, 1, "test", []);
            Assert.Equal(DeckType.RemovedArea, captured.Area.Type);
        }

        Assert.Empty(runner.Waiting(world, Threat(world), WindowKind.Interrupt));
        Assert.Null(Offering.Work(world, runner, Threat(world), WindowKind.Interrupt, []));
    }

    [Fact]
    [Rule("rr:event")]
    [Rule("rr:in-play-and-out-of-play.7")]
    public void ReturningCapturedEventToHandRestoresItsInterrupt()
    {
        // "Event cards and treachery cards implicitly resolve from an out-of-play area."
        // Highway Robbery: "Return each facedown card here to its owner's hand."
        var (world, captured, scheme) = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, scheme, 0);
        Agendas.Happening(world);
        Defeat.Scheme(world, Cards, scheme, "test", []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Same(world.Seats[1].Hand, captured.Area);
        captured.TurnFaceDown();

        var occurrence = Threat(world);
        var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(
            Offering.Work(world, runner, occurrence, WindowKind.Interrupt, []));
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        Assert.Equal(captured.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    public void AlreadyInitiatedReplacementCompletesWhenItsOwnerIsEliminated()
    {
        // "If a player is eliminated partway through the resolution of an ability,
        // resolve the entire ability." Great Responsibility replaces threat before
        // its damage eliminates Spider-Man; this is completion, not a new opportunity.
        var (world, responsibility, _) = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[1].IdentityCard.TakeDamage(9);
        var occurrence = Threat(world);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));

        var resolution = runner.ResolveWindow(world, occurrence, ability,
            Decision.Take(ability.Card));
        var events = resolution.Events.ToList();
        Sequence.Finish(world, Cards, runner, events);

        Assert.True(world.Seats[1].Eliminated);
        Assert.False(world.Seats[0].Eliminated);
        Assert.True(occurrence.Threat!.Replaced);
        Assert.Equal(0, occurrence.Threat.Remaining);
        Assert.Equal(DeckType.RemovedArea, responsibility.Area.Type);
        Assert.True(resolution.IsComplete);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, Card Event, Card Scheme) Board()
    {
        var world = new World(Cards, players: 2);
        foreach (string identity in new[] { "01010a", "01001a" })
        {
            var seat = world.CreateSeat(identity);
            seat.IdentityCard = world.CreateCard(identity, seat.Hero);
            world.CreateCard("01087", seat.Deck);
        }
        var captured = world.CreateCard("01061", world.Seats[1].Hand);
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var scheme = world.CreateCard("01166", world.AreaOf(DeckType.SideSchemesArea));
        return (world, captured, scheme);
    }

    private static Occurrence Threat(World world)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        return OccurrenceCapture.Threat(100, [Steps.ThreatWouldBePlaced], world, Cards,
            new ThreatPlacement(scheme.ObjectId, scheme.ObjectId, 1,
                ThreatCause.CardAbility, "test", player: 0));
    }
}
