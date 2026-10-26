using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored 09019 on nonpublic boards; no public product admission.</summary>
public sealed class NightNurseStatusHostTests
{
    [Theory]
    [InlineData(0, false, false, false)]
    [InlineData(2, false, true, false)]
    [InlineData(2, false, true, true)]
    [InlineData(0, true, true, false)]
    [InlineData(0, true, true, true)]
    [InlineData(2, true, true, false)]
    [InlineData(2, true, true, true)]
    [Rule("rr:and.1")]
    public void IndependentEffectsRequireSomeChangeButResolveEveryPossiblePart(int damage, bool status, bool offered, bool statusFirst)
    {
        // "Individual effects connected by 'and' are not dependent on each other."
        var (world, runner, source) = Board();
        var hero = world.Seats[1].IdentityCard;
        hero.TakeDamage(damage);
        Card? physical = status ? Statuses.Give(world, hero, Statuses.Tough) : null;
        var actions = runner.Actions(world, 0).Where(item => item.Card == source.ObjectId).ToArray();
        Assert.Equal(offered ? 1 : 0, actions.Length);
        if (!offered) return;
        var events = runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions), [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal([hero.ObjectId], prompt.Affordances.Select(item => item.AnchorId));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(hero.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(Assert.Single(prompt.Affordances).Id, statusFirst ? [1, 0] : [0, 1]), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        if (status)
        {
            Assert.Equal([physical!.ObjectId], prompt!.Affordances.Select(item => item.AnchorId));
            Assert.False(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(physical.ObjectId), events);
        }
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(Math.Max(0, damage - 1), hero.Damage);
        Assert.Equal(2, source.Tokens["c_medical"]);
        Assert.False(source.Ready);
        if (status) Assert.Equal(DeckType.RemovedArea, physical!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    internal static (World World, AbilityRunner Runner, Card Source) Board(int counters = 3, int controller = 0)
    {
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var source = world.CreateCard("09019", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(controller), cardOwner: 0));
        source.PlaceTokens("c_medical", counters);
        return (world, runner, source);
    }
}
