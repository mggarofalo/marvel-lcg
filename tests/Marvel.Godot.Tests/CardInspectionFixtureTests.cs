using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectionFixtureTests
{
    [Fact]
    public void InspectionFixturesUseCanonicalUltronAndAuthorizedSourcesWithoutHiddenFaces()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        AbilityBook abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 1, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        Card hero = seat.IdentityCard = world.CreateCard("01029a", seat.Hero);
        hero.TakeDamage(4);
        world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0, host: hero.ObjectId));
        world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        world.CreateCard("01150", world.AreaOf(DeckType.SideSchemesArea));
        BoardCardPresentation[] cards = [.. BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas.SelectMany(area => area.Cards)];
        BoardCardPresentation ultron = Assert.Single(cards, card => card.FaceId == "01135");
        Assert.Contains("Drone", ultron.RulesText);
        BoardCardPresentation armor = Assert.Single(cards, card => card.FaceId == "01036");
        // The exhausted long-title and historical rows are explicit UI stress snapshots.
        var fixtures = cards.Select(card => card.Kind == "HERO" ? card with { Status = "EXHAUSTED" } : card).ToList();
        fixtures.Add(armor with { TargetId = 800, Title = "Synthetic exceptionally long complete public source title" });
        fixtures.Add(CardValueSourceInspection.DescribeSource(new("01036", "Earlier Mark V Armor", null, true)
        { RulesText = armor.RulesText, RulesMarkup = armor.RulesMarkup }));
        fixtures.Add(new(null, 2, true, "2 concealed cards", "", "CONCEALED PILE", "", []) { Back = "ENCOUNTER" });
        Assert.DoesNotContain(TabletopAreaObject.From(new(1, "Cards", "", fixtures, [])).InspectionOrder, card => card.Concealed);
        string? output = Environment.GetEnvironmentVariable("MARVEL_B1_INSPECTION_FIXTURE");
        if (output is null) return;
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../.."));
        Assert.False(Path.GetFullPath(output).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        File.WriteAllText(output, JsonSerializer.Serialize(fixtures));
    }
}
