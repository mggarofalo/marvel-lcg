using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingManEncounterTests
{
    [Fact]
    public void CompleteEncounterFragmentCompiles()
    {
        var catalog = AbilityCatalog.Parse(AbsorbingManFragment.Json);
        var runner = new AbilityRunner(catalog);
        Assert.NotNull(runner);
        Assert.Contains(catalog.Abilities, ability => ability.Card == "04092");
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 1, 0)]
    [InlineData(5, 2, 0)]
    public void StallTacticsUsesCompletePairsAndSurgesOnlyWhenNoThreatLands(int delay, int threat, int dealt)
    {
        var f = AbsorbingManFixture.Create();
        f.Scheme.PlaceTokens("c_delay", delay);
        long before = f.Scheme.Tokens.GetValueOrDefault("k_threat");
        int queued = f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;
        f.Reveal("04085");
        f.Finish();
        Assert.Equal(before + threat, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(queued + dealt, f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    public void SteelKickInAlterEgoUsesLiveMetalTrait(bool metal, int threat)
    {
        var f = AbsorbingManFixture.Create();
        if (metal) f.Card("04083", DeckType.EnvironmentArea);
        long before = f.Scheme.Tokens.GetValueOrDefault("k_threat");
        f.Reveal("04087");
        f.Finish();
        Assert.Equal(before + threat, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void SuperAbsorbingPowerProvidesAllFourTraitsUntilItLeaves()
    {
        var f = AbsorbingManFixture.Create();
        var power = f.Card("04092", DeckType.SideSchemesArea);
        Assert.Equal<string>(["BRUTE", "ICE", "METAL", "STONE", "WOOD"], Traits.Of(f.World, f.Villain, f.World.Facts));
        Discard.Card(f.World, power, "test", []);
        Assert.Equal<string>(["BRUTE"], Traits.Of(f.World, f.Villain, f.World.Facts));
    }

    [Fact]
    public void StageTwoDealsEachPlayerWhenPowerIsAlreadyInPlay()
    {
        var f = AbsorbingManFixture.Create(players: 2);
        f.Card("04092", DeckType.SideSchemesArea);
        f.Reveal("04077");
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    public void OmniMorphResolvesEveryPresentTrait()
    {
        var f = AbsorbingManFixture.Create();
        f.Card("04092", DeckType.SideSchemesArea);
        f.Villain.TakeDamage(3);
        Hand(f.World, "01003", 2);
        f.Reveal("04089");
        Assert.False(f.World.Seats[0].IdentityCard.Ready);
        Assert.True(Statuses.Has(f.World, f.Villain, Statuses.Tough));
        Assert.Equal(2, f.Villain.Damage);
        Assert.Single(f.World.Seats[0].Hand.Cards);
        Assert.Contains(f.World.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
    }
    [Theory]
    [InlineData("{\"enemyAttacks\":{\"enemies\":{\"query\":\"villain\"},\"modifier\":\"many\"}}")]
    [InlineData("{\"discardOtherEnvironments\":\"you\"}")]
    public void MalformedEncounterInstructionsAreRejected(string effect)
    {
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(
            "{\"cards\":[{\"card\":\"04086\",\"abilities\":[{\"trigger\":{\"timing\":\"WhenRevealed\",\"event\":\"WhenCardRevealed\"},\"effect\":"
            + effect + "}]}]}")));
    }
}
