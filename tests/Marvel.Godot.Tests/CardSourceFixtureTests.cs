using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardSourceFixtureTests
{
    [Fact]
    public void NativeSourcesUseTypedCoreMeaningsInExplicitSyntheticStressLayouts()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        AbilityBook abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 2, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        Seat other = world.CreateSeat("P1");
        other.IdentityCard = world.CreateCard("01001a", other.Hero);
        Card hero = seat.IdentityCard = world.CreateCard("01029a", seat.Hero);
        hero.TakeDamage(4);
        Area controlled = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0, host: hero.ObjectId);
        world.CreateCard("01035", controlled);
        world.CreateCard("01036", controlled);
        Card fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Attach(world, "01074", fury);
        Card drone = world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        drone.TakeDamage(2);
        Attach(world, "01163", drone);
        Attach(world, "01007", drone);
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Attach(world, "01153", rhino);
        Attach(world, "01099", rhino);
        Attach(world, "01098", rhino).TakeDamage(2);
        Card environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        Attach(world, "01142", environment);
        BoardAreaPresentation[] areas = [.. BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas];
        BoardCardPresentation[] cards = [.. areas.SelectMany(area => area.Cards)];
        BoardCardPresentation Source(string id) => Assert.Single(cards, card => card.FaceId == id);
        Assert.Equal("5/7", CardProgressValue.From(Source("01143"))!.Value);
        Assert.Equal(3, Source("01084").EffectiveValues["ATK"].CurrentValue);
        Assert.Equal(6, Source("01094").EffectiveValues["ATK"].CurrentValue);
        Assert.Equal("11/15", CardProgressValue.From(Source("01029a"))!.Value);
        Assert.Equal(3, Source("01029a").EffectiveValues["HS"].CurrentValue);
        Assert.Equal("+6 max HP", CardSourceSummary.Contribution(Assert.Single(Source("01036").Persistent!.Contributions)));
        Assert.Contains("Exhaust this → ready your hero", CardSourceSummary.Lines(Source("01035"))[0]);
        Assert.Contains("Exhaust your hero", string.Join(" ", CardSourceSummary.Lines(Source("01153"))));
        Assert.Contains("[energy] [energy]", string.Join(" ", CardSourceSummary.Lines(Source("01153"))));
        Assert.Contains("Next attack:", CardSourceSummary.Lines(Source("01099"))[0]);
        Assert.DoesNotContain("overkill 0", CardSourceSummary.Lines(Source("01099"))[0]);
        Assert.Contains("discard this after attack", CardSourceSummary.Lines(Source("01099"))[0]);
        Assert.Contains("All damage → here; discard this when damage here ≥5", CardSourceSummary.Lines(Source("01098"))[0]);
        Assert.Contains("2 damage here", CardSourceSummary.Lines(Source("01098")));
        Assert.Contains("0 damage here", CardSourceSummary.Lines(Source("01098") with { Damage = 0 }));
        Assert.Single(CardSourceGroups.Attached(areas, environment.ObjectId));
        Assert.DoesNotContain(CardSourceGroups.Attached(areas, drone.ObjectId), card => card.FaceId == "01142");
        Assert.Equal(2, CardSourceGroups.Controlled(areas, 0).Length);
        // These assembled layouts are synthetic snapshots, not transcripts of legal play.
        areas = [.. areas.Select(area => area with { Cards = [.. area.Cards.Select(card => card.TargetId == hero.ObjectId
            ? card with { Status = "EXHAUSTED" } : card)] })];
        string? output = Environment.GetEnvironmentVariable("MARVEL_B1_SOURCE_FIXTURE");
        if (output is null) return;
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../.."));
        Assert.False(Path.GetFullPath(output).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        File.WriteAllText(output, JsonSerializer.Serialize(areas));
    }

    private static Card Attach(World world, string id, Card host) => world.CreateCard(id,
        world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host: host.ObjectId));
}
