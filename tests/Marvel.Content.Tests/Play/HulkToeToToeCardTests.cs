using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkToeToToeCardTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.2")]
    public void DefendedVillainAttackAndActualBoostResponseFinishBeforeFiveDamage()
    {
        // "A player may choose to defend the attack" before the boost resolves.
        var f = new HulkFinalCardsFixture();
        var ally = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        var preparation = f.World.CreateCard("08024", f.World.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 1));
        var boost = f.World.CreateCard("01131", f.World.AreaOf(DeckType.EncounterDeck));
        var source = f.World.CreateCard("10015", f.World.Seats[1].Hand);
        var payment = f.World.CreateCard("01088", f.World.Seats[1].Hand);
        f.Start(source, 0, payment);
        f.Answer(Decision.Take(f.Villain.ObjectId));
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        Assert.Equal(0, f.Villain.Damage);
        f.Answer(Decision.Take(ally.ObjectId));
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        Assert.Equal(boost.ObjectId, f.World.Agenda.Occurrence!.Subject);
        Assert.Equal(0, f.Villain.Damage);
        var response = Assert.Single(f.Pending.Affordances, a => a.AnchorId == preparation.ObjectId);
        f.Answer(Decision.Take(response.Id));
        f.Finish();
        Assert.Equal(5, f.Villain.Damage);
        Assert.Equal(0, f.Hero.Damage);
        Assert.False(Statuses.Has(f.World, f.Villain, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, preparation.Area.Type);
        var activation = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, activation.Defender);
        var damage = Assert.Single(f.Events.OfType<FieldSet>(), e => e.Card == f.Villain.ObjectId && e.Field == "health");
        Assert.True(f.Events.IndexOf(activation) < f.Events.IndexOf(damage));
        var outer = Assert.Single(f.Events.OfType<AttackBoundary>(), e => e.Started && !e.Resumed && !e.Identity.EnemyActivation);
        Assert.Equal(outer.Identity, damage.AttackIdentity);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:stun-stunned.6")]
    [Rule("rr:labeled-ability.6")]
    public void PaidAttackRetainsChosenEnemyAndCancelsTheWholeAbilityForHeroStun(bool enemyStun, bool heroStun)
    {
        // Enemy stun discards its status "instead" of its attack; hero stun
        // cancels "the entire ability (except for its costs)".
        var f = new HulkFinalCardsFixture();
        var enemy = f.Minion();
        f.World.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect,
            "health", 10, Affects: enemy.ObjectId));
        if (enemyStun) Statuses.Give(f.World, enemy, Statuses.Stunned);
        if (heroStun) Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        var source = f.World.CreateCard("10015", f.World.Seats[1].Hand);
        var payment = f.World.CreateCard("01088", f.World.Seats[1].Hand);
        f.Start(source, 0, payment);
        if (f.Pending?.Asking == Question.Element) f.Answer(Decision.Take(enemy.ObjectId));
        f.Finish();
        Assert.Equal(heroStun ? 0 : 5, enemy.Damage);
        Assert.Equal(enemyStun || heroStun ? 0 : 2, f.Hero.Damage);
        Assert.Equal(0, f.Villain.Damage);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.False(Statuses.Has(f.World, enemy, Statuses.Stunned));
        Assert.Equal(enemyStun || heroStun ? 0 : 1, f.Events.OfType<AttackCompleted>().Count());
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.True(f.Hero.Ready);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:attack-player-ability-type.2")]
    public void ActualChaseThemDownConsumesOnlyThisAttacksLethalReceipt(bool enemyStun, bool survives)
    {
        // Resolving a labeled attack ability "is considered to attack the specified target."
        var f = new HulkFinalCardsFixture();
        var enemy = f.Minion();
        if (survives) f.World.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect,
            "health", 10, Affects: enemy.ObjectId));
        if (enemyStun) Statuses.Give(f.World, enemy, Statuses.Stunned);
        var source = f.World.CreateCard("10015", f.World.Seats[1].Hand);
        var payment = f.World.CreateCard("01088", f.World.Seats[1].Hand);
        var chase = f.World.CreateCard("01052", f.World.Seats[1].Hand);
        f.Start(source, 0, payment);
        f.Answer(Decision.Take(enemy.ObjectId));
        while (f.Pending?.Asking == Question.Defender) f.Answer(Decision.Decline);
        if (survives)
        {
            Assert.True(f.Pending is null || f.Pending.Affordances.All(a => a.AnchorId != chase.ObjectId));
            f.Finish();
            Assert.Equal(5, enemy.Damage);
            Assert.Equal(4, f.Scheme.Tokens["k_threat"]);
            Assert.Contains(chase, f.World.Seats[1].Hand.Cards);
            return;
        }
        Assert.Equal(Question.Opportunity, f.Pending!.Asking);
        var response = Assert.Single(f.Pending.Affordances, a => a.AnchorId == chase.ObjectId);
        var defeat = Assert.Single(f.World.Agenda.Occurrence!.Defeats);
        Assert.Equal(enemy.ObjectId, defeat.Card);
        Assert.Equal(1, defeat.By);
        f.Answer(Decision.Take(response.Id));
        f.Answer(Decision.Take(f.Scheme.ObjectId));
        f.Finish();
        Assert.Equal(2, f.Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, chase.Area.Type);
        Assert.Equal(0, f.Villain.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void OriginalChosenMinionCannotBeReplacedByItsReenteredCopy(bool reenter)
    {
        // A card that leaves play is "considered to be a new copy of that card."
        var f = new HulkFinalCardsFixture();
        var enemy = f.Minion();
        var source = f.World.CreateCard("10015", f.World.Seats[1].Hand);
        var payment = f.World.CreateCard("01088", f.World.Seats[1].Hand);
        f.Start(source, 0, payment);
        f.Answer(Decision.Take(enemy.ObjectId));
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        int incarnation = enemy.Incarnation;
        World.MoveToTop(enemy, f.World.AreaOf(DeckType.EncounterDiscardPile));
        if (reenter) World.MoveToTop(enemy, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.Finish();
        if (reenter) Assert.NotEqual(incarnation, enemy.Incarnation);
        Assert.Equal(0, enemy.Damage);
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
