using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic board; Kang's public admission stays closed.
public sealed class KangTimeTravelHijinksEliminationTests
{
    [Fact]
    [Rule("rr:then.1")]
    [Rule("rr:player-elimination.step.5")]
    public void ADiscardThatEliminatesTheHolderLeavesNothingToPlaceBeneathTheRemovedObligation()
    {
        // 11021: "When Revealed: Discard the highest-cost card you control, then place it
        // facedown under this card." Discarding Mark V Armor ("You get +6 hit points") defeats
        // Iron Man; rr:player-elimination.step.5 removes his play area, this obligation with it.
        // rr:then.1: the post-"then" text "must also attempt to resolve", and nothing can be
        // placed beneath a card that has left play. The other players continue.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Board(4, false, events: events);
        var hero = world.Seats[3].IdentityCard;
        hero.TurnTo("01029a");
        var armor = world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(3)));
        hero.TakeDamage(10);

        KangStageFlowFixture.Reveal(world, "11021", 3, events);

        Assert.True(world.Seats[3].Eliminated);
        Assert.False(BeneathCards.IsStored(armor.Area.Type));
        Assert.All(world.Seats.Take(3), seat => Assert.False(seat.Eliminated));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
