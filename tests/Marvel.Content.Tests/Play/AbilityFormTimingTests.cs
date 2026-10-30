using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityFormTimingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.2")]
    [Rule("rr:form-change-form.3")]
    public void PublishedSplitPersonalityPaysThenChangesThenResolvesTaskmasterBeforeDrawing(bool spent)
    {
        // rr:initiating-abilities.2: a response initiates "immediately after"
        // its condition becomes true; 01025 draws after its form change.
        // 04093: "After a player changes to hero form" discard and deal boost damage.
        var (world, split, payment, boost) = Board();
        var seat = world.Seats[0];
        seat.FormChangedInRound = spent ? 1 : 0;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == split.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        Assert.Equal("01019b", seat.IdentityCard.FaceId);
        Assert.All(payment, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        Assert.Equal(8, seat.Deck.Cards.Count);

        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt); // She-Hulk's optional response follows Taskmaster's forced response.
        Assert.Equal("01019a", seat.IdentityCard.FaceId);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(8, seat.Deck.Cards.Count);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(4, seat.Hand.Cards.Count);
        Assert.Equal(4, seat.Deck.Cards.Count);
        Assert.Equal(spent ? 1 : 0, seat.FormChangedInRound);
        Assert.False(world.Agenda.IsBusy);
        Assert.Single(events.OfType<CardsFlipped>());
    }

    [Fact]
    [Rule("rr:interrupt.3")]
    [Rule("rr:initiating-abilities.2")]
    public void InterruptObservesOldFaceAndResponsesFinishBeforeTheSuffix()
    {
        // Synthetic observer supplements actual 01025/04093 without replacing their rows.
        // rr:interrupt.3: "before that triggering condition resolves".
        var observer = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenFormChanged","timing":"Interrupt","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var runner = new AbilityRunner(new AbilityBook(
            [..AuthoredCards.Book.Abilities, ..observer.Abilities],
            AuthoredCards.Book.Authored.Concat(observer.Authored).ToHashSet(StringComparer.Ordinal),
            AuthoredCards.Book.AttachTo));
        var (world, split, payment, boost) = Board();
        world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == split.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal("01019b", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDeck, boost.Area.Type);
        Assert.Equal(8, world.Seats[0].Deck.Cards.Count);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal("01019a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(8, world.Seats[0].Deck.Cards.Count);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(4, world.Seats[0].Hand.Cards.Count);
    }

    private static (World World, Card Split, int[] Payment, Card Boost) Board()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01019a,01019b", seat.Hero);
        seat.IdentityCard.TurnTo("01019b");
        world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var split = world.CreateCard("01025", seat.Hand);
        int[] payment = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01087", seat.Hand).ObjectId).ToArray();
        for (int i = 0; i < 8; i++) world.CreateCard("01087", seat.Deck);
        return (world, split, payment, boost);
    }
}
