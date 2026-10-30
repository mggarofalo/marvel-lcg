using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterDiscardReceiptTests
{
    [Theory]
    [InlineData("01104", 0)]
    [InlineData("01111", 2)]
    [Rule("rr:target.3")]
    public void HiddenCostReceiptCanHaveAnEffectWithoutInspectingTheTopCard(string top, long expected)
    {
        // A target is valid if any part of the ability can affect it; the hidden cost has not resolved yet.
        var runner = Runner("01006", "Action",
            """{"placeCounters":{"card":"you","counter":"observed","count":{"printedBoostIconsDiscarded":"game"}}}""",
            cost: """{"discardTop":{"from":"encounterDeck","count":1}}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        world.CreateCard("01187", deck);
        var discarded = world.CreateCard(top, deck);
        World.MoveToTop(discarded, deck);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(expected, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
    }

    [Theory]
    [InlineData("min")]
    [InlineData("mul")]
    [Rule("rr:target.3")]
    public void AFixedZeroBoundCannotGainPotentialFromAnUnknownReceipt(string operation)
    {
        // No value of the hidden receipt can make these zero amounts affect the target.
        string effect = """{"placeCounters":{"card":"you","counter":"observed","count":{"OP":[0,{"printedBoostIconsDiscarded":"game"}]}}}"""
            .Replace("OP", operation, StringComparison.Ordinal);
        var runner = Runner("01006", "Action", effect,
            cost: """{"discardTop":{"from":"encounterDeck","count":1}}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
    }
}
