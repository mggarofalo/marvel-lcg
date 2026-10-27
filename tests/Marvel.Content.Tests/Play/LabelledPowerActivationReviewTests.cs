using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerActivationReviewTests
{
    [Fact]
    public void OrdinaryDamageOnlyControlRetainsLethalDefeat()
    {
        // Engine receipt control: the same lethal followup without an enemy
        // activation suspension records defeat on the original attack window.
        var body = LabelledPowerActivationFixture.Body.Replace(
            "{\"enemyAttacks\":{\"enemies\":\"chosen\"}},", "", StringComparison.Ordinal);
        var f = new LabelledPowerActivationFixture(villain: false, body: body);
        f.Enemy.TakeDamage(8);
        int incarnation = f.Enemy.Incarnation;
        f.Start();
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        var defeat = Assert.Single(f.World.Agenda.Occurrence!.Defeats);
        Assert.Equal(f.Enemy.ObjectId, defeat.Card);
        Assert.Equal(incarnation, defeat.Incarnation);
        Assert.Equal(1, defeat.By);
        Assert.Equal("Attack", defeat.How);
        Assert.Equal(DeckType.EncounterDiscardPile, f.Enemy.Area.Type);
        f.Finish();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:retaliate-x.2")]
    public void FollowupSettlesBeforeRetaliateAndRetainsItsOriginalRecipient(bool lethal, bool tough)
    {
        // Retaliate is a "Forced Response" and its character "must be in play
        // after the attack resolves". This synthetic composition also checks
        // the engine's retained causal receipt after lethal departure.
        var f = new LabelledPowerActivationFixture(villain: false);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 2,
            Affects: f.Enemy.ObjectId));
        if (lethal) f.Enemy.TakeDamage(8);
        if (tough) Statuses.Give(f.World, f.Enemy, Statuses.Tough);
        int incarnation = f.Enemy.Incarnation;
        f.Start();
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        f.Answer(Decision.Take(f.Ally.ObjectId));
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        var occurrence = f.World.Agenda.Occurrence!;
        var receipt = Assert.Single(occurrence.Attacks);
        Assert.Equal(f.Enemy.ObjectId, receipt.Card);
        Assert.Equal(incarnation, receipt.Incarnation);
        Assert.Equal(tough ? 0 : 5, receipt.DamageTaken);
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, occurrence.Actor);
        Assert.Equal(f.Enemy.ObjectId, occurrence.Target);
        AssertDefeatReceipt(f, occurrence, incarnation, lethal);
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea,
            f.Enemy.Area.Type);
        Assert.Equal(lethal ? 0 : 2, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        var outer = Assert.Single(f.Events.OfType<AttackBoundary>(),
            e => e.Started && !e.Resumed && !e.Identity.EnemyActivation);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        AssertDamageEvents(f, outer, tough);
        f.Answer(Decision.Take(Assert.Single(f.Pending.Affordances).Id));
        f.Finish();
        Assert.Equal(1, f.World.Seats[1].IdentityCard.Tokens["c_attack_seen"]);
        Assert.Single(f.Events.OfType<AttackBoundary>(), e => !e.Started && e.Identity == outer.Identity);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
    }

    private static void AssertDefeatReceipt(LabelledPowerActivationFixture f,
        Occurrence occurrence, int incarnation, bool lethal)
    {
        if (!lethal) { Assert.Empty(occurrence.Defeats); return; }
        var defeat = Assert.Single(occurrence.Defeats);
        Assert.Equal(f.Enemy.ObjectId, defeat.Card);
        Assert.Equal(incarnation, defeat.Incarnation);
        Assert.Equal(1, defeat.By);
        Assert.Equal("Attack", defeat.How);
    }

    private static void AssertDamageEvents(LabelledPowerActivationFixture f,
        AttackBoundary outer, bool tough)
    {
        var followups = f.Events.OfType<FieldSet>().Where(
            e => e.Card == f.Enemy.ObjectId && e.Field == "health").ToArray();
        if (tough) Assert.Empty(followups);
        else Assert.Equal(outer.Identity, Assert.Single(followups).AttackIdentity);
    }
}
