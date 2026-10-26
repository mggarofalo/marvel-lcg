using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreventionBoostTests
{
    [Theory]
    [InlineData("01104")]
    [InlineData("01121")]
    [Rule("rr:boost-boost-icon.1")]
    public void ZeroAndStarOnlyBoostsDoNotOfferAttacrobatics(string face)
    {
        // "A star icon is not itself considered a boost icon."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        var boost = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDeck));
        Attack(f);
        f.Finish(source);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(0, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(face == "01121" ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Theory]
    [InlineData(0, 1, 3)]
    [InlineData(2, 0, 4)]
    [Rule("rr:amplify-icon.1")]
    public void AttacrobaticsCountsAmplifyAndConstantIcons(int modifier, int amplify, int expected)
    {
        // Amplify: "Each boost card gains [boost]."
        // Ruling 1ea208998735ef4b puts constants and amplify before cancellation.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        var boost = f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "boost_const", modifier, Affects: boost.ObjectId));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "amplify", amplify, Affects: f.Villain.ObjectId));
        Attack(f);
        var prompt = f.Find(source);
        Assert.Equal(expected, f.World.Agenda.Occurrence!.BoostIcons!.Remaining);
        f.Use(prompt, source);
        f.Finish();
        Assert.Equal(expected, f.Villain.Damage);
        Assert.Equal(0, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.4")]
    public void AttacrobaticsDoesNotCancelTheNextPhysicalBoost(bool decline)
    {
        // Additional boost cards are "cumulative"; cancellation binds one physical card.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        var second = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EncounterDeck));
        var first = f.World.CreateCard("01102", f.World.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        Attack(f);
        var prompt = f.Find(source);
        Assert.Equal(first.ObjectId, f.World.Agenda.Occurrence!.Subject);
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, source);
        if (decline)
        {
            var next = f.Find(source);
            Assert.Equal(second.ObjectId, f.World.Agenda.Occurrence!.Subject);
            f.Answer(next, Decision.Decline);
        }
        f.Finish();
        Assert.Equal(decline ? new long[] { 2, 1 } : [0, 1], f.Events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.Equal(decline ? 0 : 2, f.Villain.Damage);
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Fact]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:resolve.7")]
    public void StunnedAttacrobaticsPaysCostButCancelsNoIconsAndHasNoCompletion()
    {
        // "The entire ability (except for its costs) is canceled."
        // "If all of the effects of an ability are canceled", it is not resolved.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        var suit = f.Upgrade("08009");
        f.Hero.Exhaust();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        Attack(f);
        f.Use(f.Find(source), source);
        f.Finish(suit, f.Hero);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(2, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(0, f.Villain.Damage);
        Assert.True(suit.Ready);
        Assert.False(f.Hero.Ready);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Fact]
    public void AttacrobaticsCannotCancelIconsAddedByTheActualBoostAbility()
    {
        // Printed I See You adds one icon during Boost if a Goblin minion is engaged.
        // Ruling 1ea208998735ef4b places that addition after faceup cancellation.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        f.World.CreateCard("02023", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        f.World.CreateCard("02030", f.World.AreaOf(DeckType.EncounterDeck));
        Attack(f);
        f.Use(f.Find(source), source);
        f.Finish();
        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(1, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(3, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Fact]
    public void AlterEgoAttacrobaticsCannotCancelPositiveIcons()
    {
        var f = new BlackWidowPreventionFixture(false);
        var source = f.Upgrade("08006");
        f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        Attack(f);
        f.Finish(source);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(2, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    private static void Attack(BlackWidowPreventionFixture f) =>
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 0));
}
