using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerActivationContinuationTests
{
    [Fact]
    public void UninterruptedRepeatedDamageKeepsItsOrdinarySingularTargetWithoutDuplicateRecipients()
    {
        // One attack retains one recipient while both damage instructions contribute.
        string body = LabelledPowerActivationFixture.Body.Replace(
            "{\"enemyAttacks\":{\"enemies\":\"chosen\"}}",
            "{\"dealAttackDamage\":{\"cards\":\"chosen\",\"amount\":5}}", StringComparison.Ordinal);
        var f = new LabelledPowerActivationFixture(body: body);
        f.Start();
        Assert.Equal(10, f.Enemy.Damage);
        var occurrence = f.World.Agenda.Occurrence!;
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, occurrence.Actor);
        Assert.Equal(f.Enemy.ObjectId, occurrence.Target);
        Assert.True(occurrence.Is(Steps.DamageDealt));
        var recipient = Assert.Single(occurrence.Attacks);
        Assert.Equal(f.Enemy.ObjectId, recipient.Card);
        Assert.Equal(10, recipient.DamageTaken);
        Assert.Equal(0, recipient.ExcessDamage);
        f.Finish();
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.step.2")]
    [Rule("rr:labeled-ability.2")]
    public void DefenseAndBoostWaitInsideTheOriginalAttackUntilItsDamageAndEndWindow()
    {
        // "A player may choose to defend the attack"; the labeled ability is
        // "an attack made by that player's identity", including its suspension.
        var control = Run(probe: false);
        var probed = Run(probe: true);
        Assert.Equal(control, probed);
    }

    [Fact]
    [Rule("rr:overkill.3")]
    [Rule("rr:labeled-ability.2")]
    public void ResumedHeroAttackResponseReadsItsOwnDefeatedEnemyExcess()
    {
        // rr:overkill.3 counts the same excess as overkill. The nested enemy
        // attack finishes before the original labelled attack deals its damage.
        var f = new LabelledPowerActivationFixture(villain: false, includeMoment: true);
        var hero = f.World.Seats[1].IdentityCard;
        hero.TakeDamage(4);
        f.Enemy.TakeDamage(DamagePlacement.Health(f.World, f.World.Facts, f.Enemy) - 2);
        int incarnation = f.Enemy.Incarnation;
        var moment = f.World.CreateCard("12030", f.World.Seats[1].Hand);
        f.Start();
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        f.Answer(Decision.Take(f.Ally.ObjectId));
        if (f.Pending!.Asking == Question.Opportunity
            && f.Pending.Affordances.All(option => option.AnchorId != moment.ObjectId))
            f.Answer(Decision.Take(Assert.Single(f.Pending.Affordances).Id));
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        Assert.Contains(f.World.Agenda.Occurrence!.Defeats,
            defeat => defeat.Card == f.Enemy.ObjectId && defeat.Incarnation == incarnation);
        var response = Assert.Single(f.Pending.Affordances, option => option.AnchorId == moment.ObjectId);
        f.Answer(Decision.Take(response.Id));
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, f.Enemy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, moment.Area.Type);
        f.Finish();
    }

    private static string Run(bool probe)
    {
        var f = new LabelledPowerActivationFixture();
        var modifier = new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", 1,
            Affects: f.Source.ObjectId, Lasts: new Duration(Uses: 1));
        f.World.Effects.Register(modifier);
        f.Start();
        var outer = Assert.Single(f.Events.OfType<AttackBoundary>(), b => b.Started && !b.Identity.EnemyActivation);
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        Assert.Equal(0, f.Pending.Player);
        Assert.Equal(1, f.World.Attack!.Player);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Contains(modifier, f.World.Effects.Active());
        if (probe) AssertRejectedChoiceIsPure(f);
        f.Answer(Decision.Take(f.Ally.ObjectId));
        Assert.IsType<BoostFaceupOperation>(f.World.Agenda.Current!.Value.Operation);
        Assert.Equal(1, f.Pending!.Player);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Contains(modifier, f.World.Effects.Active());
        f.Answer(Decision.Take(Assert.Single(f.Pending.Affordances).Id));
        Assert.Equal(6, f.Enemy.Damage);
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        Assert.Equal(1, f.Pending.Player);
        Assert.DoesNotContain(modifier, f.World.Effects.Active());
        var receipt = Assert.Single(f.World.Agenda.Occurrence!.Attacks);
        Assert.Equal(f.Enemy.ObjectId, receipt.Card);
        Assert.Equal(f.Enemy.Incarnation, receipt.Incarnation);
        Assert.Equal(6, receipt.DamageTaken);
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, f.World.Agenda.Occurrence.Actor);
        Assert.Equal(f.Enemy.ObjectId, f.World.Agenda.Occurrence.Target);
        Assert.True(f.World.Agenda.Occurrence.Is(Steps.DamageDealt));
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Tokens.GetValueOrDefault("c_attack_seen"));
        Assert.DoesNotContain(f.Events.OfType<AttackBoundary>(), b => b.Identity == outer.Identity && !b.Started);
        var enemyAttack = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(f.Ally.ObjectId, enemyAttack.Target);
        Assert.Equal(f.Ally.ObjectId, enemyAttack.Defender);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        var damage = Assert.Single(f.Events.OfType<FieldSet>(), e => e.Card == f.Enemy.ObjectId && e.Field == "health");
        Assert.Equal(outer.Identity, damage.AttackIdentity);
        Assert.True(f.Events.IndexOf(enemyAttack) < f.Events.IndexOf(damage));
        f.Answer(Decision.Take(Assert.Single(f.Pending.Affordances).Id));
        f.Finish();
        Assert.Equal(1, f.World.Seats[1].IdentityCard.Tokens["c_boost_seen"]);
        Assert.Equal(1, f.World.Seats[1].IdentityCard.Tokens["c_attack_seen"]);
        Assert.Single(f.Events.OfType<AttackBoundary>(), b => b.Identity == outer.Identity && !b.Started);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        return f.World.Digest().Canonical() + "\n" + string.Join("\n", f.Events.Select(e =>
            System.Text.Json.JsonSerializer.Serialize(e, e.GetType())));
    }

    private static void AssertRejectedChoiceIsPure(LabelledPowerActivationFixture f)
    {
        var digest = f.World.Digest().Canonical();
        var random = f.World.Random.Generator.GetState();
        long words = f.World.Random.Generator.WordsConsumed;
        var step = f.World.Agenda.Current;
        var occurrence = f.World.Agenda.Occurrence;
        var attack = f.World.Attack;
        int events = f.Events.Count;
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World,
            LabelledPowerActivationFixture.Cards, f.Runner, f.Pending!, Decision.Take(f.Enemy.ObjectId), f.Events));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(random.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(step, f.World.Agenda.Current);
        Assert.Same(occurrence, f.World.Agenda.Occurrence);
        Assert.Equal(attack, f.World.Attack);
        Assert.Equal(events, f.Events.Count);
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void PreventedFollowupRetainsTheAttackedRecipientWithZeroActualDamage()
    {
        // A character whose tough card prevents damage "is not considered to have taken damage."
        var f = new LabelledPowerActivationFixture(villain: false);
        Statuses.Give(f.World, f.Enemy, Statuses.Tough);
        f.Start();
        for (int choice = 0; f.Pending?.Asking == Question.Defender && choice < 3; choice++)
            f.Answer(Decision.Decline);
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        var occurrence = f.World.Agenda.Occurrence!;
        var recipient = Assert.Single(occurrence.Attacks);
        Assert.Equal(f.Enemy.ObjectId, recipient.Card);
        Assert.Equal(f.Enemy.Incarnation, recipient.Incarnation);
        Assert.Equal(0, recipient.DamageTaken);
        Assert.False(occurrence.Is(Steps.DamageDealt));
        Assert.Equal(0, f.Enemy.Damage);
        Assert.False(Statuses.Has(f.World, f.Enemy, Statuses.Tough));
        f.Finish();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void DepartedEnemyCannotReceiveTheBoundFollowupEvenIfItsPhysicalCardReenters(bool reenter)
    {
        // "considered to be a new copy of that card" on leaving play.
        var f = new LabelledPowerActivationFixture(villain: false);
        f.Start();
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        int incarnation = f.Enemy.Incarnation;
        World.MoveToTop(f.Enemy, f.World.AreaOf(DeckType.EncounterDiscardPile));
        if (reenter) World.MoveToTop(f.Enemy,
            f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        if (reenter) Assert.NotEqual(incarnation, f.Enemy.Incarnation);
        f.Finish();
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        Assert.DoesNotContain(f.Events.OfType<FieldSet>(), e => e.Card == f.Enemy.ObjectId && e.Field == "health");
    }
}
