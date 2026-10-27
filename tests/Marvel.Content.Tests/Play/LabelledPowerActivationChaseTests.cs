using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerActivationChaseTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ActualChaseThemDownRequiresTheFollowupsOwnDefeatAndRemovesTwoThreat(bool lethal, bool stunned)
    {
        // Printed 01052: "After your hero attacks and defeats an enemy, remove
        // 2 threat from a scheme." An enemy attack defeating a defending ally
        // does not satisfy the hero attack's condition.
        var f = new LabelledPowerActivationFixture(villain: false, includeChase: true);
        var scheme = f.World.CreateCard("01097a", f.World.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 4);
        var chase = f.World.CreateCard("01052", f.World.Seats[1].Hand);
        if (lethal) f.Enemy.TakeDamage(8);
        if (stunned) Statuses.Give(f.World, f.Enemy, Statuses.Stunned);
        f.Start();
        if (!stunned) f.Answer(Decision.Take(f.Ally.ObjectId));
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        Assert.DoesNotContain(f.World.Agenda.Occurrence!.Defeats, d => d.Card == f.Ally.ObjectId);
        Assert.Equal(stunned ? DeckType.AlliesArea : DeckType.DiscardPile, f.Ally.Area.Type);
        if (!lethal)
        {
            Assert.DoesNotContain(f.Pending.Affordances, a => a.AnchorId == chase.ObjectId);
            f.Finish();
            Assert.Equal(4, scheme.Tokens["k_threat"]);
            Assert.Contains(chase, f.World.Seats[1].Hand.Cards);
            return;
        }
        var response = Assert.Single(f.Pending.Affordances, a => a.AnchorId == chase.ObjectId);
        Assert.Equal(f.Enemy.ObjectId, Assert.Single(f.World.Agenda.Occurrence.Defeats).Card);
        Assert.Equal(1, f.Pending.Player);
        f.Answer(Decision.Take(response.Id));
        Assert.Equal(Question.Element, f.Pending!.Asking);
        f.Answer(Decision.Take(scheme.ObjectId));
        f.Finish();
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, chase.Area.Type);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
    }
}
