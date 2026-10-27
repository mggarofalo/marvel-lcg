using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual 90004 on nonpublic prepared boards; no modular admission.</summary>
public sealed class RonanHammerActualCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation.2")]
    public void RevealActivatesRonanAgainstHisEngagedPlayerInEitherForm(bool hero)
    {
        // Minions attack the engaged hero or scheme against the engaged alter-ego.
        // Printed 90004: "activates against the player he is engaged with."
        var f = new RonanRemainingFixture();
        if (!hero) f.World.Seats[0].IdentityCard.TurnTo("01001b");
        var ronan = f.World.CreateCard("90001", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hammer = f.Reveal("90004", 1);
        f.Finish();
        Assert.Equal(hero ? 3 : 0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : 3, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(hero ? 1 : 0, f.Events.OfType<AttackCompleted>().Count());
        if (hero) Assert.Equal(ronan.ObjectId, Assert.Single(f.Events.OfType<AttackCompleted>()).Enemy);
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    [Rule("rr:surge.1")]
    public void AbsentRonanSurgesForRevealingPlayerAfterCleanup()
    {
        // Surge: "Deal yourself 1 facedown encounter card."
        var f = new RonanRemainingFixture();
        var absent = f.Card("90001", DeckType.EncounterDiscardPile);
        var expected = f.Card("01098", DeckType.EncounterDeck);
        var hammer = f.Reveal("90004", 1);
        f.Finish();
        Assert.Same(expected, Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards));
        Assert.False(expected.FaceUp);
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, absent.Area.Type);
        Assert.Empty(f.Events.OfType<AttackCompleted>());
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.2")]
    public void BoostDistinguishesEqualDamageAndDealsOnceToDistinctFirstPlayer(bool survives)
    {
        // "Only the ability text beneath the divider line is active" as a boost.
        // Printed 90004 conditions the deal on defeat, not positive attack damage.
        var f = new RonanRemainingFixture();
        var ally = Ally(f, survives);
        var hammer = f.Card("90004", DeckType.EncounterDeck);
        f.Activate(true);
        f.Finish(prompt => Defend(prompt, ally));
        Assert.Equal(5, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(survives ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        var dealt = f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards;
        Assert.Equal(survives ? 0 : 1, dealt.Count);
        Assert.All(dealt, card => Assert.False(card.FaceUp));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        f.Finish();
        Assert.Equal(survives ? 0 : 1, dealt.Count);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.3")]
    public void SchemeBoostDamageDefeatDoesNotQualifyTheActivation()
    {
        // Boost ability damage "is not considered to be damage dealt by the activation".
        var f = new RonanRemainingFixture();
        f.World.Seats[1].IdentityCard.TurnTo("01001b");
        var ally = Ally(f, false);
        ally.TakeDamage(2);
        var hammer = f.Card("90004", DeckType.EncounterDeck);
        var blast = f.Card("01154", DeckType.EncounterDeck);
        Attack.GiveAdditionalBoostCard(f.World, f.Villain, "test", f.Events);
        Assert.Equal(f.Villain.ObjectId, blast.Area.Host);
        f.Activate(false);
        f.Finish();
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Empty(f.Events.OfType<AttackCompleted>());
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    [Rule("rr:overkill.2")]
    public void AllyAndIdentityDefeatDealOnlyOneCardAndNextActivationDoesNotBorrowIt()
    {
        // Overkill excess "is considered damage from an attack".
        // Printed 90004 deals one card if a character is defeated, not one per defeat.
        var f = new RonanRemainingFixture();
        var ally = Ally(f, false);
        f.World.Seats[1].IdentityCard.TakeDamage(9);
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "overkill", Amount: 1, Affects: f.Villain.ObjectId));
        f.Card("90004", DeckType.EncounterDeck);
        f.Activate(true);
        f.Finish(prompt => Defend(prompt, ally));
        Assert.True(f.World.Seats[1].Eliminated);
        Assert.Equal(DeckType.RemovedArea, ally.Area.Type);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        var survivor = f.World.CreateCard("01083", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "health", Amount: 10, Affects: survivor.ObjectId));
        f.Card("90004", DeckType.EncounterDeck);
        f.Activate(true, 0);
        f.Finish(prompt => Defend(prompt, survivor));
        Assert.Equal(DeckType.AlliesArea, survivor.Area.Type);
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    private static Card Ally(RonanRemainingFixture f, bool survives)
    {
        var ally = f.World.CreateCard("01083", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        if (survives) f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "health", Amount: 10, Affects: ally.ObjectId));
        return ally;
    }

    private static Decision Defend(Prompt prompt, Card ally) => prompt.Asking == Question.Defender
        && prompt.Affordances.Any(option => option.AnchorId == ally.ObjectId)
            ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == ally.ObjectId).Id)
            : Decision.Decline;
}
