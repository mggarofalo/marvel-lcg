using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingDiscardedFactsTests
{
    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("01005", 3, 1)]
    [InlineData("01088", 0, 1)]
    [InlineData("14006", 0, 1)]
    [Rule("rr:non-numerical-variable")]
    public void RandomDiscardRetainsExactPrintedCostAndType(string face, int cost, int types)
    {
        // Undefined X is "equal to 0" outside the play declaration that chooses its value.
        var runner = Runner("""{"discardAtRandom":{"player":"you","count":1}}""");
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        if (face.Length > 0) world.CreateCard(face, world.Seats[0].Hand);
        Execute(world, runner, source!);
        Assert.Equal(cost, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_cost"));
        Assert.Equal(types, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_types"));
        Assert.Empty(world.Seats[0].Hand.Cards);
    }

    [Fact]
    public void DeckReceiptCountsDistinctPrintedTypesAndDoesNotIncludeOlderDiscards()
    {
        var runner = Runner("""{"discardTop":{"from":"yourDeck","count":3}}""");
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, "01007");
            board.CreateCard("01002", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01005", board.Seats[0].Deck);
            board.CreateCard("01003", board.Seats[0].Deck);
            board.CreateCard("01007", board.Seats[0].Deck);
        }, abilities: runner);
        Execute(world, runner, source!);
        Assert.Equal(4, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_cost"));
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_types"));
    }

    [Fact]
    public void RandomReceiptUsesSelectedPhysicalCardAndCanAdmitFutureDamage()
    {
        var runner = Runner("""{"discardAtRandom":{"player":"you","count":1}}""", damage: true);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        string[] faces = ["01005", "01003", "01007"];
        var candidates = faces.Select(face => world.CreateCard(face, world.Seats[0].Hand)).ToArray();
        Execute(world, runner, source!);
        var discarded = Assert.Single(candidates, card => card.Area.Type == DeckType.DiscardPile);
        long expected = world.Facts.PrintedValue(discarded.FaceId, "Cost", world.Players);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_types"));
    }

    private static void Execute(World world, AbilityRunner runner, Card source)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
    }

    private static AbilityRunner Runner(string discard, bool damage = false) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01007","abilities":[{
          "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
          "effect":{"seq":[DISCARD,
            {"placeCounters":{"card":"you","counter":"cost","count":{"printedCostDiscarded":"game"}}},
            {"placeCounters":{"card":"you","counter":"types","count":{"distinctCardTypesDiscarded":"game"}}}]}
        }]}]}
        """.Replace("DISCARD", discard, StringComparison.Ordinal)
            .Replace("{\"placeCounters\":{\"card\":\"you\",\"counter\":\"cost\",\"count\":{\"printedCostDiscarded\":\"game\"}}}",
                damage ? "{\"dealDamage\":{\"cards\":\"you\",\"amount\":{\"printedCostDiscarded\":\"game\"}}}"
                    : "{\"placeCounters\":{\"card\":\"you\",\"counter\":\"cost\",\"count\":{\"printedCostDiscarded\":\"game\"}}}",
                StringComparison.Ordinal)));
}
