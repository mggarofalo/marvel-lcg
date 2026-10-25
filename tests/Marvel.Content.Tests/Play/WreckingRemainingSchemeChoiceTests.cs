using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingRemainingSchemeChoiceTests
{
    [Fact]
    public void PileDriveOffersOnlyEachPlayersTiedHighestCostAndWaitsToReset()
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
        // rr:first-player.1 assigns selection when there are "multiple eligible targets".
        // The other player's sole eligible card resolves without another choice.
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(3, groups[2].Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(DeckType.DiscardPile, other.Area.Type);
        Assert.Equal(DeckType.SupportsArea, first.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, cheaper.Area.Type);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void PileDriveContinuesPastPlayersWithoutAnEligiblePermanent(int owner)
    {
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, players: 2);
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        world.Abilities = runner;
        Card? permanent = null;
        if (owner >= 0)
        {
            world.CreateCard("01088", world.Seats[owner].Deck);
            permanent = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(owner), cardOwner: owner));
        }
        groups[2].Scheme.PlaceTokens("k_threat", 11);
        Threat.Schedule(world, new([groups[2].Scheme], null, 1, ThreatCause.CardAbility, "test", 0));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, groups[2].Scheme.Tokens["k_threat"]);
        Assert.Equal(Outcome.Unfinished, world.Result);
        if (permanent is not null) Assert.Equal(DeckType.DiscardPile, permanent.Area.Type);
    }

    [Fact]
    public void HardHitterToughPreventionStillAllowsIndependentReset()
    {
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0);
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        world.Abilities = runner;
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        groups[0].Scheme.PlaceTokens("k_threat", 9);
        Threat.Schedule(world, new([groups[0].Scheme], null, 1, ThreatCause.CardAbility, "test", 0));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(3, groups[0].Scheme.Tokens["k_threat"]);
    }
}
