using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeLastSupportInventoryTests
{
    [Fact]
    public void ThreeReviewedProgramsRemainWithoutCountersAndKeepOtherPacksUnauthored()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        string[] faces = ["09015", "09027", "09029"];
        Assert.Equal(faces, rows.Skip(679).Take(3).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(679).Take(3), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(40, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        string[] unauthored = ["52031"];
        Assert.All(unauthored, face => Assert.DoesNotContain(face, AuthoredCards.Book.Authored));
    }
}
