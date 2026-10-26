using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored research rows, independently challenged at boundary conditions.
public sealed class MutagenFinalIndependentReviewTests : CoreCardTestBase
{
    [Fact]
    public void ReinforcementsWithNoLiveGoblinMinionsKeepsOnlyPrintedThreat()
    {
        var f = Create();
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("02024", f.World.AreaOf(DeckType.EncounterDiscardPile));
        var scheme = f.Reveal("02026", 1);
        f.Finish();
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData("02014", true, 4, 1, 2)]
    [InlineData("02014", false, 0, 3, 1)]
    [InlineData("02016", true, 7, 2, 3)]
    [InlineData("02016", false, 0, 6, 2)]
    public void DeathFromAboveUsesStageOneAndThreeInBothForms(string stage, bool hero, int damage, int threat, int basePower)
    {
        // Printed 02029 adds the villain's stage number to this activation.
        var f = Create(stage, hero);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) f.World.CreateCard("02029", deck);
        f.Reveal("02029", 1);
        f.Finish();
        Assert.Equal(damage, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(threat, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(basePower, StateFields.Modified(f.World, f.Villain, hero ? "attack" : "scheme", Cards, 2));
        Assert.Null(f.World.Activation);
    }

    [Fact]
    public void OverrunKeepsExactPlayerReceiptsThroughFinalCardReset()
    {
        var f = Create();
        f.World.FirstPlayer = 1;
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        var outside = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EncounterDiscardPile));
        var zeroSecond = f.World.CreateCard("02024", deck);
        var zeroFirst = f.World.CreateCard("02024", deck);
        var oneSecond = f.World.CreateCard("02024", deck);
        var oneFirst = f.World.CreateCard("02024", deck);
        var scheme = f.Reveal("02028", 1);
        f.Finish();
        Threat.Remove(f.World, Cards, f.World.ThreatAbilities, new(scheme, 2, "test", "Remove_Threat")
            { RecordDefeatOn = new Occurrence(1, []) }, []);
        var events = f.Finish();
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.All(new[] { oneFirst, oneSecond }, card => Assert.Equal(PlayArea.Of(1), card.Area.PlayArea));
        Assert.All(new[] { zeroFirst, zeroSecond }, card => Assert.Equal(PlayArea.Of(0), card.Area.PlayArea));
        Assert.All(new[] { oneFirst, oneSecond, zeroFirst, zeroSecond }, card =>
            Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Assert.NotEqual(DeckType.EngagedEnemiesArea, outside.Area.Type);
        var entries = events.OfType<CardsMoved>().Where(e => e.Verb == "Put_Into_Play")
            .SelectMany(e => e.Cards).Select(card => card.Card);
        Assert.Equal(new[] { oneFirst.ObjectId, oneSecond.ObjectId, zeroFirst.ObjectId, zeroSecond.ObjectId }, entries);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }

    [Fact]
    public void WickedAmbitionsResumesOrderedChoicesAcrossImmediateReset()
    {
        var f = Create();
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        var outside = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EncounterDiscardPile));
        var second = f.World.CreateCard("02024", deck);
        var first = f.World.CreateCard("02024", deck);
        f.Reveal("02032", 1);
        int choices = 0;
        f.Finish(prompt =>
        {
            Assert.Equal(1, prompt.Player);
            if (choices++ == 0)
            {
                Assert.Same(deck, second.Area);
                Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
                Assert.Equal(0, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
            }
            else
            {
                Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
                Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
            }
            return Decision.Take(prompt.Affordances[1].Id);
        });
        Assert.Equal(2, choices);
        Assert.All(new[] { first, second }, card =>
        {
            Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type);
            Assert.Equal(PlayArea.Of(1), card.Area.PlayArea);
        });
        Assert.NotEqual(DeckType.EngagedEnemiesArea, outside.Area.Type);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("02014", 2)]
    [InlineData("02016", 6)]
    public void WickedAmbitionsMillsFullStageCountWithoutMatchingGoblin(string stage, int count)
    {
        var f = Create(stage);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 6; i++) f.World.CreateCard("01101", deck);
        f.World.CreateCard("02014", deck);
        f.Reveal("02032", 1);
        f.Finish(); // Any unintended mandatory choice fails without an answer callback.
        Assert.Equal(7 - count, deck.Cards.Count);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Empty(f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
    }

    [Fact]
    public void TwoISeeYouBoostCopiesEachGainOneIconUnderHysteria()
    {
        var f = Create();
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        f.World.CreateCard("02029", deck);
        var second = f.World.CreateCard("02030", deck);
        var first = f.World.CreateCard("02030", deck);
        f.World.CreateCard("02024", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.Reveal("02020", 1);
        f.Finish();
        f.Reveal("02030", 1);
        f.Finish();
        Assert.Equal(6, f.World.Seats[1].IdentityCard.Damage);
        Assert.All(new[] { first, second }, card =>
        {
            Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
            Assert.Equal(1, StateFields.Modified(f.World, card, "boost_const", Cards, 2));
        });
        Assert.Single(deck.Cards);
        Assert.Null(f.World.Activation);
    }

    private static RedSkullFixture Create(string stage = "02014", bool hero = true)
    {
        var world = Hero("01001a", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", world.Seats[1].Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard(stage, world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        return new(world, runner);
    }
}
