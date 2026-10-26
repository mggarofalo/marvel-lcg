using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LiveLastingTraitReviewTests
{
    [Fact]
    [Rule("rr:lasting-effects.3")]
    public void ActualUltronGrantReadsEffectiveTraitAndOriginalEngagedSeat()
    {
        // "Lasting effects update whenever the game state updates."
        // Actual Ultron creates a Drone from a physical resource card. Synthetic
        // trait changes and in-play movement isolate the live selector during Whip.
        var world = WidowEncounterFixture.Board();
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        World.MoveToTop(f.Villain, world.AreaOf(DeckType.RemovedArea));
        Card ultron = world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        Card upgrade = WidowEncounterFixture.Upgrade(world, "01057");
        world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: ultron.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            Card drone = Assert.Single(world.Cards, card => card.Area.Type == DeckType.EngagedEnemiesArea);
            Assert.Equal(CardKind.Resource, world.Facts.Kind(drone.FaceId));
            Assert.Equal(CardKind.Minion, EffectiveCards.Kind(drone, world.Facts));
            Assert.Equal(3, Attack(world, ultron));
            using (world.Effects.Register(new(EffectSource.LastingEffect,
                Characteristics.Lost + Traits.Granted + "DRONE", Card: ultron.ObjectId,
                Affects: drone.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack))))
                Assert.Equal(2, Attack(world, ultron));
            Assert.Equal(3, Attack(world, ultron));
            World.MoveToTop(drone, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Assert.Equal(2, Attack(world, ultron));
            World.MoveToTop(drone, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            Assert.Equal(3, Attack(world, ultron));
            return Decision.Take(upgrade.ObjectId);
        });
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(2, Attack(world, ultron));
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }

    private static long Attack(World world, Card card) =>
        StateFields.Modified(world, card, "attack", world.Facts, world.Players);
}
