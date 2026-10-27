using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual 90002 on nonpublic prepared boards; no modular-set admission.</summary>
public sealed class RonanJudgeActualCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevealAndBoostEnterWithPrintedThreatAndSurviveCleanup(bool boost)
    {
        var f = new RonanRemainingFixture();
        f.World.Seats[1].IdentityCard.TurnTo("01001b");
        var judge = boost ? f.Card("90002", DeckType.EncounterDeck) : f.Reveal("90002");
        if (boost) f.Activate(false);
        f.Finish();
        Assert.Equal(DeckType.SideSchemesArea, judge.Area.Type);
        Assert.Equal(6, judge.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(boost ? 1 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.DoesNotContain(judge, f.World.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [Rule("rr:friendly")]
    public void FriendlyAllyDefeatUsesActualBookAcrossControllers(int controller, bool lethal)
    {
        // Friendly "refers to cards the players control."
        var f = new RonanRemainingFixture();
        f.Reveal("90002");
        f.Finish();
        var ally = f.World.CreateCard("01041", f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(controller), cardOwner: controller));
        if (lethal) ally.TakeDamage(1);
        f.Activate(true);
        f.Finish(prompt => prompt.Asking == Question.Defender
            && prompt.Affordances.Any(option => option.AnchorId == ally.ObjectId)
                ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == ally.ObjectId).Id)
                : Decision.Decline);
        Assert.Equal(lethal ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(lethal ? 2 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(f.Events.OfType<AttackCompleted>());
    }

    [Fact]
    [Rule("rr:friendly")]
    public void AnotherPlayersDefendingHeroQualifiesAfterElimination()
    {
        // Friendly "refers to cards the players control", including the helping hero.
        var f = new RonanJudgeProbeFixture(observerPlayer: 1);
        var identity = f.World.Seats[0].IdentityCard;
        identity.TakeDamage(9);
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: 4, Affects: f.Villain.ObjectId));
        f.AttackUntilResponse(identity);
        Assert.True(f.World.Seats[0].Eliminated);
        Assert.False(f.World.Seats[1].Eliminated);
        Assert.Equal(identity.ObjectId, Assert.Single(f.World.Agenda.Occurrence!.Defeats).Card);
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        Assert.Single(f.Events.OfType<AttackCompleted>());
    }

    [Fact]
    [Rule("rr:overkill.2")]
    [Rule("rr:triggering-condition.1")]
    public void ForeignAllyOverkillDefeatsItsAlterEgoControllerOnceEach()
    {
        // Overkill to an identity "is considered damage from an attack";
        // each response triggers "once per occurrence of its triggering condition."
        var f = new RonanJudgeProbeFixture(observerPlayer: 1);
        var ally = f.Ally(0, true);
        var identity = f.World.Seats[0].IdentityCard;
        identity.TurnTo("01001b");
        identity.TakeDamage(9);
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: 1, Affects: f.Villain.ObjectId));
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "overkill", Amount: 1, Affects: f.Villain.ObjectId));
        f.AttackUntilResponse(ally);
        Assert.True(f.World.Seats[0].Eliminated);
        Assert.Equal(new[] { ally.ObjectId, identity.ObjectId },
            f.World.Agenda.Occurrence!.Defeats.Select(defeat => defeat.Card));
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:friendly")]
    public void EnemyDefeatByAttackCannotSatisfyFriendlyRecipient()
    {
        // Friendly "refers to cards the players control"; an enemy is excluded.
        // This synthetic attack isolates the actor/recipient guard with a real damage procedure.
        var f = new RonanJudgeProbeFixture(observerPlayer: 1);
        var enemy = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(1)));
        DamageAttacks.Attack(f.World, f.World.Facts, f.Villain, enemy, 3,
            "WhenAttackEnds", "Deal_Damage", f.Events);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }
}
