using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RoninActualCardTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:upgrade.3")]
    [Rule("rr:attachment")]
    public void BonusRequiresAnUpgradeOnRoninAndDoesNotCountCopies()
    {
        // "Some upgrades enter play and ‘attach to’ another card."
        // "Attachment is an encounter card type."
        // Printed 12013 grants +1 THW and +1 ATK while an upgrade is attached.
        var world = ThorActualFixture.Board("06001a", 2);
        var ronin = world.CreateCard("12013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var suit = world.CreateCard("12018", Hosted(world, other));
        var attachment = world.CreateCard("04117", Hosted(world, ronin));
        AssertStats(world, ronin, 2, 1);
        World.MoveToTop(suit, Hosted(world, ronin));
        AssertStats(world, ronin, 3, 2);
        var second = world.CreateCard("01074", Hosted(world, ronin));
        // Inspired contributes its own +1 to both stats; Ronin's bonus stays +1.
        AssertStats(world, ronin, 4, 3);
        Blank(world, second);
        AssertStats(world, ronin, 3, 2);
        Discard.Card(world, suit, "test", []);
        AssertStats(world, ronin, 3, 2);
        World.MoveToTop(second, world.Seats[1].Hand);
        AssertStats(world, ronin, 2, 1);
        Assert.Equal(ronin.ObjectId, attachment.Area.Host);
    }

    [Fact]
    [Rule("rr:ownership-and-control.2.1")]
    public void PlayedUpgradeAndRoninBlankingFollowTheCurrentController()
    {
        // "Upgrades attached to a card controlled by a player other than the
        // upgrade’s owner are controlled by that other player."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var ronin = world.CreateCard("12013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var suit = world.CreateCard("12018", world.Seats[0].Hand);
        var payment = world.CreateCard("12021", world.Seats[0].Hand);
        Assert.Contains(ronin.ObjectId, runner.AttachmentTargets(world, suit)!);
        CardPlay.Play(world, Cards, runner, world.Seats[0], suit, [payment.ObjectId], [], [ronin.ObjectId]);
        Assert.Equal(0, suit.Owner);
        Assert.Equal(PlayArea.Of(1), suit.Area.PlayArea);
        AssertStats(world, ronin, 3, 2);
        Blank(world, suit);
        AssertStats(world, ronin, 3, 2);
        Blank(world, ronin);
        AssertStats(world, ronin, 2, 1);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        AssertStats(world, ronin, 3, 2);
        CardControlTransfer.TakeControl(world, Cards, ronin, 0);
        Assert.Equal(0, CardControl.ControllerOf(world, ronin));
        Assert.Equal(0, CardControl.ControllerOf(world, suit));
        Assert.Equal(1, ronin.Owner);
        AssertStats(world, ronin, 3, 2);
        Discard.Card(world, suit, "test", []);
        AssertStats(world, ronin, 2, 1);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
    }

    private static Area Hosted(World world, Card host) =>
        world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Owner);

    private static void Blank(World world, Card card) => world.Effects.Register(
        new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: card.ObjectId, Affects: card.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
        { TargetIncarnation = card.Incarnation });

    private static void AssertStats(World world, Card ronin, long attack, long thwart)
    {
        Assert.Equal(attack, Modified(world, ronin, "attack"));
        Assert.Equal(thwart, Modified(world, ronin, "thwart"));
    }
}
