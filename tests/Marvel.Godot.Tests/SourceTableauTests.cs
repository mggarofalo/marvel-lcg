using System.Text.Json;
using Godot;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SourceTableauTests
{
    [Fact]
    public void SixteenCoreSourcesRetainDistinctSlotsAndStateAcrossExhaustion()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        AbilityBook abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 1, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        Card hero = seat.IdentityCard = world.CreateCard("01029a", seat.Hero);
        Area upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0, host: hero.ObjectId);
        Area supports = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
        foreach (string id in new[] { "01035", "01036", "01037", "01038", "01038", "01039", "01039", "01081", "01093", "01093", "01093" })
            world.CreateCard(id, upgrades);
        foreach (string id in new[] { "01033", "01034", "01080", "01091", "01092" }) world.CreateCard(id, supports);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        foreach (string id in new[] { "01031", "01032", "01088", "01089", "01090" }) world.CreateCard(id, seat.Hand);
        world.Cards.First(card => card.FaceId == "01039").Exhaust();
        BoardAreaPresentation[] areas = [.. BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas];
        BoardCardPresentation[] sources = CardSourceGroups.Controlled(areas, 0);
        Assert.Equal(16, sources.Length);
        Assert.Equal(2, sources.Count(card => card.Title == "Powered Gauntlets"));
        Assert.Contains(sources, card => card.Title == "Pepper Potts");
        var reordered = areas.AsEnumerable().Reverse().Select(area => area with { Cards = [.. area.Cards.Reverse()
            .Select(card => card with { Status = "EXHAUSTED" })] }).ToArray();
        Assert.Equal(sources.Select(card => card.TargetId), CardSourceGroups.Controlled(reordered, 0).Select(card => card.TargetId));
        Assert.Equal("Ready", SourceTableauTile.State(sources[0]));
        Assert.Equal("Exhausted", SourceTableauTile.State(sources[0] with { Status = "EXHAUSTED" }));
        var runner = (AbilityRunner)world.Abilities;
        Affordance[] offers = [.. runner.Actions(world, 0).Select((action, index) => runner.Describe(world, action) with { Id = index + 1 })];
        var prompt = new Prompt(0, Question.Option, TimingPriority.Untimed, "Source fixture", "Choose a source", false, offers);
        WorldDescriptor descriptor = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        Assert.Equal(2, offers.Count(offer => offer.Label == "Powered Gauntlets"));
        Assert.Single(offers, offer => offer.Label == "Rocket Boots");
        // Synthetic assembled board: proves density and identity, not a legal play transcript.
        string? output = System.Environment.GetEnvironmentVariable("MARVEL_TABLEAU_FIXTURE");
        if (output is null) return;
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../.."));
        Assert.False(Path.GetFullPath(output).StartsWith(root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        File.WriteAllText(output, JsonSerializer.Serialize(areas));
        File.WriteAllText(output + ".world", JsonSerializer.Serialize(descriptor));
        File.WriteAllText(output + ".prompt", JsonSerializer.Serialize(prompt));
    }

    [Theory]
    [InlineData(1060)]
    [InlineData(1320)]
    [InlineData(1670)]
    public void ResolvingContextKeepsEverySourceReachableInsideNarrowPaymentTable(float width)
    {
        var table = new AstraTableGeometry(width, 962, true, HasRevealingCard: true,
            PhysicalCardSize: new Vector2(240, 240), HasSourceTableau: true);
        Assert.True(new Rect2(0, 0, width, 962).Encloses(table.SourceTableau));
        Assert.False(table.SourceTableau.Intersects(table.Identity));
        Assert.False(table.SourceTableau.Intersects(table.Revealing));
        Assert.False(table.SourceTableau.Intersects(table.Hand));
        Assert.False(table.SourceTableau.Intersects(table.Context));
        Assert.True(table.SourceTableau.Size.Y >= 150);
        Assert.True(table.SourceTableau.Size.X >= 156);
        var handSize = new Vector2(170, 230);
        for (int index = 0; index < 6; index++)
        {
            SpatialCardPlacement placement = table.HandCard(index, 6, handSize.X);
            var pose = new Transform2D(placement.Rotation,
                placement.Position + handSize / 2 - handSize.Rotated(placement.Rotation) / 2);
            Assert.False(SpatialCardFootprint.Bounds(pose, handSize).Intersects(table.SourceTableau));
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(150)]
    public void DenseDesktopSourcesFitWithoutCoveringHandCharactersOrDecision(int percent)
    {
        var table = new AstraTableGeometry(1670, 962, percent >= 130, PhysicalCardSize: new Vector2(240, 240), HasSourceTableau: true);
        var layout = new SourceTableauLayout(table.SourceTableau.Size.X - 12);
        Assert.Equal(4, layout.Columns);
        for (int index = 0; index < 16; index++)
        {
            var tile = new Rect2(table.SourceTableau.Position + new Vector2(0, 24) + layout.Position(index),
                new Vector2(layout.TileWidth, SourceTableauLayout.TileHeight));
            Assert.True(table.SourceTableau.Encloses(tile));
            Assert.False(tile.Intersects(table.Hand));
            Assert.False(tile.Intersects(table.Identity));
            Assert.False(tile.Intersects(table.Allies));
            Assert.False(tile.Intersects(table.Context));
        }
    }

    [Fact]
    public void InstalledSourcesReserveReadableAllyAndActionSpace()
    {
        var table = new AstraTableGeometry(1670, 962, true,
            PhysicalCardSize: new Vector2(240, 240), HasSourceTableau: true) { HasAllies = true };
        Vector2 ally = SpatialCardFootprint.OccupiedSize(new Vector2(240, 240));
        Assert.True(table.Allies.Size.X >= ally.X);
        Assert.False(new Rect2(table.Allies.Position, ally).Intersects(table.SourceTableau));
        Assert.False(table.Hand.Intersects(table.SourceTableau));
        var withoutSources = table with { HasSourceTableau = false };
        var withoutAllies = withoutSources with { HasAllies = false };
        Assert.Equal(withoutAllies.Allies, withoutSources.Allies);
        Assert.Equal(withoutAllies.Identity, withoutSources.Identity);
    }
}
