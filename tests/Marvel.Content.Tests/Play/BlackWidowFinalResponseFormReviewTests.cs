using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowFinalResponseFormReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.13")]
    public void CounterattackRequiresItsControllersHeroForm(bool alterEgo)
    {
        // A bold trigger containing "Hero" can only be used "in the specified form".
        // The fixture schedules an explicit attack, independent of normal activation selection.
        var f = new CounterattackReceiptFixture();
        if (alterEgo) f.Hero.TurnTo("08001b");
        f.Attack();
        Assert.Equal(alterEgo ? 0 : 1, f.Finish(use: false));
        Assert.Equal(2, f.Hero.Damage);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
        Assert.Single(f.Events.OfType<AttackCompleted>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.13")]
    public void RapidResponseRequiresItsControllersHeroForm(bool alterEgo)
    {
        // A bold trigger containing "Hero" can only be used "in the specified form".
        var f = new RapidResponseFixture();
        if (alterEgo) f.World.Seats[0].IdentityCard.TurnTo("08001b");
        DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.World.TheCardIn(DeckType.VillainArea)!, f.Ally, 3, "Fixture", "Damage"), f.Events);
        var prompt = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events);
        if (alterEgo) Assert.Null(prompt);
        else
        {
            Assert.NotNull(prompt);
            Assert.Equal(0, prompt.Player);
            Assert.Contains(prompt.Affordances, offer => offer.AnchorId == f.Rapid.ObjectId);
            Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt, Decision.Decline, f.Events);
            Assert.Null(Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events));
        }
        Assert.Equal(DeckType.DiscardPile, f.Ally.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, f.Rapid.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }
}
