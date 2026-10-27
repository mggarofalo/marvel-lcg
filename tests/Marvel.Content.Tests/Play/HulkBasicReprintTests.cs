using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkBasicReprintTests
{
    [Fact]
    public void ExactlyFiveBasicFacesAppendWithoutCountersOrOtherHulkAdmission()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] faces = ["10020", "10021", "10022", "10023", "10024"];
        Assert.Equal(716, rows.Length);
        Assert.Equal(faces, rows.Skip(688).Take(5).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(688).Take(5), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(new List<string> { "10001a", "10001b", "10004", "10005" }.Concat(faces).Concat(Enumerable.Range(10011, 9).Where(id => id != 10015).Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture))).Concat(["10002", "10003", "10006", "10007", "10008", "10009", "10010", "10030", "10031", "10032", "10025", "10026", "10027", "10028"]).Order(),
            AuthoredCards.Book.Authored.Where(face => face.StartsWith("10", StringComparison.Ordinal)).Order());
        Assert.DoesNotContain("10015", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData("10020", "YY")]
    [InlineData("10021", "BB")]
    [InlineData("10022", "RR")]
    [Rule("rr:resource.1")]
    public void OnePhysicalResourceCardPaysTwoResources(string face, string icons)
    {
        // A player discards hand cards to generate "the resource or resources indicated".
        var (_, world) = Playing(_ => { }, hero: true);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("01032", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        int held = world.Seats[0].Hand.Cards.Count;
        Assert.Equal(icons, runner.ResourcesGeneratedBy(world, payment, source));
        Assert.Equal("1", world.Facts.Attributes(face)["MaxPerDeck"]);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Equal(icons, Assert.Single(price.Generators, item => item.Effect == payment.ObjectId).Generates);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [world.TheCardIn(DeckType.VillainArea)!.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0), payment.Area);
        Assert.Single(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards, card => card.ObjectId == payment.ObjectId);
        Assert.Equal(held - 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void NonFirstMansionControllerChoosesExactlyOnePlayersDraw(int recipient, bool alterEgo)
    {
        Card source = null!;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("10023", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
            board.Seats[1].IdentityCard.TurnTo(alterEgo ? "01010b" : "01010a");
        }, heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var top = world.Seats[recipient].Deck.Cards[^1];
        int[] held = world.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Activate(world, runner, source, recipient);
        Assert.Same(world.Seats[recipient].Hand, top.Area);
        for (int player = 0; player < 2; player++)
            Assert.Equal(held[player] + (player == recipient ? 1 : 0), world.Seats[player].Hand.Cards.Count);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == source.ObjectId);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void HelicarrierDiscountSurvivesQueriesAndConsumesOnlyOnSuccessfulPlay(int recipient, bool rejectPayment)
    {
        var (_, prepared) = Playing(_ => { });
        var world = new World(prepared.Facts, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001a");
            for (int card = 0; card < 5; card++) world.CreateCard("01003", seat.Deck);
        }
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10024", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
        world.Abilities = runner;
        var eventCard = world.CreateCard("01070", world.Seats[recipient].Hand);
        var payment = world.CreateCard("01003", world.Seats[recipient].Hand);
        var next = world.CreateCard("01083", world.Seats[recipient].Hand);
        var other = world.CreateCard("01083", world.Seats[1 - recipient].Hand);
        Activate(world, runner, source, recipient);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        var action = Assert.Single(runner.Actions(world, recipient), item => item.Card == eventCard.ObjectId);
        for (int query = 0; query < 2; query++)
        {
            Assert.Equal("1", Assert.Single(runner.Describe(world, action).CostOptions).Cost);
            Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[recipient], next).Amount);
            Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[1 - recipient], other).Amount);
        }
        if (rejectPayment)
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal("1", Assert.Single(runner.Describe(world, action).CostOptions).Cost);
        }
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [world.Seats[recipient].IdentityCard.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(recipient, payment.Area.CardOwner);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[recipient], next).Amount);
    }

    [Theory]
    [InlineData("10023", "01091")]
    [InlineData("10024", "01092")]
    [Rule("rr:max-maximum.3")]
    public void PerPlayerControlLimitIncludesCoreCopiesButAllowsAnotherPlayersCopy(string face, string core)
    {
        // "Max 1 per player" restricts copies "each player may control in play".
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var candidate = world.CreateCard(face, world.Seats[1].Hand);
        world.CreateCard(core, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.True(CardPlay.WithinPerPlayerLimit(world, world.Facts, world.Seats[1], candidate, [], runner));
        var existing = world.CreateCard(core, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
        Assert.False(CardPlay.WithinPerPlayerLimit(world, world.Facts, world.Seats[1], candidate, [], runner));
        World.MoveToTop(existing, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.True(CardPlay.WithinPerPlayerLimit(world, world.Facts, world.Seats[1], candidate, [], runner));
    }

    [Fact]
    public void UnusedHelicarrierReductionExpiresAtThePlayerPhaseBoundary()
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10024", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var next = world.CreateCard("01083", world.Seats[0].Hand);
        Activate(world, runner, source, 0);
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
        world.Effects.Expire(TimingPoints.EndOfPlayerPhase);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
    }

    private static void Activate(World world, AbilityRunner runner, Card source, int recipient)
    {
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(world.Seats[recipient].IdentityCard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
    }
}
