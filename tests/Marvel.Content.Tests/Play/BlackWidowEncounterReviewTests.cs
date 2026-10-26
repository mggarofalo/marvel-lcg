using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowEncounterReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:unique-icon.4.1")]
    public void QuincarrierInEitherPlayersAreaBlocksAnotherCopy(int existingSeat)
    {
        // A matching player card in play "cannot be played or put into play".
        var world = WidowEncounterFixture.Board();
        var entering = world.CreateCard("08023", world.Seats[1].Hand);
        var control = world.CreateCard("01092", world.Seats[1].Hand);
        world.CreateCard("08020", world.Seats[1].Hand);
        world.CreateCard("08021", world.Seats[1].Hand);
        Assert.NotNull(CardPayment.Price(world, world.Facts, world.Seats[1], entering));
        world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(existingSeat), cardOwner: existingSeat));
        Assert.Null(CardPayment.Price(world, world.Facts, world.Seats[1], entering));
        Assert.NotNull(CardPayment.Price(world, world.Facts, world.Seats[1], control));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskmasterCountsOrdinaryUpgradesAndTracksItsEngagement(bool boost)
    {
        // Printed 08026 counts every upgrade, including non-Preparation upgrades.
        var world = WidowEncounterFixture.Board();
        var ordinary = WidowEncounterFixture.Upgrade(world, "01007");
        WidowEncounterFixture.Upgrade(world);
        WidowEncounterFixture.Upgrade(world, "01007", 0);
        world.CreateCard("08010", world.Seats[1].Hand);
        world.CreateCard("08010", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 1));
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        if (boost)
        {
            world.CreateCard("08026", world.AreaOf(DeckType.EncounterDeck));
            var villain = f.Villain;
            world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
                Subject: villain.ObjectId, Seat: 1));
            f.Finish();
            Assert.Equal(4, world.Seats[1].IdentityCard.Damage);
            Assert.Equal(2, StateFields.Modified(world, villain, "attack", world.Facts, 2));
        }
        else
        {
            var minion = f.Reveal("08026", 1);
            f.Finish();
            Assert.Equal(2, StateFields.Modified(world, minion, "attack", world.Facts, 2));
            Discard.Card(world, ordinary, "fixture", []);
            Assert.Equal(1, StateFields.Modified(world, minion, "scheme", world.Facts, 2));
            WidowEncounterFixture.Upgrade(world, "01007", 0);
            World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Assert.Equal(2, StateFields.Modified(world, minion, "attack", world.Facts, 2));
        }
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("08025", 2, false)]
    [InlineData("08025", 2, true)]
    [InlineData("08029", 1, false)]
    [InlineData("08029", 1, true)]
    [Rule("rr:boost-boost-icon.2")]
    public void NumericBoostDoesNotResolveBurnNoticeOrDeadlyShot(string face, int icons, bool attack)
    {
        // "Only the ability text beneath the divider line is active" as a boost.
        var world = WidowEncounterFixture.Board(attack);
        var own = WidowEncounterFixture.Upgrade(world);
        var boost = world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish();
        Assert.Equal(attack ? 2 + icons : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(attack ? 0 : 1 + icons, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.UpgradesArea, own.Area.Type);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
    }
}
