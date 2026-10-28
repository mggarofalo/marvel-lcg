using static Marvel.Content.Tests.Play.AntManNemesisCardFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SizeIncreaseCopiesReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:triggering-condition.1.1")]
    [Rule("rr:forced.5")]
    public void BothPhysicalCopiesRespondToOneActivation(bool scheme)
    {
        // rr:triggering-condition.1.1: "Multiple copies of a card with an interrupt or response can each be triggered by the same triggering condition."
        // rr:forced.5: the first player chooses the order of simultaneous forced abilities.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var enemy = world.CreateCard(Yellowjacket,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var first = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, first, 0, []);
        var second = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, second, 0, []);
        Assert.Equal(enemy.ObjectId, first.Area.Host);
        Assert.Equal(enemy.ObjectId, second.Area.Host);
        Assert.Equal(6, StateFields.Modified(world, enemy, "attack", Facts, world.Players));
        Assert.Equal(6, StateFields.Modified(world, enemy, "scheme", Facts, world.Players));
        Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 0, Subject: enemy.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        Prompt? order = null;
        for (int i = 0; i < 20 && order is null; i++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Facts, runner, events));
            if (prompt.Asking == Question.Order) order = prompt;
            else
            {
                Assert.True(prompt.Cancellable, prompt.Description);
                Sequence.Answer(world, Facts, runner, prompt, Decision.Decline, events);
            }
        }
        Assert.NotNull(order);
        Assert.Contains(order.Affordances, option => option.AnchorId == first.ObjectId);
        Assert.Contains(order.Affordances, option => option.AnchorId == second.ObjectId);
        var chosen = Assert.Single(order.Affordances, option => option.AnchorId == second.ObjectId);
        Sequence.Answer(world, Facts, runner, order, Decision.Take(chosen.Id), events);
        events.AddRange(Finish(world, runner));
        Assert.Equal(2, first.Tokens.GetValueOrDefault("c_size"));
        Assert.Equal(2, second.Tokens.GetValueOrDefault("c_size"));
        Assert.Equal(new[] { second.ObjectId, first.ObjectId }, events.OfType<FieldSet>()
            .Where(field => field.Field == "c_size" && field.To == 2)
            .Select(field => field.Card));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }
}

