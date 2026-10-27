using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkBeatCopCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.1")]
    [Rule("rr:move.6")]
    public void StoredThreatDeterminesDamageAfterExhaustAndDiscardInEitherForm(bool alterEgo)
    {
        // A cost arrow distinguishes "pay cost resolve effect"; moved threat
        // "is considered to be removed from that scheme."
        var f = new HulkFinalCardsFixture();
        if (alterEgo) f.Hero.TurnTo("10001b");
        var source = f.Support();
        var other = f.Support();
        other.PlaceTokens("k_threat", 7);
        var minion = f.Minion();
        f.Start(source);
        f.Answer(Decision.Take(f.Scheme.ObjectId));
        f.Finish();
        Assert.Equal(3, f.Scheme.Tokens["k_threat"]);
        Assert.Equal(1, source.Tokens["k_threat"]);
        Assert.False(source.Ready);
        Assert.DoesNotContain(f.Runner.Actions(f.World, 1), a => a.Card == source.ObjectId);
        source.Refresh();
        f.Start(source);
        f.Answer(Decision.Take(f.Scheme.ObjectId));
        f.Finish();
        Assert.Equal(2, source.Tokens["k_threat"]);
        source.Refresh();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        Statuses.Give(f.World, f.Hero, Statuses.Confused);
        f.Start(source, 1);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.Element, f.Pending!.Asking);
        Assert.DoesNotContain(f.Pending.Affordances, a => a.Id == f.Villain.ObjectId);
        f.Answer(Decision.Take(minion.ObjectId));
        f.Finish();
        Assert.Equal(2, minion.Damage);
        Assert.Equal(7, other.Tokens["k_threat"]);
        Assert.True(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.True(Statuses.Has(f.World, f.Hero, Statuses.Confused));
        Assert.DoesNotContain(f.Events, e => e.Verb is "Attack" or "Thwart");
        Assert.Empty(f.Events.OfType<AttackBoundary>());
    }

    [Fact]
    [Rule("rr:cost.6")]
    public void DamageActionCannotPayWithoutThreatOrAnEligibleMinion()
    {
        // "An ability's cost cannot be paid" without a required valid target.
        var f = new HulkFinalCardsFixture();
        var source = f.Support();
        var minion = f.Minion();
        Assert.DoesNotContain(f.Runner.Actions(f.World, 1), a => a.Card == source.ObjectId && a.Ordinal == 1);
        source.PlaceTokens("k_threat", 2);
        Assert.Contains(f.Runner.Actions(f.World, 1), a => a.Card == source.ObjectId && a.Ordinal == 1);
        World.MoveToTop(minion, f.World.AreaOf(DeckType.EncounterDiscardPile));
        Assert.DoesNotContain(f.Runner.Actions(f.World, 1), a => a.Card == source.ObjectId && a.Ordinal == 1);
        Assert.True(source.Ready);
        Assert.Equal(2, source.Tokens["k_threat"]);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }
}
