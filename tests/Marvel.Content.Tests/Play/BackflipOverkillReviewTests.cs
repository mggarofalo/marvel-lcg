using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BackflipOverkillReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.3")]
    public void NoOptionalBackflipWhenExcessIsZeroOrToughAlreadyPreventsIt(bool tough)
    {
        // Excess is "damage ... beyond its hit points"; "Tough status cards" precede
        // abilities triggered "when [character] would take any amount of damage".
        var f = new BlackWidowPreventionFixture();
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        var ally = Ally(f, tough ? 2 : 1);
        if (tough) Statuses.Give(f.World, f.Hero, Statuses.Tough);
        BeginAttack(f, 1);
        f.FinishDefendingWith(ally, backflip);
        Assert.Equal(0, f.Hero.Damage);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Tough));
        Assert.Same(f.World.Seats[1].Hand, backflip.Area);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(2, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    [Rule("rr:defend-defense.4.7")]
    public void HelpingAllysControllerPreventsItsOwnSpillWithoutReplacingTheAllyDefender()
    {
        // Excess goes "to the identity of that ally's controller". When their ally
        // defends, "the player's identity does not become the defender".
        var f = new BlackWidowPreventionFixture();
        var ally = Ally(f, 2);
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        var otherBackflip = f.World.CreateCard("01003", f.World.Seats[0].Hand);
        BeginAttack(f, 0);
        Prompt? prevention = null;
        for (int count = 0; count < 15; count++)
        {
            var prompt = f.Work();
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == otherBackflip.ObjectId);
            if (prompt.Affordances.Any(a => a.AnchorId == backflip.ObjectId)) { prevention = prompt; break; }
            f.Answer(prompt, prompt.Asking == Question.Defender && prompt.Affordances.Any(a => a.Id == ally.ObjectId)
                ? Decision.Take(ally.ObjectId) : Decision.Decline);
        }
        Assert.NotNull(prevention);
        Assert.Equal(1, prevention.Player);
        Assert.Equal(f.Hero.ObjectId, f.World.Agenda.Occurrence!.Target);
        Assert.Equal(1, f.World.Agenda.Occurrence.DirectDamage!.Remaining);
        Assert.Equal(ally.ObjectId, f.World.Attack!.Defender);
        f.Use(prevention, backflip);
        f.Finish(backflip, otherBackflip);
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
        Assert.Same(f.World.Seats[0].Hand, otherBackflip.Area);
        var completed = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, completed.Target);
        Assert.Equal(ally.ObjectId, completed.Defender);
        Assert.Equal(2, completed.DamageDealt);
    }

    private static Card Ally(BlackWidowPreventionFixture f, long damage)
    {
        var ally = f.World.CreateCard("01020", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(damage);
        return ally;
    }

    private static void BeginAttack(BlackWidowPreventionFixture f, int seat)
    {
        f.World.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1, Affects: f.Villain.ObjectId));
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 0,
            Subject: f.Villain.ObjectId, Seat: seat));
    }
}
