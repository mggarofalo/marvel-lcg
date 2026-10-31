using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.ScarletWitchBoards;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class ScarletWitchBoostCountTests
{
    private const string ChaosManipulation = "15027";

    [Fact]
    [Rule("rr:would")]
    public void TheCrestRaisesAChaosManipulationCountSoLuminousAttacks()
    {
        // rr:would: "establishes a higher timing priority for those abilities than
        // interrupts to the same triggering condition without the word 'would.'"
        // Ruling 310c4caa404d0f5a: Luminous's "2 or more boost icons" is a count
        // Chaos Control and the Crest can change.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var identity = world.Seats[0].IdentityCard;
        var crest = InPlay(world, Crest, DeckType.UpgradesArea);
        var luminous = world.CreateCard(Luminous, world.AreaOf(DeckType.EncounterDiscardPile));
        var top = Stack(world, OneBoost)[0];
        Reveal(world, runner, ChaosManipulation);
        var counts = new List<string>();
        var seen = Drive(world, runner, prompt =>
        {
            if (IsCount(world, prompt))
                counts.Add(string.Join(",", prompt.Affordances.Select(offer => world.Cards[offer.AnchorId].FaceId)));
            if (IsCount(world, prompt) && OfferOn(prompt, crest) is { } exhaust) return Decision.Take(exhaust.Id);
            if (prompt.Affordances.Count == 2 && prompt.Affordances.All(offer => offer.AnchorId == crest.ObjectId))
                return Decision.Take(prompt.Affordances[0].Id);
            return null;
        });
        // The would interrupt is offered alone first; the Crest follows in its own tier.
        Assert.Equal([Hero, Crest], counts.Take(2));
        Assert.Equal(DeckType.EncounterDiscardPile, top.Area.Type);
        Assert.False(crest.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, luminous.Area.Type);
        // Luminous attacks undefended for her printed 2.
        Assert.Equal(2, identity.Damage);
        Assert.NotEmpty(seen);
    }

    [Fact]
    [Rule("rr:replacement-effect.1")]
    public void ChaosControlCountsTheNextCardInsteadAndIsLimitedOncePerPhase()
    {
        // rr:replacement-effect.1: a replacement "replaces the resolution of a triggering condition".
        var runner = AuthoredCards.Runner();
        var world = Board();
        var identity = world.Seats[0].IdentityCard;
        var luminous = world.CreateCard(Luminous, world.AreaOf(DeckType.EncounterDiscardPile));
        var cards = Stack(world, ZeroBoost, TwoBoost, OneBoost);
        Reveal(world, runner, ChaosManipulation);
        int offered = 0;
        Drive(world, runner, prompt =>
        {
            if (IsCount(world, prompt) && OfferOn(prompt, identity) is { } chaos)
            {
                offered++;
                return Decision.Take(chaos.Id);
            }
            return null;
        });
        // Stampede (1) is replaced by Charge (2), so Luminous enters and attacks.
        Assert.All(cards[1..], card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Equal(DeckType.EngagedEnemiesArea, luminous.Area.Type);
        Assert.Equal(2, identity.Damage);
        // Luminous's own count discards Advance (0); Chaos Control is spent this phase.
        Assert.Equal(DeckType.EncounterDiscardPile, cards[0].Area.Type);
        Assert.Equal(1, offered);
    }

    [Fact]
    [Rule("rr:alter-ego-form.1")]
    public void AnUnobservedCountOpensNoWindow()
    {
        // rr:alter-ego-form.1: only the faceup side's abilities are active, so the
        // alter-ego has no Chaos Control and there is no Crest.
        var runner = AuthoredCards.Runner();
        var world = Board(AlterEgo);
        var luminous = world.CreateCard(Luminous, world.AreaOf(DeckType.EncounterDiscardPile));
        Stack(world, OneBoost);
        Reveal(world, runner, ChaosManipulation);
        var seen = Drive(world, runner, _ => null);
        Assert.DoesNotContain(seen, prompt => IsCount(world, prompt));
        Assert.Equal(DeckType.EngagedEnemiesArea, luminous.Area.Type);
        // One boost icon: Luminous does not activate.
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }
}
