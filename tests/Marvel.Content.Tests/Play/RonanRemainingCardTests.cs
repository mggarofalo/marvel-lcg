using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RonanRemainingCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attach-to")]
    public void AccusedRevealAndBoostAttachToExactNonFirstIdentity(bool boost)
    {
        // "it must be attached to ... the specified game element as it enters play."
        var f = new RonanRemainingFixture();
        f.World.Seats[1].IdentityCard.TurnTo("01001b");
        var accused = boost ? f.Card("90003", DeckType.EncounterDeck) : f.Reveal("90003");
        if (boost) f.Activate(false);
        f.Finish();
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, accused.Area.Host);
        Assert.Equal(-1, accused.Owner);
        Assert.NotEqual(f.World.Seats[0].IdentityCard.ObjectId, accused.Area.Host);
        Assert.Single(f.Events.OfType<CardAttached>(), item => item.Card == accused.ObjectId);
    }

    [Fact]
    public void AccusedBonusBelongsToAttachedIdentityAndExpiresAfterEachAttack()
    {
        var f = new RonanRemainingFixture();
        f.Reveal("90003");
        f.Finish();
        f.Activate(true);
        f.Finish();
        Assert.Equal(3, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 2));
        f.Activate(true, 0);
        f.Finish();
        Assert.Equal(2, f.World.Seats[0].IdentityCard.Damage);
        f.Activate(true);
        f.Finish();
        Assert.Equal(6, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(3, f.Events.OfType<AttackCompleted>().Count());
    }

    [Fact]
    [Rule("rr:forced.1")]
    public void AccusedRemainsForOtherDefeatAndDiscardsAfterExactRonanDefeat()
    {
        // "Forced Response abilities must be resolved when their triggering conditions are met."
        var f = new RonanRemainingFixture();
        var accused = f.Reveal("90003");
        f.Finish();
        var other = f.Card("01101", DeckType.EngagedEnemiesArea);
        f.World.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 1));
        Agendas.Happening(f.World);
        Defeat.Character(f.World, f.World.Facts, other, "test", f.Events);
        f.Finish();
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, accused.Area.Host);
        var ronan = f.Card("90001", DeckType.EngagedEnemiesArea);
        f.World.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 1));
        Agendas.Happening(f.World);
        Defeat.Character(f.World, f.World.Facts, ronan, "test", f.Events);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, accused.Area.Type);
    }

    [Fact]
    public void OpposeDealsOnlyExactMatchingDiscardsToRevealingPlayerInOrder()
    {
        var f = new RonanRemainingFixture();
        var lower = f.Card("90001", DeckType.EncounterDeck);
        var no = f.Card("01101", DeckType.EncounterDeck);
        var match = f.Card("90003", DeckType.EncounterDeck);
        var no2 = f.Card("01101", DeckType.EncounterDeck);
        var top = f.Card("90004", DeckType.EncounterDeck);
        var effect = f.Reveal("90005");
        f.Finish();
        Assert.Equal(new[] { top.ObjectId, match.ObjectId, lower.ObjectId },
            f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Select(card => card.ObjectId));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, no.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, no2.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, effect.Area.Type);
        Assert.All(new[] { top, match, lower }, card => Assert.False(card.FaceUp));
        Assert.Equal(-1, match.Area.Host);
    }

    [Fact]
    public void OpposeOverkillEndsWithTheExactAttack()
    {
        var f = new RonanRemainingFixture();
        var ally = f.World.CreateCard("01041", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(1);
        f.Card("90005", DeckType.EncounterDeck);
        f.Activate(true);
        f.Finish(prompt => prompt.Asking == Question.Defender && prompt.Affordances.Any(option => option.AnchorId == ally.ObjectId)
            ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == ally.ObjectId).Id)
            : Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(2, f.World.Seats[1].IdentityCard.Damage);
        Assert.False(Keywords.Has(f.World, f.Villain, "overkill", f.World.Facts));
        f.Activate(true);
        f.Finish();
        Assert.Equal(4, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
    }

}
