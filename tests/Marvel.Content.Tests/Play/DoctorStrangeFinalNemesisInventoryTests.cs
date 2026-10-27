using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeFinalNemesisInventoryTests
{
    private static readonly string[] FinalFaces = ["09028", "09031", "09030"];

    [Fact]
    public void FinalNemesisProgramsCompleteAllFortyFacesWithoutStartingCounters()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(712, rows.Length);
        Assert.Equal(FinalFaces, rows.Skip(682).Take(3).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(682).Take(3), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        using var printed = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var faces = printed.RootElement.GetProperty("cards").EnumerateArray()
            .Select(row => row.GetProperty("card_id").GetString()!)
            .Where(face => face.StartsWith("09", StringComparison.Ordinal)).Order().ToArray();
        Assert.Equal(40, faces.Length);
        Assert.Equal(faces, AuthoredCards.Book.Authored.Where(face => face.StartsWith("09", StringComparison.Ordinal)).Order());
        Assert.DoesNotContain("90001", AuthoredCards.Book.Authored);
        using var snapshot = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("marvelsdb", "pack", "drs_encounter.json")));
        foreach (string face in FinalFaces)
        {
            var card = Assert.Single(snapshot.RootElement.EnumerateArray(), row => row.GetProperty("code").GetString() == face);
            Assert.Equal(face == "09030" ? 2 : 1, card.GetProperty("quantity").GetInt32());
            Assert.Equal("doctor_strange_nemesis", card.GetProperty("set_code").GetString());
        }
    }
}
