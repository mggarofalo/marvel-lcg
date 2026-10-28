using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManSupportingCardTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:remaining-hit-points.2")]
    public void GiantManTracksRemainingHealthAndBlanking()
    {
        // Start with maximum hit points and "subtract their sustained damage".
        var world = ThorActualFixture.Board("06001a", 2);
        var giant = world.CreateCard("12012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal(4, Modified(world, giant, "attack"));
        giant.TakeDamage(1);
        Assert.Equal(4, Modified(world, giant, "attack"));
        giant.TakeDamage(1);
        Assert.Equal(2, Modified(world, giant, "attack"));
        var suit = world.CreateCard("12018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), giant.ObjectId, 1));
        Assert.Equal(4, Modified(world, giant, "attack"));
        Discard.Card(world, suit, "test", []);
        Assert.Equal(2, Modified(world, giant, "attack"));
        DamageRecovery.Heal(world, Cards, giant, 1, "test", "test", []);
        Assert.Equal(4, Modified(world, giant, "attack"));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: giant.ObjectId, Affects: giant.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
            { TargetIncarnation = giant.Incarnation });
        Assert.Equal(2, Modified(world, giant, "attack"));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(4, Modified(world, giant, "attack"));
    }

    [Fact]
    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:hit-points.3.1")]
    [Rule("rr:max-maximum.4")]
    public void SuitAttachesToAnotherPlayersAllyAndDepartureCanDefeatIt()
    {
        // A player controls upgrades attached to characters they control. An ally
        // is defeated if loss of extra hit points leaves damage at or above HP.
        // "Max 1 per [game element]" limits copies attached to that element.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("12012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var suit = world.CreateCard("12018", world.Seats[0].Hand);
        var payment = world.CreateCard("12021", world.Seats[0].Hand);
        Assert.Contains(ally.ObjectId, runner.AttachmentTargets(world, suit)!);
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, runner.AttachmentTargets(world, suit)!);
        CardPlay.Play(world, Cards, runner, world.Seats[0], suit, [payment.ObjectId], [], [ally.ObjectId]);
        Assert.Equal(0, suit.Owner);
        Assert.Equal(PlayArea.Of(1), suit.Area.PlayArea);
        Assert.Equal(ally.ObjectId, suit.Area.Host);
        Assert.Equal(6, DamagePlacement.Health(world, Cards, ally));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: suit.ObjectId, Affects: suit.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
            { TargetIncarnation = suit.Incarnation });
        Assert.Equal(4, DamagePlacement.Health(world, Cards, ally));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(6, DamagePlacement.Health(world, Cards, ally));
        var otherAlly = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var duplicate = world.CreateCard("12018", world.Seats[1].Hand);
        Assert.DoesNotContain(ally.ObjectId, runner.AttachmentTargets(world, duplicate)!);
        Assert.Contains(otherAlly.ObjectId, runner.AttachmentTargets(world, duplicate)!);
        ally.TakeDamage(4);
        Discard.Card(world, suit, "test", []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(DeckType.DiscardPile, suit.Area.Type);
    }

}
