using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardLiveStateFixtureTests
{
    [Fact]
    public void NativeStatesUseCanonicalCoreFacesAndExplicitSyntheticStressSnapshots()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        AbilityBook abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 2, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        world.CreateSeat("P1");
        seat.IdentityCard = world.CreateCard("01029a", seat.Hero);
        Area engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card drone = world.CreateCard("01091", engaged);
        drone.AssignProfile(AbilityLowering.Book(abilities).Profiles["effective-drone"]);
        drone.TurnFaceDown();
        world.CreateCard("01143", engaged);
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        foreach (string id in new[] { "01121", "01129", "01130", "01131", "01132" }) world.CreateCard(id, engaged);
        BoardCardPresentation[] projected = BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas.SelectMany(area => area.Cards).ToArray();
        BoardCardPresentation ordinary = Assert.Single(projected, card => card.Title == "Drone");
        BoardCardPresentation advanced = Assert.Single(projected, card => card.FaceId == "01143");
        Assert.Equal("1/1", CardProgressValue.From(ordinary)!.Value);
        Assert.Equal("4/4", CardProgressValue.From(advanced)!.Value);
        Assert.Null(ordinary.FaceId);
        Assert.DoesNotContain("01091", JsonSerializer.Serialize(ordinary));
        // Snapshot-only stress cases: statuses, counters, damage and quantities below are synthetic.
        var fixtures = projected.Select(card => card.Kind == "MINION" ? card with { Statuses = ["Stunned"] } : card).ToList();
        fixtures.Add(Assert.Single(projected, card => card.Kind == "HERO") with { TargetId = 700, Title = "Synthetic exhausted long-name hero",
            Status = "EXHAUSTED", Statuses = ["Tough", "Stunned", "Confused"], Counters = [new("USES", "12")],
            Damage = 4, Fields = [new("HEALTH", "5/9")], Retaliate = 1 });
        fixtures.Add(Assert.Single(projected, card => card.Kind == "MAIN SCHEME") with
        { Title = "Synthetic near-threshold scheme", Fields = [new("THREAT", "13"), new("TARGET_THREAT", "14"),
            new("ESCALATION_THREAT", "2"), new("HAZARD", "1"), new("ACCELERATION_ICON", "2")] });
        fixtures.Add(new(null, 4, true, "4 concealed encounter cards", "Identity and order hidden", "CONCEALED PILE", "", [])
            { Back = "ENCOUNTER" });
        fixtures.Add(ordinary with { TargetId = 701, Title = "Fresh Drone", Statuses = [], Counters = [], Damage = 0 });
        fixtures.Add(ordinary with { TargetId = 702, Title = "Synthetic modified Drone", Damage = 2,
            Statuses = [], Fields = [new("HEALTH", "5/7")], EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["HP"] = new(1, 7, "Replacement", true,
                [new("Add", 3, Source(false, 900), null), new("Add", 3, Source(true, null), null)]) } });
        string? output = Environment.GetEnvironmentVariable("MARVEL_B1_STATE_FIXTURE");
        if (output is null) return;
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../.."));
        Assert.False(Path.GetFullPath(output).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        File.WriteAllText(output, JsonSerializer.Serialize(fixtures));
    }

    private static CardValueSourceDescriptor Source(bool historical, int? id) =>
        new("synthetic-source", "Duplicate source", id, historical)
        { RulesText = "Synthetic source text. This specimen grants the supplied maximum hit points." };
}
