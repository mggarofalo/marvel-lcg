using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EnvironmentTraitTests
{
    [Theory]
    [InlineData("04076")]
    [InlineData("04077")]
    [InlineData("04078")]
    [Rule("rr:environment")]
    public void EveryStageGainsEachEnvironmentTraitOnlyWhileInPlay(string stage)
    {
        // 04076-78: "gains the trait of each environment in play."
        var fixture = EnvironmentTraitFixture.Create(stage);
        fixture.Environment("04080");
        fixture.Environment("04081");
        fixture.Environment("04082");
        fixture.Environment("04083", DeckType.EncounterDeck);
        fixture.Environment("04083", DeckType.EncounterDiscardPile);
        fixture.Environment("04083", DeckType.VictoryDisplay);
        fixture.Environment("04080");
        fixture.World.CreateCard("04084", fixture.World.AreaOf(DeckType.EnvironmentArea));
        string digest = fixture.World.Digest().Canonical();
        long rng = fixture.World.Random.Generator.WordsConsumed;
        var agenda = fixture.World.Agenda.Outstanding.ToArray();
        for (int repeat = 0; repeat < 3; repeat++)
            Assert.Equal<string>(["BRUTE", "WOOD", "ICE", "STONE"], fixture.Traits());
        Assert.Equal(digest, fixture.World.Digest().Canonical());
        Assert.Equal(rng, fixture.World.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, fixture.World.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:environment.1")]
    public void EnvironmentDepartureAndReplacementChangeTraitsImmediately()
    {
        // An environment remains in play until an ability or effect causes it to leave.
        var fixture = EnvironmentTraitFixture.Create();
        var forest = fixture.Environment("04080");
        Assert.Contains("WOOD", fixture.Traits());
        Discard.Card(fixture.World, forest, "synthetic environment replacement", []);
        Assert.DoesNotContain("WOOD", fixture.Traits());
        fixture.Environment("04083");
        Assert.Equal<string>(["BRUTE", "METAL"], fixture.Traits());
    }

    [Fact]
    [Rule("rr:traits.1")]
    public void EffectiveSourceTraitsIncludeGrantsAndLossesWithoutOverwritingRecipientTraits()
    {
        // Card abilities reference traits a card possesses, not only its printed list.
        var fixture = EnvironmentTraitFixture.Create();
        var forest = fixture.Environment("04080");
        var world = fixture.World;
        world.Effects.Register(new(EffectSource.LastingEffect, Traits.Granted + "METAL", Card: forest.ObjectId, Affects: forest.ObjectId));
        world.Effects.Register(new(EffectSource.LastingEffect, Characteristics.Lost + Traits.Granted + "WOOD", Card: forest.ObjectId, Affects: forest.ObjectId));
        world.Effects.Register(new(EffectSource.LastingEffect, Traits.Granted + "ICE", Card: fixture.Villain.ObjectId, Affects: fixture.Villain.ObjectId));
        Assert.Equal<string>(["BRUTE", "ICE", "METAL"], fixture.Traits());
        Discard.Card(world, forest, "synthetic departure", []);
        Assert.Equal<string>(["BRUTE", "ICE"], fixture.Traits());
    }

    [Fact]
    [Rule("rr:ability.10")]
    public void OtherConstantGrantsParticipateInTheSimultaneousTraitResult()
    {
        // Each constant instance affects the game independently; this environment's
        // synthetic extra trait proves copying reads the fixed point, not printed facts.
        var fixture = EnvironmentTraitFixture.Create(extra: """
            ,{"card":"04080","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"grant":{"card":"this","trait":"METAL"}}}]}
            """);
        fixture.Environment("04080");
        Assert.Equal<string>(["BRUTE", "WOOD", "METAL"], fixture.Traits());
    }

    [Fact]
    [Rule("rr:leaves-play")]
    public void DeparturePreflightReadsThePostDepartureTraitSetAndRestoresItAfterward()
    {
        // Leaving play ends the environment's contribution; preflight must read that same state.
        var fixture = EnvironmentTraitFixture.Create();
        var forest = fixture.Environment("04080");
        var ending = fixture.World.Effects.PreflightConstantsEnding(forest);
        using (ending.Begin()) Assert.DoesNotContain("WOOD", fixture.Traits());
        Assert.Contains("WOOD", fixture.Traits());
    }

    [Fact]
    [Rule("rr:ability.8.2")]
    public void CopyAbilityEndsWithItsSourceAndTheNextStageHasItsOwnCopy()
    {
        // A constant is active while its card remains in play.
        var fixture = EnvironmentTraitFixture.Create();
        fixture.Environment("04080");
        World.MoveToTop(fixture.Villain, fixture.World.AreaOf(DeckType.VictoryDisplay));
        var next = fixture.World.CreateCard("04077", fixture.World.AreaOf(DeckType.VillainArea));
        Assert.Equal<string>(["BRUTE"], fixture.Traits());
        Assert.Equal<string>(["BRUTE", "WOOD"], Traits.Of(fixture.World, next, fixture.World.Facts));
    }
}
