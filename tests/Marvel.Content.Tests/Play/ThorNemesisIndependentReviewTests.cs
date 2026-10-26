using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorNemesisIndependentReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:obligation.1")]
    public void OdinCannotRemoveWithoutReadyAlterEgoAndCannotDiscardAnotherHand(bool alterEgo)
    {
        // "Abilities on obligations that use the words 'you' or 'your' apply only to the player whose play area the obligation is in."
        var world = ThorActualFixture.Board(alterEgo ? "06001b" : "06001a", 2);
        var runner = AuthoredCards.Runner();
        var identity = world.Seats[0].IdentityCard;
        if (alterEgo) identity.Exhaust();
        var foreignHammer = world.CreateCard("06009", world.Seats[1].Hand);
        var obligation = new RedSkullFixture(world, runner).Reveal("06026", 1);
        var flip = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(0, flip.Player);
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        var only = Assert.Single(choice.Affordances);
        Assert.Contains("Discard Mjolnir", only.Description);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(only.Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(world.Seats[1].Hand, foreignHammer.Area);
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.Equal(alterEgo ? "06001b" : "06001a", identity.FaceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:tough.3")]
    public void FrostGiantTracksSurvivingDefendingAllyAndEndsWithActivation(bool tough)
    {
        // A character whose tough prevents the damage "is not considered to have taken damage."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var identity = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        var boost = world.CreateCard("06029", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(tough ? 0 : 3, ally.Damage);
        Assert.Equal(!tough, Statuses.Has(world, ally, Statuses.Stunned));
        Assert.Equal(0, identity.Damage);
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
        if (!tough) World.MoveToTop(Assert.Single(Statuses.On(world, ally, Statuses.Stunned)), world.AreaOf(DeckType.RemovedArea));
        DamagePlacement.Resolve(world, world.Facts, new(ally, ally, 1, "later", "later"), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(Statuses.Has(world, ally, Statuses.Stunned));
    }
}
