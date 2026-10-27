using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreventionInventoryTests
{
    [Fact]
    public void PreventionRowsHaveTheExactFourFacesAndNoStartingCounters()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] added = ["08006", "08008", "08017", "08032"];
        Assert.Equal(682, rows.Length);
        Assert.Equal(added, rows.Skip(639).Take(4).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(639).Take(4), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.All(added, face => Assert.Contains(face, AuthoredCards.Book.Authored));
        Assert.DoesNotContain("11028", AuthoredCards.Book.Authored);
    }
}
