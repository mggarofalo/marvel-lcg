using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RoninUpgradeSelectorReviewTests
{
    [Fact]
    [Rule("rr:upgrade.3")]
    public void EffectiveUpgradeKindAndSourceDepartureAgreeAcrossLiveAndProjection()
    {
        // rr:upgrade.3: some upgrades "attach to" another card.
        // The effective profile below is synthetic; it tests the query contract.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var source = world.CreateCard("12013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var hosted = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), source.ObjectId);
        var first = world.CreateCard("01074", hosted);
        var second = world.CreateCard("01067", hosted);
        second.AssignProfile(new EffectiveCardProfile("synthetic", "Synthetic", CardKind.Upgrade,
            [], ImmutableDictionary<string, long>.Empty));
        var query = new AbilityQueryContext(world, source, new Occurrence(1, []), 0,
            source.Incarnation, null, null, null, []);
        var admission = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("""{"cards":[]}""")),
            world.ResourceAbilities, new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
                [], "", 0, false, null), new(), null);
        var selection = new AbilityCardSelection.Query(AbilityCardQuery.UpgradesOnThis);
        var projected = new AreaProjectionState(admission);

        Assert.Equal([first, second], new AbilitySelectorEvaluation(query).Every(selection));
        Assert.Equal([first, second], AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        projected.Departed.Add(source.ObjectId);
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        Assert.Equal([first, second], new AbilitySelectorEvaluation(query).Every(selection));
        projected.Departed.Remove(source.ObjectId);
        World.MoveToTop(first, seat.Hand);
        World.MoveToTop(first, hosted);
        var last = new AbilityCardSelection.Last(selection);
        Assert.Equal([second], new AbilitySelectorEvaluation(query).Every(last));
        Assert.Equal([second], AbilityProjectedStateQueries.ProjectedEvery(last, projected, admission));
        Assert.Equal([first, second], new AbilitySelectorEvaluation(query).Every(selection));
        Assert.Equal([first, second], AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));

        // Projected host changes retain the same id order even when the lower id enters last.
        var other = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        projected.Hosts[first.ObjectId] = other.ObjectId;
        Assert.Equal([second], AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        World.MoveToTop(first, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), other.ObjectId));
        Assert.Equal(new AbilitySelectorEvaluation(query).Every(selection),
            AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        projected.Hosts[first.ObjectId] = source.ObjectId;
        Assert.Equal([first, second], AbilityProjectedStateQueries.ProjectedEvery(selection, projected.Clone(), admission));
        Assert.Equal([second], AbilityProjectedStateQueries.ProjectedEvery(last, projected, admission));
        World.MoveToTop(first, hosted);
        Assert.Equal(new AbilitySelectorEvaluation(query).Every(selection),
            AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
    }
}
