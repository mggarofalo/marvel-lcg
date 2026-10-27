using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RonanJudgeDefeatReceiptTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:player-elimination.5.1")]
    public void IdentityDefeatRetainsItsReceiptBeforeEliminationFinishesTheAttack()
    {
        // "The attack ends immediately" when the attacked player is eliminated.
        // Printed 90002 still answers the friendly character's defeat.
        var f = new RonanJudgeProbeFixture();
        var identity = f.World.Seats[1].IdentityCard;
        identity.TakeDamage(8);
        f.AttackUntilResponse(null);
        var defeat = Assert.Single(f.World.Agenda.Occurrence!.Defeats);
        Assert.Equal(identity.ObjectId, defeat.Card);
        Assert.Equal(1, defeat.Controller);
        Assert.True(f.World.Seats[1].Eliminated);
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    public void OverkillCanDefeatBothAllyAndIdentityWithOneResponseForEach()
    {
        // Excess damage "is considered to be damage from an attack".
        var f = new RonanJudgeProbeFixture();
        var ally = f.Ally(1, true);
        f.World.Seats[1].IdentityCard.TakeDamage(9);
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: 1, Affects: f.Villain.ObjectId));
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "overkill", Amount: 1, Affects: f.Villain.ObjectId));
        f.AttackUntilResponse(ally);
        Assert.Equal(2, f.World.Agenda.Occurrence!.Defeats.Count);
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:indirect-damage.3")]
    [Rule("rr:triggering-condition.1")]
    public void IndirectAttackDefeatsRetainSeparateFriendlyRecipients()
    {
        // Assigned indirect damage is "dealt simultaneously"; each defeat is
        // one occurrence of 90002's triggering condition.
        var f = new RonanJudgeProbeFixture();
        var first = f.Ally(1, true);
        var second = f.Ally(1, true);
        first.TakeDamage(1);
        second.TakeDamage(1);
        f.AttackUntilResponse(null, [first.ObjectId, second.ObjectId]);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId },
            f.World.Agenda.Occurrence!.Defeats.Select(item => item.Card));
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    public void ReplacedDefeatHasDamageButNoDefeatResponse()
    {
        // A changed imminent condition "is no longer valid".
        var f = new RonanJudgeProbeFixture(extraCards: """
            {"cards":[{"card":"01041","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"heal":{"card":"this","amount":{"damageOn":"this"}}}
            }]}]}
            """);
        var ally = f.Ally(1, true);
        f.AttackUntilResponse(ally);
        Assert.Empty(f.World.Agenda.Occurrence!.Defeats);
        Assert.Single(f.World.Agenda.Occurrence.ReceivedAttackDamage);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        f.Finish();
    }

    [Fact]
    [Rule("rr:triggering-condition.2")]
    public void NestedNonattackDefeatDoesNotJoinTheEnemyAttack()
    {
        // Only "a single attack causing" the defeat shares its response window.
        var f = new RonanJudgeProbeFixture(extraCards: """
            {"cards":[{"card":"01041","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"dealDamage":{"cards":{"titled":"Black Cat"},"amount":2}}
            }]}]}
            """);
        var unrelated = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        var ally = f.Ally(1, true);
        f.AttackUntilResponse(ally);
        Assert.Equal(DeckType.DiscardPile, unrelated.Area.Type);
        Assert.Equal(ally.ObjectId, Assert.Single(f.World.Agenda.Occurrence!.Defeats).Card);
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void PreventedAttackHasNeitherDamageNorDefeatReceipt()
    {
        // A character whose Tough absorbs the damage "is not considered to have taken damage".
        var f = new RonanJudgeProbeFixture();
        var ally = f.Ally(1, true);
        Statuses.Give(f.World, ally, Statuses.Tough);
        f.AttackUntilResponse(ally);
        Assert.Empty(f.World.Agenda.Occurrence!.Defeats);
        Assert.Empty(f.World.Agenda.Occurrence.ReceivedAttackDamage);
        Assert.False(f.World.Agenda.Occurrence.DamageFromAttack);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
    }

    [Fact]
    [Rule("rr:stun-stunned.1")]
    public void CancelledAttackCannotReuseAnEarlierDefeatReceipt()
    {
        // Stunned discards its status "instead of attacking".
        var f = new RonanJudgeProbeFixture();
        f.AttackUntilResponse(f.Ally(1, true));
        f.Finish();
        Statuses.Give(f.World, f.Villain, Statuses.Stunned);
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish();
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:boost-boost-icon.3")]
    public void BoostAbilityDefeatIsNotDefeatByTheEnemyAttack()
    {
        // Boost damage "is not considered to be damage dealt by the activation".
        var f = new RonanJudgeProbeFixture(extraCards: """
            {"cards":[{"card":"01098","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"dealDamage":{"cards":{"titled":"Shuri"},"amount":2}}
            }]}]}
            """);
        var ally = f.Ally(1, true);
        f.AttackUntilResponse(ally);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Empty(f.World.Agenda.Occurrence!.Defeats);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
    }

    [Fact]
    [Rule("rr:retaliate-x.1")]
    public void EnemyRetaliationDoesNotCountAsAnEnemyAttackDefeat()
    {
        // Retaliate is a "Forced Response" that deals damage to the attacker.
        var f = new RonanJudgeProbeFixture("WhenDamageDealt");
        var identity = f.World.Seats[1].IdentityCard;
        identity.TakeDamage(9);
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "retaliate", Amount: 1, Affects: f.Villain.ObjectId));
        DamageAttacks.Attack(f.World, f.World.Facts, identity, f.Villain, 1,
            "WhenAttackEnds", "Deal_Damage", f.Events);
        f.Finish();
        Assert.True(f.World.Seats[1].Eliminated);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }
}
