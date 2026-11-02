using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Astral Projection (09003) offers Total Destruction (10027) only while Abomination (10026) is absent.</summary>
public sealed class AstralProjectionTotalDestructionTests
{
    // Total Destruction: "Threat cannot be removed from this scheme while Abomination is in play."
    // Astral Projection: "Choose a scheme → remove 3 threat from that scheme ..."
    [Rule("rr:cannot")]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AstralProjectionOffersTotalDestructionOnlyWithoutAbomination(bool present)
    {
        // rr:cannot: "The word 'cannot' is absolute and cannot be countermanded by other
        // abilities or effects." rr:in-play-and-out-of-play.4: "Card abilities only
        // interact with, and can only target, cards that are in play".
        var (world, runner, scheme) = Board(AuthoredCards.Runner());
        if (present) world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 3);
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        world.CreateCard("01090", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        int[] expected = present ? [main.ObjectId] : [main.ObjectId, scheme.ObjectId];
        Assert.Equal(expected, runner.Describe(world, action).Targets!.Legal.Order());
        Assert.Equal(before, world.Digest().Canonical());
    }

    private static (World World, AbilityRunner Runner, Card Scheme) Board(AbilityRunner runner)
    {
        var (world, _) = HulkNemesisFixture.Create();
        world.Abilities = runner;
        Forms.Change(world.Seats[0], world.Facts);
        var scheme = HulkNemesisFixture.Dealt(world, "10027");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        return (world, runner, scheme);
    }
}
