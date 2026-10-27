using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkStarterTimingReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:consequential-damage.1")]
    [Rule("rr:attack-player-ability-type.step.7")]
    public void BrawnRespondsBeforeLethalConsequentialDamageButNotAfterLethalRetaliate(bool retaliate)
    {
        // Consequential damage follows "abilities ... triggered by the ally attacking";
        // forced abilities "such as the retaliate keyword" precede optional responses.
        var f = new HulkStarterFixture();
        var ally = f.World.CreateCard("10011", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.TakeDamage(4);
        if (retaliate) f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1, Affects: f.Villain.ObjectId));
        int responses = 0;
        long threat = f.Scheme.Tokens["k_threat"];
        AllyBasicPowers.AllyPower(f.World, f.World.Facts, ally, f.Villain, BasicPowers.AttackVerb, f.Events);
        f.Finish(prompt =>
        {
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == ally.ObjectId);
            if (offer is null) return null;
            responses++;
            Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
            Assert.Equal(4, ally.Damage);
            return new Decision(offer.Id, [f.Scheme.ObjectId], []);
        });
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, ally.Area.PlayArea.Player);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(retaliate ? 0 : 1, responses);
        Assert.Equal(threat - (retaliate ? 0 : 1), f.Scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SentryPutIntoPlayUsesNewControllerAndControlTransferDoesNotReenter(bool alterEgo)
    {
        // Actual Sentry: "After Sentry enters play under your control, deal yourself 1 encounter card."
        var f = new HulkStarterFixture();
        if (alterEgo) f.Hero.TurnTo("01001b");
        var sentry = f.World.CreateCard("10012", f.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var top = f.World.AreaOf(DeckType.EncounterDeck).Cards[^1];
        CardPlay.PutAllyIntoPlay(f.World, f.World.Facts, f.Runner, sentry, 1, "review", f.Events);
        f.Finish();
        Assert.Equal(1, sentry.Area.PlayArea.Player);
        Assert.Equal(0, sentry.Owner);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, top.Area.Type);
        Assert.Equal(1, top.Area.PlayArea.Player);
        Assert.False(top.FaceUp);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        int incarnation = sentry.Incarnation;
        World.MoveToTop(sentry, f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        f.Finish();
        Assert.Equal(incarnation, sentry.Incarnation);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [Rule("rr:attack-enemy-activation.step.2")]
    [Rule("rr:you-your.2")]
    public void YoullPayFollowsHelpingAllysControllerAndOnlyItsIdentityOverkill(bool overkill, int removed)
    {
        // The defending player "becomes the target player"; damage "you" take is
        // applied "to the hit point dial of their identity", excluding ally damage.
        var f = new HulkStarterFixture();
        var ally = f.World.CreateCard("01020", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.TakeDamage(2);
        if (overkill) f.World.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1, Affects: f.Villain.ObjectId));
        var source = f.Held("10016");
        var other = f.World.CreateCard("10016", f.World.Seats[0].Hand);
        var payment = f.Held("10002");
        long threat = f.Scheme.Tokens["k_threat"];
        int responses = 0;
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: f.Villain.ObjectId, Seat: 0));
        f.Finish(prompt =>
        {
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == other.ObjectId);
            if (prompt.Asking == Question.Defender && prompt.Affordances.Any(a => a.Id == ally.ObjectId))
                return Decision.Take(ally.ObjectId);
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == source.ObjectId);
            if (offer is null) return null;
            responses++;
            Assert.Equal(1, prompt.Player);
            return PaidDecision.Take(offer, [payment.ObjectId]) with { Targets = [f.Scheme.ObjectId] };
        });
        Assert.Equal(removed, responses);
        Assert.Equal(removed, f.Hero.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat - removed, f.Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Single(f.Events.OfType<AttackCompleted>());
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(4, 5)]
    public void ActualYoullPayAndActualShrinkApplyPrintedCapAfterBonus(int attack, int removed)
    {
        // FAQ 10016: Shrink modifies "that event removes", so "the maximum of 5 applies".
        var f = new HulkStarterFixture();
        var shrink = f.World.CreateCard("05011", f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", attack - 2, Affects: f.Villain.ObjectId));
        var source = f.Held("10016");
        var payment = f.Held("10002");
        long threat = f.Scheme.Tokens["k_threat"];
        int shrinks = 0;
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == source.ObjectId);
            if (offer is not null) return PaidDecision.Take(offer, [payment.ObjectId]) with { Targets = [f.Scheme.ObjectId] };
            var bonus = prompt.Affordances.SingleOrDefault(a => a.AnchorId == shrink.ObjectId);
            if (bonus is null) return null;
            shrinks++;
            return Decision.Take(bonus.Id);
        });
        Assert.Equal(1, shrinks);
        Assert.False(shrink.Ready);
        Assert.Equal(attack, f.Hero.Damage);
        Assert.Equal(threat - removed, f.Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
