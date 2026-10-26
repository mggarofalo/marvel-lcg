using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RapidResponseEntryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:enters-play")]
    [Rule("rr:leaves-play.1")]
    public void EntryResponseFinishesBeforeDamageAndCannotDamageAReenteredCopy(bool reenter)
    {
        // Entry is "any time when a card transitions from an out-of-play area
        // into play"; departure makes "a new copy of the card".
        var f = new RapidResponseFixture(entryChoice: true);
        f.Answer(f.Defeat(), f.Rapid.ObjectId);
        var entry = f.Work();
        Assert.Equal(DeckType.AlliesArea, f.Ally.Area.Type);
        Assert.Equal(0, f.Ally.Damage);
        Assert.Equal(4, f.Ally.Tokens["c_arrow"]);
        Assert.DoesNotContain(f.World.Agenda.Outstanding, step => step.Operation is PreparationAbilityResolvedOperation);
        f.Answer(entry, f.Ally.ObjectId);
        var choice = f.Work();
        if (reenter)
        {
            var area = f.Ally.Area;
            World.MoveToTop(f.Ally, f.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(f.Ally, area);
        }
        Sequence.Answer(f.World, f.World.Facts, f.Runner, choice, Decision.Take(choice.Affordances[0].Id), f.Events);
        var preparation = f.Work();
        Assert.NotNull(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal(reenter ? 0 : 1, f.Ally.Damage);
        Assert.Contains(preparation.Affordances, a => a.AnchorId == f.World.Seats[0].IdentityCard.ObjectId);
    }

    [Fact]
    [Rule("rr:damage.step.8")]
    public void OneHpReturnIsDefeatedAgainBeforePreparationCompletion()
    {
        // Step 8: "Discarding of a defeated character."
        var f = new RapidResponseFixture(oneHp: true);
        f.Answer(f.Defeat(), f.Rapid.ObjectId);
        f.Work();
        Assert.Equal(DeckType.DiscardPile, f.Ally.Area.Type);
        Assert.Equal(2, f.Ally.Incarnation);
        Assert.Equal(0, f.Ally.Damage);
        Assert.Equal(0, f.Ally.Tokens["c_arrow"]);
        Assert.NotNull(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal(DeckType.DiscardPile, f.Rapid.Area.Type);
    }
}
