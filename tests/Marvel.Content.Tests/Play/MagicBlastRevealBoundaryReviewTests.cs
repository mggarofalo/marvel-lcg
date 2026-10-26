using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MagicBlastRevealBoundaryReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-revealed-abilities")]
    [Rule("rr:damage.step.3")]
    public void PendingActualGoblinRevealKeepsThePowerTailBehindItsAllocation(bool discardFirst)
    {
        // "All When Revealed abilities on the card resolve." Norman's printed
        // damage replacement flips him and reveals Goblin before the later 2.
        var world = ThorActualFixture.Board("06001a");
        var runner = DoctorStrangeMagicBlastFixture.Runner();
        world.Abilities = runner;
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        var enemy = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea))
            .PlaceTokens("c_infamy", 5);
        var discarded = world.CreateCard("01055", world.Seats[0].Deck);
        var source = world.CreateCard("09004", world.Seats[0].Hand);
        var first = world.CreateCard("01088", world.Seats[0].Hand);
        var second = world.CreateCard("01089", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [first.ObjectId, second.ObjectId], [])).Events.ToList();
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Answer(world, world.Facts, runner, target,
            Decision.Take(target.Affordances.Single(a => a.AnchorId == enemy.ObjectId).Id), events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(Assert.Single(order.Affordances).Id, discardFirst ? [1, 0] : [0, 1], []), events);

        var allocation = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var option = Assert.Single(allocation.Affordances);
        Assert.Equal(3, option.Targets!.Min);
        Assert.Equal("02001b", enemy.FaceId);
        Assert.Equal(0, enemy.Damage);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.False(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.False(Statuses.Has(world, enemy, Statuses.Confused));
        Assert.Equal(discardFirst ? DeckType.DiscardPile : DeckType.PlayerDeck, discarded.Area.Type);
        Assert.NotEqual(DeckType.DiscardPile, source.Area.Type);
        Assert.DoesNotContain(events.OfType<AttackBoundary>(), e => !e.Started);
        string before = Snapshot(world, events);
        string promptBefore = JsonSerializer.Serialize(allocation);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(promptBefore, JsonSerializer.Serialize(Sequence.Work(world, world.Facts, runner, events)));
            Assert.Equal(before, Snapshot(world, events));
        }
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, allocation,
            Decision.Take(option.Id, [hero.ObjectId, hero.ObjectId], []), events));
        Assert.Equal(before, Snapshot(world, events));
        Assert.Equal(promptBefore, JsonSerializer.Serialize(Sequence.Work(world, world.Facts, runner, events)));

        Sequence.Answer(world, world.Facts, runner, allocation,
            Decision.Take(option.Id, [hero.ObjectId, hero.ObjectId, hero.ObjectId], []), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(3, hero.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(2, enemy.Damage);
        Assert.True(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.True(Statuses.Has(world, enemy, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        var attack = Assert.Single(events.OfType<AttackBoundary>(), e => e.Started);
        Assert.Single(events.OfType<AttackBoundary>(), e => !e.Started);
        var separateDamage = Assert.Single(events.OfType<FieldSet>(), e => e.Card == enemy.ObjectId
            && e.Field == "health" && e.AttackIdentity is not null);
        Assert.Equal(2, separateDamage.From - separateDamage.To);
        Assert.Equal(attack.Identity, separateDamage.AttackIdentity);
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new
        {
            step.What, step.Round, step.Number, step.Index, step.Subject, step.Seat,
            step.Plan, step.OccurrenceId, Operation = JsonSerializer.Serialize(step.Operation, step.Operation.GetType()),
        }),
        Events = events.Select(value => JsonSerializer.Serialize(value, value.GetType())),
    });
}
