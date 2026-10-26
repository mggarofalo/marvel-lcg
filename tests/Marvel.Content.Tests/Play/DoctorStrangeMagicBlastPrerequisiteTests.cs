using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeMagicBlastPrerequisiteTests
{
    [Theory]
    [InlineData("01090", 5, true, false, false)]
    [InlineData("01090", 5, true, false, true)]
    [InlineData("01088", 7, false, false, false)]
    [InlineData("01088", 7, false, false, true)]
    [InlineData("01089", 5, false, true, false)]
    [InlineData("01089", 5, false, true, true)]
    [InlineData("01055", 7, true, true, false)]
    [InlineData("01055", 7, true, true, true)]
    [Rule("rr:printed")]
    [Rule("rr:and.1")]
    public void ExactDiscardReceiptAppliesEachPrintedResourceTypeOnce(
        string face, int damage, bool stunned, bool confused, bool discardFirst)
    {
        // "Printed" refers to the physically printed card; independent effects
        // "resolve as much of each effect as possible". Ruling 59bea1238c870653
        // checks presence rather than the count of double-resource icons.
        var world = ThorActualFixture.Board("06001a");
        var discarded = world.CreateCard(face, world.Seats[0].Deck);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var events = Play(world, enemy, discardFirst);
        Assert.Equal(damage, enemy.Damage);
        Assert.Equal(stunned, Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.Equal(confused, Statuses.Has(world, enemy, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
        Assert.NotEmpty(events);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void DefeatedEnemyIsNotReplacedForWildBranches()
    {
        // A card that leaves play has "no memory" of its previous existence.
        var world = ThorActualFixture.Board("06001a");
        world.CreateCard("01055", world.Seats[0].Deck);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Play(world, minion);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(0, villain.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        Assert.False(Statuses.Has(world, villain, Statuses.Confused));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:event")]
    public void FinalDeckCardResetsBeforeMagicBlastIsDiscardedAndReceiptSurvives()
    {
        // Events enter their discard "after those effects resolve".
        // Ruling d1f9f33e3ae4ba2f explicitly confirms this Magic Blast ordering.
        var world = ThorActualFixture.Board("06001a");
        foreach (var card in world.Seats[0].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var discarded = world.CreateCard("01055", world.Seats[0].Deck);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Play(world, enemy);
        Assert.Equal(7, enemy.Damage);
        Assert.True(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.True(Statuses.Has(world, enemy, Statuses.Confused));
        Assert.Equal(DeckType.PlayerDeck, discarded.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards, c => c.FaceId == "09004");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:event.5")]
    public void OneAttackRetainsModifierAndSeparateDamageReceipts(bool discardFirst)
    {
        // "Each of those instances is modified." The printed 5 and conditional
        // 2 remain distinct damage instances within the same labelled attack.
        var world = ThorActualFixture.Board("06001a");
        world.CreateCard("01055", world.Seats[0].Deck);
        var source = world.Seats[0].Hand.Cards.SingleOrDefault(c => c.FaceId == "09004")
            ?? world.CreateCard("09004", world.Seats[0].Hand);
        var modifier = new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attackDamage", Amount: 1, Affects: source.ObjectId,
            Lasts: new Duration(Uses: 1));
        world.Effects.Register(modifier);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var events = Play(world, enemy, discardFirst);
        Assert.Equal(9, enemy.Damage);
        Assert.DoesNotContain(modifier, world.Effects.Active());
        var attack = Assert.Single(events.OfType<AttackBoundary>(), e => e.Started);
        Assert.Single(events.OfType<AttackBoundary>(), e => !e.Started);
        var damage = events.OfType<FieldSet>().Where(e => e.Card == enemy.ObjectId
            && e.Field == "health" && e.AttackIdentity is not null).ToArray();
        Assert.Equal(new long?[] { 6, 3 }, damage.Select(e => e.From - e.To));
        Assert.All(damage, e => Assert.Equal(attack.Identity, e.AttackIdentity));
    }

    [Fact]
    public void OrderSuspensionRetainsUninterruptedPowerCausalMetadata()
    {
        var world = ThorActualFixture.Board("06001a");
        world.CreateCard("01088", world.Seats[0].Deck);
        var actual = Play(world, world.TheCardIn(DeckType.VillainArea)!);
        var control = ThorActualFixture.Board("06001a");
        control.CreateCard("01088", control.Seats[0].Deck);
        var expected = Play(control, control.TheCardIn(DeckType.VillainArea)!,
            sequentialControl: true);
        var damage = actual.OfType<FieldSet>().Where(e => e.AttackIdentity is not null).ToArray();
        Assert.NotEmpty(damage);
        Assert.Equal(expected.OfType<FieldSet>().Where(e => e.AttackIdentity is not null)
            .Select(e => (e.Card, e.Field, e.From, e.To, e.Trigger, e.Verb,
                e.AttackIdentity, e.Cause)),
            damage.Select(e => (e.Card, e.Field, e.From, e.To, e.Trigger, e.Verb,
                e.AttackIdentity, e.Cause)));
    }
    private static List<GameEvent> Play(World world, Card enemy, bool discardFirst = false, bool sequentialControl = false)
    {
        var runner = DoctorStrangeMagicBlastFixture.Runner(sequentialControl);
        world.Abilities = runner;
        var source = world.Seats[0].Hand.Cards.SingleOrDefault(c => c.FaceId == "09004")
            ?? world.CreateCard("09004", world.Seats[0].Hand);
        var first = world.CreateCard("01088", world.Seats[0].Hand);
        var second = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [first.ObjectId, second.ObjectId], [])).Events.ToList();
        bool selected = false;
        for (int count = 0; count < 30; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Decision answer;
            if (prompt.Asking == Question.Order)
                answer = Decision.Take(prompt.Affordances[0].Id, discardFirst ? [1, 0] : [0, 1], []);
            else if (!selected && prompt.Affordances.Any(a => a.AnchorId == enemy.ObjectId))
            {
                selected = true;
                answer = Decision.Take(prompt.Affordances.Single(a => a.AnchorId == enemy.ObjectId).Id);
            }
            else answer = Decision.Decline;
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.True(selected);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        return events;
    }
}
