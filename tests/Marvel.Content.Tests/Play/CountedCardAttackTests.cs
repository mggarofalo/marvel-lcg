using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CountedCardAttackTests
{
    [Rule("rr:choose-game-element.3")]
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:guard.1")]
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(2, true)]
    public void CountedAttackChoosesAvailableDistinctEnemiesTogether(int count, bool guard)
    {
        // "Simultaneously choose as many as are available, to a maximum of the
        // specified number." Guard: "The engaged player cannot attack any villain."
        string effect = Effect(count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var runner = Runner("01004", "Action", effect);
        Card? source = null;
        Card? payment = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            minion = board.CreateCard(guard ? "01101" : "01102",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paid = [payment!.ObjectId];
        game.Resolve(new Decision(action.Id, [], paid,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paid)));
        var prompt = Assert.IsType<Prompt>(game.Pending);
        var choice = Assert.Single(prompt.Affordances);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int expectedCount = Math.Min(count, guard ? 1 : 2);
        int[] selected = expectedCount == 1 ? [minion!.ObjectId] : [minion!.ObjectId, villain.ObjectId];
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(choice.Id, [], [])));
        if (expectedCount == 2)
            Assert.Throws<ArgumentException>(() =>
                game.Resolve(new Decision(choice.Id, [minion!.ObjectId, minion.ObjectId], [])));
        if (guard)
            Assert.Throws<ArgumentException>(() =>
                game.Resolve(new Decision(choice.Id, [villain.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        game.Resolve(new Decision(choice.Id, selected, []));
        Assert.Equal(1, minion.Damage);
        Assert.Equal(expectedCount - 1, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:stun-stunned.5")]
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(2, true)]
    public void PhysicalDiscardReceiptBindsTargetCountAndStunPaysBeforeCancelling(int discarded, bool stunned)
    {
        // Costs "must be paid simultaneously"; stun replaces the attack after
        // its costs are paid. Resource cards paying the event are not discards
        // made to pay its separate printed hand-discard cost.
        var runner = Runner("01004", "Action", Effect("""{"cardsDiscarded":"game"}"""),
            cost: """{"discardAnyFromHand":"yourHand"}""", labels: """["attack"]""");
        Card? source = null;
        Card? minion = null;
        Card[] held = [];
        var (game, world) = Playing(board =>
        {
            Hand(board, "01088", 0);
            source = board.CreateCard("01004", board.Seats[0].Hand);
            held = [.. Enumerable.Range(0, 5).Select(_ => board.CreateCard("01088", board.Seats[0].Hand))];
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (stunned) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paid = [held[0].ObjectId];
        game.Resolve(new Decision(action.Id, [.. held.Skip(1).Take(discarded).Select(card => card.ObjectId)],
            paid, Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paid)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (!stunned)
        {
            int[] targets = discarded == 1 ? [minion!.ObjectId] : [minion!.ObjectId, villain.ObjectId];
            game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, targets, []));
        }
        Assert.Equal(stunned ? 0 : 1, minion!.Damage);
        Assert.Equal(!stunned && discarded > 1 ? 1 : 0, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.All(held.Take(discarded + 1), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.False(Statuses.Afflicted(world, world.Facts, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Theory]
    [InlineData(",\"simultaneous\":1", "")]
    [InlineData("\"powerTargets\"", "\"enemies\"")]
    [InlineData("\"amount\":1", "\"amount\":{\"cardsDiscarded\":\"game\"}")]
    public void CountedAttackRejectsUnsupportedBodyShapes(string original, string replacement)
    {
        var error = Assert.Throws<AbilityException>(() =>
            Runner("01004", "Action", Effect("2").Replace(original, replacement, StringComparison.Ordinal)));
        Assert.Contains("counted attack requires", error.Message);
    }

    [Fact]
    public void UnsupportedDynamicCountRaisesDuringLowering()
    {
        var error = Assert.Throws<AbilityException>(() =>
            Runner("01004", "Action", Effect("""{"printedResourceIconsDiscarded":"game"}""")));
        Assert.Contains("constant or physical discard-receipt count", error.Message);
    }

    [Rule("rr:target.2")]
    [Fact]
    public void ZeroCountOffersNoAttackAndRepeatedQueriesLeaveStateUnchanged()
    {
        // An ability can only initiate if it has "at least one valid target".
        var runner = Runner("01004", "Action", Effect("0"));
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var step = world.Agenda.Current;
        for (int query = 0; query < 3; query++)
            Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(step, world.Agenda.Current);
    }

    private static string Effect(string count) => """
        {"attack":{"target":{"query":"attackableEnemies"},"count":COUNT,"effect":{
          "dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}
        }}}
        """.Replace("COUNT", count, StringComparison.Ordinal);
}
