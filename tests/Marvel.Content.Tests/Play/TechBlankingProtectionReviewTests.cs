using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TechBlankingProtectionReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:permanent.1")]
    public void DifferentSetPermanentRetainsTextWhileOrdinaryTechIsBlanked()
    {
        // Effects from outside a permanent card's set cannot blank its text.
        var world = Hero("01001a");
        world.Abilities = Runner("12026", "01008", "draw");
        world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var protectedCard = world.CreateCard("31001b", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ordinary = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.NotEqual(world.Facts.EncounterSet("12026"), world.Facts.EncounterSet("31001b"));
        Assert.True(world.Facts.Attributes("31001b").ContainsKey("Permanent"));
        Assert.True(Traits.Has(world, protectedCard, "TECH", world.Facts));
        Assert.False(PrintedText.IsBlanked(world, protectedCard));
        Assert.True(PrintedText.IsBlanked(world, ordinary));
    }

    [Theory]
    [InlineData("01008", "same-set TECH blanking of Permanent")]
    [InlineData("05017", "TECH blanking of Permanent requires printed set identity")]
    [Rule("rr:permanent.1")]
    public void SameOrMissingSetPermanentRefusesWithoutChangingTheBoard(string face, string message)
    {
        // A synthetic Permanent grant exercises the unsupported partial-text case.
        // Different-set card effects cannot blank any part of a Permanent text box.
        var world = Hero("01001a");
        var source = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "permanent", Amount: 1,
            Card: source.ObjectId, Affects: target.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        var originalAbilities = world.Abilities;
        string before = world.Digest().Canonical();
        world.Abilities = Runner("01002", face, "draw");
        var error = Assert.Throws<RulesNotImplementedException>(() => PrintedText.IsBlanked(world, target));
        Assert.Contains(message, error.Message);
        world.Abilities = originalAbilities;
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Fact]
    [Rule("rr:winning-the-game.1.1")]
    public void CompiledAlternateVictoryRefusesRevealBeforeCommit()
    {
        // Alternate win or loss conditions cannot be blanked or canceled.
        // This synthetic player-card instruction exercises the compiled guard.
        var world = Hero("01001a");
        world.Abilities = Runner("12026", "01008", "playersWin");
        var target = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var source = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        string before = world.Digest().Canonical();
        var error = Assert.Throws<RulesNotImplementedException>(() => Reveal.Resolve(world, world.Facts, source, 0, []));
        Assert.Contains("alternate victory requires protected-text support", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        Assert.False(PrintedText.IsBlanked(world, target));
        Assert.False(world.IsOver);
    }

    private static AbilityRunner Runner(string source, string target, string effect)
    {
        string targetEffect = effect == "playersWin" ? "{\"playersWin\":\"game\"}"
            : "{\"draw\":{\"player\":\"you\",\"count\":1}}";
        return new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[
              {"card":"{{{source}}}","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"{{{target}}}","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "effect":{{{targetEffect}}}}]}
            ]}
            """));
    }
}
