using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LiveLastingActualRowsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.5")]
    public void TwoBerserkMutateBoostsBothReadTheFinalCounterCount(bool attack)
    {
        // "Lasting effects update whenever the game state updates."
        // Actual 04116 increments the main scheme twice; both lasting bonuses read two.
        var world = WidowEncounterFixture.Board(attack);
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        world.CreateCard("04116", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("04116", world.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish();
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(attack ? 6 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(attack ? 0 : 5, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.LiveAmount is not null);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }

    [Fact]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.5")]
    public void ActualUltronGrantTracksDroneDepartureBeforeCompletedAttack()
    {
        // "Lasting effects update whenever the game state updates."
        // Actual Ultron creates the Drone; test-only departure while Whip is suspended
        // distinguishes a live count from an attack-start snapshot.
        var world = WidowEncounterFixture.Board();
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        World.MoveToTop(f.Villain, world.AreaOf(DeckType.RemovedArea));
        var ultron = world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        var upgrade = WidowEncounterFixture.Upgrade(world, "01007");
        world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: ultron.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            var drone = Assert.Single(world.Cards, card => card.Area.Type == DeckType.EngagedEnemiesArea);
            Assert.Equal(3, StateFields.Modified(world, ultron, "attack", world.Facts, world.Players));
            Discard.Card(world, drone, "fixture", []);
            Assert.Equal(2, StateFields.Modified(world, ultron, "attack", world.Facts, world.Players));
            return Decision.Take(upgrade.ObjectId);
        });
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.LiveAmount is not null);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }

    [Fact]
    [Rule("rr:lasting-effects.3")]
    public void ActualBerserkBoostReadsCurrentMainSchemeAfterMembershipChanges()
    {
        // "Lasting effects update whenever the game state updates."
        // Test-only main-scheme replacement exercises the live relation during a real boost continuation.
        var world = WidowEncounterFixture.Board();
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        var upgrade = WidowEncounterFixture.Upgrade(world, "01007");
        world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("04116", world.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            var original = f.Main;
            Assert.Equal(1, original.Tokens.GetValueOrDefault("c_test"));
            World.MoveToTop(original, world.AreaOf(DeckType.RemovedArea));
            var replacement = world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
            replacement.PlaceTokens("c_test", 3);
            Assert.Equal(5, StateFields.Modified(world, f.Villain, "attack", world.Facts, world.Players));
            return Decision.Take(upgrade.ObjectId);
        });
        Assert.Equal(5, world.Seats[1].IdentityCard.Damage);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.LiveAmount is not null);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }
}
