using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DeckArrowCostTests
{
    [Fact]
    public void CombinedDiscardCostsCannotReuseTheSameAvailableCards()
    {
        var runner = Runner("01006", "Action", """{"ready":"you"}""",
            cost: """{"seq":[{"discardTop":{"from":"yourDeck","count":5}},{"discardTop":{"from":"yourDeck","count":1}}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        world.Seats[0].IdentityCard.Exhaust();
        ClearDeck(world, world.Seats[0].Deck);
        for (int index = 0; index < 5; index++) world.CreateCard("01088", world.Seats[0].Deck);
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [Rule("rr:cost-arrow-icon")]
    public void CompleteDeckDiscardIsRequiredBeforeSpendingAnyResources(int available, bool offered)
    {
        // Text before the arrow "must be paid and/or resolved in full".
        var runner = Runner("01006", "Action", """{"ready":"you"}""",
            cost: """{"seq":[{"spend":"R"},{"discardTop":{"from":"yourDeck","count":5}}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        world.Seats[0].IdentityCard.Exhaust();
        ClearDeck(world, world.Seats[0].Deck);
        for (int index = 0; index < available; index++) world.CreateCard("01088", world.Seats[0].Deck);
        var resource = world.CreateCard("01090", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var actions = runner.Actions(world, 0).Where(option => option.Card == source.ObjectId).ToArray();
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(offered ? 1 : 0, actions.Length);
        if (!offered) return;
        runner.ExecuteAction(world, new PlayerAction(actions[0], [resource.ObjectId], []));
        // Paying the last discard resets the player deck immediately and deals
        // the normal facedown encounter card; the paid resource is reshuffled too.
        Assert.Equal(6, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.PlayerDeck, resource.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    public void EncounterDiscardCostSuppliesItsPrintedBoostReceipt()
    {
        var runner = Runner("01006", "Action",
            """{"placeCounters":{"card":"you","counter":"observed","count":{"boostIconsDiscarded":"game"}}}""",
            cost: """{"discardTop":{"from":"encounterDeck","count":1}}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        ClearDeck(world, world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var discarded = world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(discarded, discarded.Area);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens["c_observed"]);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
    }

    [Fact]
    public void EncounterDealCostIsFacedownAndPrecedesTheEffect()
    {
        var runner = Runner("01006", "Action", """{"ready":"you"}""", cost: """{"dealEncounterCard":"you"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        world.Seats[0].IdentityCard.Exhaust();
        var top = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Same(top, Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards));
        Assert.False(top.FaceUp);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    private static void ClearDeck(World world, Area deck)
    {
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
    }
}
