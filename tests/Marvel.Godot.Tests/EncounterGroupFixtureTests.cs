using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EncounterGroupFixtureTests
{
    [Fact]
    public void ResearchFacesSupplyExplicitSyntheticTopologyWithoutAdmittingScenario()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        foreach (bool extreme in new[] { false, true })
        {
            var world = new World(catalog, 1, 7);
            var seat = world.CreateSeat("Synthetic layout");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
            world.CreateCard("01002", seat.Hand);
            world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
            string[][] codes = [["07002", "07003", "07004"], ["07017", "07018", "07019"],
                ["07032", "07033", "07034"], ["07046", "07047", "07048"]];
            var groups = codes.Select(group => Group(world, group, extreme)).ToArray();
            world.BindEncounterTopology(new EncounterTopology(world, groups, groups[0].Villain));
            var visible = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, 1)).World;
            var board = BoardPresentation.From(visible);
            Assert.Equal(4, board.EncounterGroups.Count);
            Assert.Equal(8, board.Areas.Count(area => area.Zone is "EncounterDeck" or "EncounterDiscardPile"
                && groups.Any(group => group.Deck.Id == area.Id || group.Discard.Id == area.Id)));
            Assert.All(board.EncounterGroups, group => Assert.Equal(extreme ? 2 : 1, group.Stages.Count));
            Assert.All(board.Areas.Where(area => area.Zone == "SideSchemesArea").SelectMany(area => area.Cards),
                card => Assert.Equal("ENCOUNTER SIDE SCHEME", card.Kind));
            string? output = Environment.GetEnvironmentVariable("MARVEL_GROUP_FIXTURE");
            if (output is null) continue;
            Assert.False(Path.GetFullPath(output).StartsWith(RepositoryPaths.Root + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase));
            File.WriteAllText(output + (extreme ? ".extreme" : ""), JsonSerializer.Serialize(board));
        }
    }

    private static EncounterDeckGroup Group(World world, string[] codes, bool extreme)
    {
        var villain = world.CreateCard(codes[0], world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard(codes[2], world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 6); // Synthetic stress value, not scenario setup evidence.
        var deck = world.CreateArea(DeckType.EncounterDeck);
        var discard = world.CreateArea(DeckType.EncounterDiscardPile);
        var members = Enumerable.Range(0, 15).Select(_ => world.CreateCard("07005", deck)).ToArray();
        Card[] stages = extreme
            ? [villain, world.CreateCard(codes[1], world.AreaOf(DeckType.VillainDeck))] : [villain];
        return new EncounterDeckGroup(villain, scheme, deck, discard, members, stages);
    }
}
