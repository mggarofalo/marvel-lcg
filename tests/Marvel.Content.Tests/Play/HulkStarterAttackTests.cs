using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkStarterAttackTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DropKickUsesActualPhysicalPaymentAndDrawDoesNotDependOnStunning(bool mixed, bool alreadyStunned)
    {
        var f = new HulkStarterFixture();
        if (alreadyStunned) Statuses.Give(f.World, f.Villain, Statuses.Stunned);
        var two = f.Held(mixed ? "01089" : "10022");
        var one = f.Held("10002");
        int deck = f.World.Seats[1].Deck.Cards.Count;
        var source = f.PlayEvent("10014", f.Villain, two, one);
        f.Finish();
        Assert.Equal(4, f.Villain.Damage);
        Assert.Equal(!mixed || alreadyStunned, Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(deck - (mixed ? 0 : 1), f.World.Seats[1].Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(2, 0, false, 2)]
    [InlineData(7, 0, false, 5)]
    [InlineData(7, 5, false, 2)]
    [InlineData(2, 0, true, 0)]
    [Rule("rr:target.2")]
    [Rule("rr:target.3")]
    public void YoullPayUsesActualIdentityDamageFromCompletedVillainAttack(int attack, int prevention, bool tough, int removed)
    {
        // A target is valid only "if any part of that ability can affect that target".
        // Zero attack damage supplies no threat removal and no valid scheme target.
        var f = new HulkStarterFixture();
        f.Hero.TakeDamage(3);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", attack - 2, Affects: f.Villain.ObjectId));
        if (prevention > 0) f.World.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", prevention,
            Affects: f.Hero.ObjectId, Lasts: Duration.NextUses(1)));
        if (tough) Statuses.Give(f.World, f.Hero, Statuses.Tough);
        var source = f.Held("10016");
        var pay = f.Held("10002");
        long threat = f.Scheme.Tokens["k_threat"];
        int offered = 0;
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish(prompt =>
        {
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            if (offer is null) return null;
            offered++;
            Assert.Equal(1, prompt.Player);
            var paid = PaidDecision.Take(offer, [pay.ObjectId]);
            return paid with { Targets = [f.Scheme.ObjectId] };
        });
        Assert.Equal(threat - removed, f.Scheme.Tokens["k_threat"]);
        Assert.Equal(removed > 0 ? 1 : 0, offered);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(3 + (tough ? 0 : attack - prevention), f.Hero.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RescueIsHeroThwartAndConfusedCancelsItsRemoval(bool confused)
    {
        var f = new HulkStarterFixture();
        long threat = f.Scheme.Tokens["k_threat"];
        if (confused) Statuses.Give(f.World, f.Hero, Statuses.Confused);
        f.PlayEvent("10019", f.Scheme, f.Held("10022"));
        f.Finish();
        Assert.Equal(threat - (confused ? 0 : 2), f.Scheme.Tokens["k_threat"]);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Confused));
    }
}
