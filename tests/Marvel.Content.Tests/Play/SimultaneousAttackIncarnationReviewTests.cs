using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousAttackIncarnationReviewTests
{
    [Fact]
    [Rule("rr:damage.step.6")]
    [Rule("rr:leaves-play.1")]
    public void ReenteredRecipientIsNotDefeatedByDamageToItsPreviousIncarnation()
    {
        // rr:leaves-play.1: a card that reenters play is a new copy.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01083","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"seq":[{"returnToHand":"this"},
                {"putIntoPlay":{"card":"this","where":"yourPlayArea"}}]}
            }]}]}
            """));
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard("01101", area);
        var second = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        int incarnation = second.Incarnation;
        var events = new List<GameEvent>();
        var hero = world.Seats[0].IdentityCard;
        var result = SimultaneousAttackDamage.Resolve(world, cards,
            new(hero, hero, [first, second], 10, "synthetic", "Attack"), events);
        Assert.Null(Sequence.Work(world, cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(incarnation + 1, second.Incarnation);
        Assert.True(DeckTypes.IsInPlay(second.Area.Type));
        Assert.Equal(0, second.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(0, result.Recipients[1].Excess);
    }
}


