using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NightNursePhysicalStatusTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [Rule("rr:status-cards.1.1")]
    [Rule("rr:uses-x-type")]
    public void SelectsExactlyOnePhysicalStatusOnTheChosenForeignHeroAfterPayment(int counters, int controller)
    {
        // Steady permits "one additional confused status card and one additional stunned status card".
        // Uses: "When the last all-purpose counter is removed ... discard that card."
        var (world, runner, source) = NightNurseStatusHostTests.Board(counters, controller);
        var hero = world.Seats[1 - controller].IdentityCard;
        hero.TakeDamage(2);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "steady", Amount: 1, Affects: hero.ObjectId));
        var first = Statuses.Give(world, hero, Statuses.Stunned);
        var second = Statuses.Give(world, hero, Statuses.Stunned);
        var tough = Statuses.Give(world, hero, Statuses.Tough);
        var foreign = Statuses.Give(world, world.Seats[controller].IdentityCard, Statuses.Confused);
        Assert.NotSame(first, second);
        Assert.DoesNotContain(runner.Actions(world, 1 - controller), item => item.Card == source.ObjectId);
        var action = Assert.Single(runner.Actions(world, controller), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Assert.Equal(counters - 1, source.Tokens["c_medical"]);
        Assert.Equal(counters == 1 ? DeckType.DiscardPile : DeckType.SupportsArea, source.Area.Type);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(controller, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(hero.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(Assert.Single(prompt.Affordances).Id, [1, 0]), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(controller, prompt.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId, tough.ObjectId }, prompt.Affordances.Select(item => item.AnchorId));
        string before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(foreign.ObjectId), events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(2, hero.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(second.ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.RemovedArea, second.Area.Type);
        Assert.All(new[] { first, tough, foreign }, card => Assert.Equal(DeckType.StatusArea, card.Area.Type));
        var moved = Assert.Single(events.OfType<CardsMoved>(), item => item.Cards.Any(card => card.Card == second.ObjectId));
        Assert.Equal(hero.ObjectId, moved.From.Host);
        Assert.Equal("Discard", moved.Verb);
        Assert.Equal("RemovedArea", moved.To.Zone);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    public void RepeatedOffersAndStatusPromptsPreserveRngAndContinuationAgainstAnUnqueriedControl(int counters, int controller)
    {
        string Run(bool probe)
        {
            var (world, runner, source) = NightNurseStatusHostTests.Board(counters, controller);
            var hero = world.Seats[1 - controller].IdentityCard;
            hero.TakeDamage(2);
            var status = Statuses.Give(world, hero, Statuses.Confused);
            var retained = Statuses.Give(world, hero, Statuses.Tough);
            Statuses.Give(world, world.Seats[controller].IdentityCard, Statuses.Stunned);
            string before = world.Digest().Canonical();
            var rng = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            if (probe)
                for (int i = 0; i < 3; i++)
                    foreach (var offered in runner.Actions(world, controller)) runner.Describe(world, offered);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding);
            var action = Assert.Single(runner.Actions(world, controller), item => item.Card == source.ObjectId);
            var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
            Assert.Equal(counters - 1, source.Tokens["c_medical"]);
            Assert.Equal(counters == 1 ? DeckType.DiscardPile : DeckType.SupportsArea, source.Area.Type);
            var prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(hero.ObjectId), events);
            prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Sequence.Answer(world, world.Facts, runner, prompt, new Decision(Assert.Single(prompt.Affordances).Id, [1, 0]), events);
            prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Assert.Equal(controller, prompt.Player);
            Assert.Equal([source.ObjectId], prompt.ContextCardIds);
            Assert.Equal(new[] { status.ObjectId, retained.ObjectId }, prompt.Affordances.Select(item => item.AnchorId));
            before = world.Digest().Canonical();
            pending = world.Agenda.Outstanding.ToArray();
            var current = world.Agenda.Current ?? throw new InvalidOperationException("paid status frame missing");
            var stage = world.Agenda.Stage;
            var paidRandom = world.Random.Generator.GetState();
            long paidWords = world.Random.Generator.WordsConsumed;
            var priorEvents = events.ToArray();
            if (probe)
                for (int i = 0; i < 3; i++)
                {
                    var queried = runner.Choosing(world, current)!;
                    Assert.Equal(controller, queried.Player);
                    Assert.Equal([source.ObjectId], queried.ContextCardIds);
                    Assert.False(queried.Cancellable);
                    Assert.Equal(new[] { status.ObjectId, retained.ObjectId }, queried.Affordances.Select(item => item.AnchorId));
                    Assert.All(queried.Affordances, item => Assert.Equal(source.ObjectId, item.SelectionSourceId));
                }
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Equal(current, world.Agenda.Current);
            Assert.Equal(stage, world.Agenda.Stage);
            Assert.Equal(priorEvents, events);
            Assert.Equal(paidWords, world.Random.Generator.WordsConsumed);
            Assert.Equal(paidRandom.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(paidRandom.Index, world.Random.Generator.GetState().Index);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(status.ObjectId), events);
            Sequence.Finish(world, world.Facts, runner, events);
            Assert.Equal(1, hero.Damage);
            Assert.Equal(DeckType.RemovedArea, status.Area.Type);
            Assert.Equal(DeckType.StatusArea, retained.Area.Type);
            Assert.Empty(world.Agenda.Outstanding);
            return JsonSerializer.Serialize(new
            {
                Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
                world.Random.Generator.WordsConsumed, world.Agenda.Stage,
                Events = events.Select(item => JsonSerializer.Serialize(item, item.GetType())),
            });
        }
        Assert.Equal(Run(false), Run(true));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void UnpayableCostsAndNonheroTargetsCannotActivate(int counters, bool ready)
    {
        var (world, runner, source) = NightNurseStatusHostTests.Board(counters);
        if (!ready) source.Exhaust();
        world.Seats[0].IdentityCard.TurnTo("06001b");
        world.Seats[0].IdentityCard.TakeDamage(2);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        Statuses.Give(world, ally, Statuses.Tough);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
