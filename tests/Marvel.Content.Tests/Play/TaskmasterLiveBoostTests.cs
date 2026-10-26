using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TaskmasterLiveBoostTests
{
    [Fact]
    [Rule("rr:lasting-effects.3")]
    public void TaskmasterBoostUpdatesAfterLaterElectricWhipDiscardsAnUpgrade()
    {
        // "Lasting effects update whenever the game state updates."
        // Both boost rows are actual authored cards; Electric Whip discards the
        // only upgrade after Taskmaster resolves but before attack calculation.
        var world = WidowEncounterFixture.Board();
        var own = WidowEncounterFixture.Upgrade(world, "01007");
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        var whip = world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        var taskmaster = world.CreateCard("08026", world.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            Assert.Equal(1, prompt.Player);
            Assert.Equal(own.ObjectId, Assert.Single(prompt.Affordances).Id);
            return Decision.Take(own.ObjectId);
        });
        Assert.Equal(DeckType.DiscardPile, own.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, taskmaster.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, whip.Area.Type);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.5")]
    public void SuspendedActualBoostKeepsSeatBindingAndQueriesDoNotChangeContinuation(bool probe)
    {
        // "Lasting effects update whenever the game state updates."
        // Test-only in-play moves exercise the captured binding while actual Electric Whip is suspended.
        var world = WidowEncounterFixture.Board();
        var own = WidowEncounterFixture.Upgrade(world, "01007");
        var foreign = WidowEncounterFixture.Upgrade(world, "01007", 0);
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("08026", world.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            var effects = world.Effects.Registered.Where(effect => effect.LiveAmount is not null).ToArray();
            Assert.Equal(2, effects.Length);
            Assert.All(effects, effect => Assert.Equal(new ContinuousAmount.ControlledUpgrades(1), effect.LiveAmount));
            var before = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            long consumed = world.Random.Generator.WordsConsumed;
            var current = world.Agenda.Current;
            var stage = world.Agenda.Stage;
            int pending = world.Agenda.Count;
            var activation = world.Activation;
            if (probe)
            {
                for (int read = 0; read < 3; read++)
                {
                    Assert.Equal(3, Value(world, f.Villain, "attack"));
                    Assert.Equal(2, Value(world, f.Villain, "scheme"));
                    _ = CardValues.Evaluate(world, f.Villain, "attack", world.Facts, world.Players);
                }
                Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, f.Runner,
                    prompt, Decision.Take(foreign.ObjectId), []));
            }
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            Assert.Equal(current, world.Agenda.Current);
            Assert.Equal(stage, world.Agenda.Stage);
            Assert.Equal(pending, world.Agenda.Count);
            Assert.Same(activation, world.Activation);
            Assert.Equal(effects, world.Effects.Registered.Where(effect => effect.LiveAmount is not null));
            Assert.Equal(3, Value(world, f.Villain, "attack"));
            // Transfer preserves ownership while control follows the destination area.
            World.MoveToTop(foreign, own.Area);
            Assert.Equal(0, foreign.Owner);
            Assert.Equal(4, Value(world, f.Villain, "attack"));
            World.MoveToTop(own, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            Assert.Equal(3, Value(world, f.Villain, "attack"));
            var gained = WidowEncounterFixture.Upgrade(world);
            Assert.Equal(4, Value(world, f.Villain, "attack"));
            Discard.Card(world, gained, "fixture", []);
            Assert.Equal(3, Value(world, f.Villain, "attack"));
            World.MoveToTop(foreign, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(own, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
            return Decision.Take(own.ObjectId);
        });
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(2, Value(world, f.Villain, "attack"));
        Assert.Equal(1, Value(world, f.Villain, "scheme"));
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.LiveAmount is not null);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }

    private static long Value(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);
}
