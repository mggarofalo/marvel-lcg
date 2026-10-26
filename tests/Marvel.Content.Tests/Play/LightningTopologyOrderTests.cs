using System.Collections.Immutable;
using Marvel.Rules.Timing;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic topology ordering contracts; no Lightning Strike row is admitted.</summary>
public sealed class LightningTopologyOrderTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EncounterChronologyPreservesVillainFirstAndPhysicalMinionOrder(bool promote)
    {
        var world = Hero("01001a");
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var older = world.CreateCard("01101", area);
        var newer = world.CreateCard("01101", area);
        World.MoveToTop(older, area);
        var first = Group(world, "07002", "07003", "07004");
        var second = Group(world, "07017", "07018", "07019");
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], first.Villain));
        var context = Context(world, 0);
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var state = AbilityPowerStateFactory.Initial(scope);
        if (promote)
            state = AbilityPowerEncounterProjection.Advance(state, first.Villain, first.Stages[1], scope);
        var current = world.Cards[state.CurrentVillain];
        var trace = new AbilitySelectorTraceContext(current.ObjectId, scope, [], [], [], [])
        { EncounterStages = state.EncounterStages, EncounterOrder = state.EncounterOrder };
        AbilityCardSelection query = new AbilityCardSelection.Query(AbilityCardQuery.VillainAndMinionsEngagedWithYou);
        AbilityCardSelection[] selectors = [query,
            new AbilityCardSelection.WithTrait(query, "absent") { Exclude = true },
            new AbilityCardSelection.FirstAvailable([new AbilityCardSelection.Query(AbilityCardQuery.SupportsYouControl), query])];
        Card[] expected = [current, newer, older];
        foreach (var selector in selectors)
        {
            Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(selector, trace));
            Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(selector, scope, state));
            var ordered = new AbilityCardSelection.InObjectIdOrder(selector);
            Assert.Equal(expected.OrderBy(card => card.ObjectId), AbilitySelectorTrace.MatchingCandidates(ordered, trace));
            Assert.Equal(expected.OrderBy(card => card.ObjectId), AbilityPowerSelectionTrace.PowerEvery(ordered, scope, state));
            var last = new AbilityCardSelection.Last(selector);
            Assert.Same(older, AbilitySingularTraceSelection.Find(last, trace));
            Assert.Same(older, AbilityPowerSelectionTrace.PowerFind(last, scope, state));
        }
        var enemies = new AbilityCardSelection.Query(AbilityCardQuery.Enemies);
        Card[] chronology = [newer, older, .. state.EncounterOrder!.Select(id => world.Cards[id])];
        Assert.Equal(chronology, AbilitySelectorTrace.MatchingCandidates(enemies, trace));
        Assert.Equal(chronology, AbilityPowerSelectionTrace.PowerEvery(enemies, scope, state));
        var fallback = new AbilityCardSelection.FirstAvailable([enemies, query]);
        Assert.Equal(chronology, AbilitySelectorTrace.MatchingCandidates(fallback, trace));
        Assert.Equal(chronology, AbilityPowerSelectionTrace.PowerEvery(fallback, scope, state));
    }

    private static AbilityAdmissionContext Context(World world, int player)
    {
        var source = world.Seats[player].IdentityCard;
        var query = new AbilityQueryContext(world, source, new Occurrence(player, []), player,
            source.Incarnation, null, null, null, []);
        return new(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")), world.ResourceAbilities,
            new(query, ImmutableDictionary<string, long>.Empty, [], "", 0, false, null), new(), null);
    }

    private static EncounterDeckGroup Group(World world, string face, string successor, string scheme)
    {
        var villain = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard(successor, world.AreaOf(DeckType.VillainDeck));
        return new(villain, world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile), [], [villain, next]);
    }
}
