using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingEngineReviewPileDriveTests
{
    [Fact]
    public void FirstPlayerChoosesTiedHighestCostForBothPlayersBeforeReset()
    {
        // 07034: each player discards the upgrade or support they control with the highest cost.
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, players: 2);
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        world.Abilities = runner;
        foreach (var seat in world.Seats) world.CreateCard("01088", seat.Deck);
        var first = world.CreateCard("01056", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        first.PlaceTokens("attack", 3);
        var second = world.CreateCard("01092", first.Area);
        var cheaper = world.CreateCard("01057", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var otherTie = world.CreateCard("01056", other.Area);
        otherTie.PlaceTokens("attack", 3);
        groups[2].Scheme.PlaceTokens("k_threat", 12);
        Threat.Schedule(world, new([groups[2].Scheme], null, 1, ThreatCause.CardAbility, "test", 0));
        var order = Sequence.Work(world, world.Facts, runner, [])!;
        var orderOffer = Assert.Single(order.Affordances);
        Sequence.Answer(world, world.Facts, runner, order, new(orderOffer.Id, [0, 1]), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(0, choice.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }.Order(), choice.Affordances.Select(item => item.Id).Order());
        Assert.Equal(13, groups[2].Scheme.Tokens["k_threat"]);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(second.ObjectId), []);
        choice = Sequence.Work(world, world.Facts, runner, [])!;
        // rr:first-player.1: "the first player selects among the eligible options."
        Assert.Equal(world.FirstPlayer, choice.Player);
        Assert.Equal(new[] { other.ObjectId, otherTie.ObjectId }.Order(),
            choice.Affordances.Select(item => item.Id).Order());
        Assert.Equal(13, groups[2].Scheme.Tokens["k_threat"]);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(other.ObjectId), []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, groups[2].Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(DeckType.DiscardPile, other.Area.Type);
        Assert.Equal(DeckType.SupportsArea, first.Area.Type);
        Assert.Equal(DeckType.SupportsArea, otherTie.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, cheaper.Area.Type);
    }

}
