using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SideSchemeDepartureTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SleeperPresencePreventsDiscardRemovalAndDefeatWithoutInventingDefeatEvents(bool present)
    {
        // ruling:25a8a46bc9925963: a side scheme that cannot leave play cannot be defeated.
        // The Sleeper Awakened04140 supplies the conditional printed prohibition.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04140","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"if":{"test":{"exists":{"titled":"The Sleeper"}},
                "then":{"preventSideSchemeDeparture":"this"}}}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var scheme = world.CreateCard("04140", world.AreaOf(DeckType.SideSchemesArea));
        var sleeper = world.CreateCard("04130", world.AreaOf(present ? DeckType.EngagedEnemiesArea : DeckType.AsideDeck, PlayArea.Of(0)));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(!present, Discard.EffectCanRemove(world, Cards, source, scheme));
        var events = new List<GameEvent>();
        var occurrence = new Occurrence(1, []);
        Defeat.Scheme(world, Cards, scheme, "test", events, recordOn: occurrence);
        Assert.Equal(present ? DeckType.SideSchemesArea : DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(present ? 0 : 1, occurrence.Defeats.Count);
        if (!present) return;
        Assert.Empty(events);
        Discard.Card(world, scheme, "test", events);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Empty(events);
        Tuck.Card(world, scheme, source, "test", events);
        BeneathCards.Place(world, scheme, source, true, "test", events);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Empty(events);
        Discard.Card(world, sleeper, "test", events);
        Assert.True(Discard.EffectCanRemove(world, Cards, source, scheme));
        Discard.Card(world, scheme, "test", events);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }
}
