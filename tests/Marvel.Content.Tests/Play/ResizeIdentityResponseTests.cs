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

// Test-local interpretations of printed Ant-Man faces and Resize; no pack admission.
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
        var strength = world.Cards.Single(c => c.FaceId == "12009");
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to, seat.IdentityCard.FaceId);
        Assert.Empty(seat.Hand.Cards);
        Assert.Equal(to == "12001c", prompt.Affordances.Any(a => a.AnchorId == strength.ObjectId));
        var helmetOffer = Assert.Single(prompt.Affordances, a => a.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(helmetOffer.Id), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to == "12001a" ? 1 : 0, seat.Hand.Cards.Count);
        Assert.Equal(to == "12001c" ? 1 : 3, seat.IdentityCard.Damage);
        if (to == "12001c")
        {
            var strengthOffer = Assert.Single(prompt.Affordances, a => a.AnchorId == strength.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(strengthOffer.Id), events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
            Assert.NotNull(prompt);
            Assert.Equal(4, CardValues.Evaluate(world, seat.IdentityCard, "attack", world.Facts, 1).CurrentValue);
        }
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(to == "12001a" ? 2 : 1, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, AbilityRunner Runner, Card Resize, Card Scheme, Card Villain) Board(string face, bool interrupt, bool upgrades = false)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var book = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},"effect":{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"draw":{"player":"you","count":1}}]}}]},
              {"card":"12001a","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"this"},"effect":{"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":1}}}}}]},
              {"card":"12001b","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"this"},"effect":{"heal":{"card":"you","amount":1}}}]},
              {"card":"12001c","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"this"},"effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"dealDamage":{"cards":"chosen","amount":1}}}}}]},
              {"card":"12008","abilities":[
                {"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"you","form":"hero"},"when":{"hasTrait":{"card":"you","trait":"GIANT"}},"effect":{"heal":{"card":"you","amount":2}}},
                {"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"you","form":"hero"},"when":{"hasTrait":{"card":"you","trait":"TINY"}},"effect":{"draw":{"player":"you","count":1}}}]},
              {"card":"12009","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"you","form":"hero"},"when":{"hasTrait":{"card":"you","trait":"GIANT"}},"effect":{"grantUntil":{"card":"you","keyword":"attack","amount":1,"until":"EndOfTurn"}}}]},
              {"card":"01092","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"ForcedInterrupt","subject":"game"},"cost":{"exhaust":"this"},"effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}}]}
            ]}
            """);
        var runner = new AbilityRunner(book);
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        var scheme = world.CreateCard("01116b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var villain = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 8; i++) world.CreateCard("01087", seat.Deck);
        var resize = world.CreateCard("12005", seat.Hand);
        if (interrupt) world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        if (upgrades)
        {
            world.CreateCard("12008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }
        world.Abilities = runner;
        return (world, runner, resize, scheme, villain);
    }
}
