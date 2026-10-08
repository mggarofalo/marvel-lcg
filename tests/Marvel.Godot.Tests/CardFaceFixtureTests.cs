using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardFaceFixtureTests
{
    [Fact]
    public void CanonicalFacesProvideNativeFixtureWithoutExpandingPlayableContent()
    {
        string json = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
        var catalog = CardCatalog.Parse(json);
        using JsonDocument source = JsonDocument.Parse(json);
        string[] research = ["03014", "27047", "41001a", "02001a"];
        string[] ids = source.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => card.GetProperty("pack").GetString() == "core"
                || research.Contains(card.GetProperty("card_id").GetString()))
            .Select(card => card.GetProperty("card_id").GetString()!).ToArray();
        var world = new World(catalog, players: 1, seed: 7);
        Seat seat = world.CreateSeat("Visual fixture");
        foreach (string id in ids) world.CreateCard(id, seat.Hand);
        WorldDescriptor visible = WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        BoardCardPresentation[] faces = BoardPresentation.From(visible).Areas.SelectMany(area => area.Cards).ToArray();
        Assert.Equal(ids.Length, faces.Length);
        Assert.Equal("—", Mark(faces, "01050", "THW").Value);
        Assert.Equal("X", Mark(faces, "01162", "ATK").Value);
        Assert.Equal("★", Mark(faces, "02001a", "ATK").Value);
        Assert.True(Mark(faces, "02001a", "ATK").SpecialStar);
        Assert.Equal("0", Mark(faces, "01121", "Boost").Value);
        Assert.True(Mark(faces, "01121", "Boost").SpecialStar);
        // Presentation-only stress specimen: no expansion mechanics or engine decision is executed.
        faces = [.. faces, new BoardCardPresentation(null, 1, false, "Measured marks", "", "ALLY", "", [])
        {
            FaceId = "synthetic", Classification = "BASIC", Cost = "0",
            PrintedStats = [new("RES", "RYBW")],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(12, 14, "Printed", true, []) },
            PrintedMarks = [new("THW", false, 0) { Value = "0" },
                new("ATK", false, 2) { Value = "12", SpecialStar = true },
                new("HP", false, 0) { Value = "3" }],
            RulesMarkup = "<b>Native geometry specimen.</b> A two-digit value keeps its separate special star and centered consequence group.",
        }];
        faces = [.. faces, new BoardCardPresentation(null, 1, false, "Defined quantity", "", "MINION", "", [])
        {
            FaceId = "synthetic-defined",
            PrintedMarks = [new("ATK", false, 0) { Value = "X" }],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(6, 6, "Defined", false, []) },
            RulesMarkup = "<b>Native geometry specimen.</b> Resolving a printed X does not imply an external modifier.",
        }];
        // Cost notation is a renderer specimen, not an offered expansion card or payable action.
        foreach (string? cost in new string?[] { null, "0", "X" })
            faces = [.. faces, new BoardCardPresentation(null, 1, false, "Cost notation", "", "EVENT", "", [])
            {
                FaceId = "synthetic-cost-" + (cost ?? "absent"), Classification = "BASIC", Cost = cost,
                RulesMarkup = "<b>Native geometry specimen.</b> Absent, zero and variable costs retain their distinct printed meaning.",
            }];
        string? output = Environment.GetEnvironmentVariable("MARVEL_B1_FACE_FIXTURE");
        if (output is null) return;
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../.."));
        Assert.False(Path.GetFullPath(output).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        File.WriteAllText(output, JsonSerializer.Serialize(faces));
    }
    private static BoardPrintedValueMark Mark(IEnumerable<BoardCardPresentation> faces, string id, string attribute) =>
        Assert.Single(Assert.Single(faces, card => card.FaceId == id).PrintedMarks, mark => mark.Attribute == attribute);

}
