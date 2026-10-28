using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Ant-Man rows on a synthetic board; no pack admission.
public sealed class ResizeIdentityResponseTests
{
    [Theory]
    [InlineData("12001c", "12001a")]
    [InlineData("12001a", "12001c")]
    [Rule("rr:initiating-abilities.2")]
    public void PrintedDestinationResponseResolvesBeforeResizeDraw(string from, string to)
    {
        // rr:initiating-abilities.2: responses initiate "immediately after" their condition.
        var (world, runner, resize, scheme, villain) = Board(from, false);
        var seat = world.Seats[0];
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to, seat.IdentityCard.FaceId);
        Assert.Empty(seat.Hand.Cards);
        Assert.Equal(8, seat.Deck.Cards.Count);
        var response = Assert.Single(prompt.Affordances, a => a.AnchorId == seat.IdentityCard.ObjectId);
        var target = to == "12001a" ? scheme : villain;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id, [target.ObjectId], []), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(to == "12001a" ? 1 : 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(to == "12001c" ? 1 : 0, villain.Damage);
        Assert.Single(seat.Hand.Cards);
        Assert.Equal(7, seat.Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.Single(events.OfType<CardsFlipped>());
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:interrupt.3")]
    public void NestedInterruptReachesCapturedDestinationAndResizeStillDraws()
    {
        // rr:interrupt.3: an interrupt resolves "before that triggering condition resolves".
        var (world, runner, resize, _, _) = Board("12001a", true);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal("12001c", world.Seats[0].IdentityCard.FaceId);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal("12001c", world.Seats[0].IdentityCard.FaceId);
        Assert.Single(events.OfType<CardsFlipped>());
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001c", "12001a")]
    [InlineData("12001a", "12001c")]
    public void PrintedUpgradeResponsesResolveBeforeTheIndependentDraw(string from, string to)
    {
        var (world, runner, resize, _, _) = Board(from, false, upgrades: true);
        var seat = world.Seats[0];
        seat.IdentityCard.TakeDamage(3);
        var helmet = world.Cards.Single(c => c.FaceId == "12008");
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to, seat.IdentityCard.FaceId);
        Assert.Empty(seat.Hand.Cards);
        var helmetOffer = Assert.Single(prompt.Affordances, a => a.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(helmetOffer.Id), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to == "12001a" ? 1 : 0, seat.Hand.Cards.Count);
        Assert.Equal(to == "12001c" ? 1 : 3, seat.IdentityCard.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(to == "12001a" ? 2 : 1, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, AbilityRunner Runner, Card Resize, Card Scheme, Card Villain) Board(string face, bool interrupt, bool upgrades = false)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AntManF1Fixture.Runner(["12001a", "12001b", "12001c", "12005", "12008"],
            """{"card":"01092","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"ForcedInterrupt","subject":"game"},"cost":{"exhaust":"this"},"effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}}]}""");
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        var scheme = world.CreateCard("01116b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var villain = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 8; i++) world.CreateCard("01087", seat.Deck);
        if (upgrades)
        {
            // Prepare this upgrade-response journey during an actual player turn.
            var game = Game.Begin(world, cards, runner);
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                a => a.Verb == Game.ResolveMulligans).Id));
            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
            foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        }
        var resize = world.CreateCard("12005", seat.Hand);
        if (interrupt) world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        if (upgrades)
        {
            world.CreateCard("12008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }
        world.Abilities = runner;
        return (world, runner, resize, scheme, villain);
    }
}
