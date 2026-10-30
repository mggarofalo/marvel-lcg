using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic authored permission exercises 05018 without admitting its pack.</summary>
public sealed class DiscardPlayPermissionTests
{
    private static AbilityRunner Permission() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05018","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},
          "effect":{"allowPlayFromDiscard":"this"}
        }]}]}
        """));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:play-restrictions-and-permissions.2")]
    public void OwnDiscardOffersOrdinaryPrintedCostAndOnlyHandResourcesOnOwnTurn(bool discount)
    {
        // Printed 05018: "from your discard pile during your turn (paying his resource cost)".
        Card ally = null!;
        var runner = Permission();
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", Pile(board, 1));
            for (int i = 0; i < 4; i++) board.CreateCard("01093", board.Seats[1].Hand);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == ally.ObjectId);
        if (discount) CardPayment.ReduceNextCardCost(world, world.Seats[1].IdentityCard, 1, 1);
        game.Resolve(Decision.Decline);
        var offer = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally.ObjectId);
        Assert.Equal(CardPlay.Verb, offer.Verb);
        Assert.Equal(1, offer.AnchorPlayer);
        Assert.Equal([world.Seats[1].IdentityCard.ObjectId], offer.Targets!.Legal);
        var cost = Assert.Single(offer.Costs!);
        Assert.Equal(discount ? "3" : "4", cost.Cost);
        Assert.DoesNotContain(cost.Sources!, source => source.Effect == ally.ObjectId);
        Assert.All(cost.Sources!, source => Assert.Contains(world.Seats[1].Hand.Cards,
            card => card.ObjectId == source.Effect));
        Assert.Empty(runner.Actions(world, 1));
        int[] paying = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01093")
            .Take(discount ? 3 : 4).Select(card => card.ObjectId).ToArray();
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[1], ally, paying, events);
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.CardPlayed);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Same(ally, Assert.Single(world.Cards, card => card.ObjectId == ally.ObjectId));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.All(paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Theory]
    [InlineData("deck")]
    [InlineData("foreign-discard")]
    [InlineData("missing-permission")]
    [InlineData("unique")]
    [InlineData("self-payment")]
    public void RejectedPlayPreservesCardsRandomPendingWorkAndDiscount(string invalid)
    {
        var runner = Permission();
        var (game, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var ally = world.CreateCard("05018", Pile(world, 1));
        var payments = Enumerable.Range(0, 4).Select(_ => world.CreateCard("01093", world.Seats[1].Hand).ObjectId).ToArray();
        CardPayment.ReduceNextCardCost(world, world.Seats[1].IdentityCard, 1, 1);
        if (invalid == "deck") World.MoveToTop(ally, world.Seats[1].Deck);
        if (invalid == "foreign-discard") World.MoveToTop(ally, Pile(world, 0));
        if (invalid == "missing-permission") world.Abilities = new NoCardAbilities();
        if (invalid == "unique") world.CreateCard("05018", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (invalid == "self-payment") payments = [ally.ObjectId, ..payments.Take(2)];
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var prompt = game.Pending;
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts,
            world.CardPlayAbilities, world.Seats[1], ally, payments, events));
        Assert.Empty(events);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Same(prompt, game.Pending);
    }

    [Fact]
    public void RepeatedPricingQueriesPreserveSubsequentOrdinaryPlay()
    {
        var runner = Permission();
        var (_, control) = Playing(_ => { }, abilities: runner);
        var (_, probed) = Playing(_ => { }, abilities: runner);
        var expected = control.CreateCard("05018", Pile(control, 0));
        var actual = probed.CreateCard("05018", Pile(probed, 0));
        var before = probed.Digest().Canonical();
        var random = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++) Assert.NotNull(CardPayment.Price(probed, probed.Facts, probed.Seats[0], actual));
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(random.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        var expectedEvents = Pay(control, runner, expected);
        var actualEvents = Pay(probed, runner, actual);
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(expectedEvents.Select(item => item.GetType()), actualEvents.Select(item => item.GetType()));
        Assert.Equal(control.Random.Generator.WordsConsumed, probed.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("when")]
    [InlineData("form")]
    [InlineData("action")]
    [InlineData("nested")]
    [InlineData("wrong-marker")]
    public void DiscardPermissionRejectsUnsupportedAuthoredShapes(string variant)
    {
        string trigger = variant == "action"
            ? "{\"timing\":\"Action\",\"event\":\"WhenActionTriggered\",\"subject\":\"this\"}"
            : variant == "form" ? "{\"timing\":\"Constant\",\"form\":\"hero\",\"subject\":\"this\"}"
            : "{\"timing\":\"Constant\",\"subject\":\"this\"}";
        string effect = variant == "wrong-marker" ? "{\"allowPlayFromDiscard\":\"yourHand\"}"
            : "{\"allowPlayFromDiscard\":\"this\"}";
        if (variant == "nested") effect = "{\"seq\":[" + effect + "]}";
        string when = variant == "when" ? ",\"when\":{\"exists\":\"yourHero\"}" : "";
        string json = "{\"cards\":[{\"card\":\"05018\",\"abilities\":[{\"trigger\":" + trigger
            + ",\"effect\":" + effect + when + "}]}]}";
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void OrdinaryDiscardPlaySchedulesAllyLimitBeforeEntryResponses()
    {
        // rr:ally-limit: immediately choose and discard allies before resolving entry abilities.
        var runner = Permission();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        foreach (string face in new[] { "01067", "01068", "01041" }) world.CreateCard(face, area);
        var ally = world.CreateCard("05018", Pile(world, 0));
        Pay(world, runner, ally);
        Assert.NotNull(Sequence.Work(world, world.Facts, runner, []));
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        int limit = Array.FindIndex(pending, step => step.What == Steps.ChooseAllyForLimit);
        int response = Array.FindIndex(pending, step => step.What == Steps.CardPlayed);
        Assert.True(limit >= 0 && response > limit);
        Assert.Equal(0, pending[limit].Seat);
        Assert.Equal(ally.ObjectId, pending[response].Subject);
    }

    private static List<GameEvent> Pay(World world, AbilityRunner runner, Card ally)
    {
        int[] resources = Enumerable.Range(0, 4).Select(_ => world.CreateCard("01093", world.Seats[0].Hand).ObjectId).ToArray();
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally, resources, events);
        return events;
    }

    private static Area Pile(World world, int player) => world.AreaOf(
        DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
}
