using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HitPointAssignmentAreaTests
{
    [Fact]
    public void AssignmentCannotHideASubsequentAmbiguousDiscardSelector()
    {
        // Synthetic different-title stage progression makes the attached Charge leave with its villain.
        var (world, source, _, _) = ActionAbilityTypedAreaProjectionFixtures.AreaProjectionBoard();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("01136", world.AreaOf(DeckType.VillainDeck));
        var attached = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        var discarded = world.CreateCard("01099", world.AreaOf(DeckType.EncounterDiscardPile));
        var runner = ActionAbilitySupportFixtures.Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"setHitPoints":{"card":{"query":"villain"},"amount":1}},
              {"dealDamage":{"cards":{"query":"villain"},"amount":1}},
              {"removeFromGame":{"cardsIn":{"area":"encounterDiscardPile","title":"Charge"}}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.True(source.Ready);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(villain.ObjectId, attached.Area.Host);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
    }
}
