using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GlobalEncounterAttributionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("07004", false)]
    [InlineData("01006", true)]
    public void OnlyEncounterSourcesMayHaveNoResponsiblePlayer(string face, bool requiresPlayer)
    {
        // rr:ownership-and-control.2: player cards enter under their owner's control.
        // Global encounter effects belong to no player; a player effect must retain its actor.
        var world = Hero("01001a");
        var source = world.CreateCard(face, world.AreaOf(DeckType.AsideDeck));
        var query = new AbilityQueryContext(world, source, new Occurrence(1, []), -1,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty, [], "", -1, false, null);
        var context = new AbilityDamageAndThreatContext(expression, AbilityLowering.Book(AbilityCatalog.Parse("""{"cards":[]}""")),
            "test", [], null, null, null, false, null, null, 0, world.ThreatAbilities);
        if (requiresPlayer)
        {
            Assert.Throws<RulesNotImplementedException>(() => AbilityEffectAttribution.IdentityPlayer(context));
            Assert.Throws<RulesNotImplementedException>(() => AbilityEffectAttribution.ResponsiblePlayer(context));
        }
        else
        {
            Assert.Equal(-1, AbilityEffectAttribution.IdentityPlayer(context));
            Assert.Equal(-1, AbilityEffectAttribution.ResponsiblePlayer(context));
        }
    }
}
