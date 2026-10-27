using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkFinalCardInventoryTests
{
    [Fact]
    public void TwoFinalFacesCompleteTheExactHulkBookWithoutStartingCounters()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(722, rows.Length);
        var appended = rows.Skip(716).Take(2).ToArray();
        Assert.Equal(["10015", "10029"], appended.Select(row => row.GetProperty("card").GetString()));
        Assert.All(appended, row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        string[] identity = ["10001a", "10001b"];
        var expected = identity.Concat(Enumerable.Range(10002, 31)
            .Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal(expected.Order(), AuthoredCards.Book.Authored.Where(id => id.StartsWith("10", StringComparison.Ordinal)).Order());
        Assert.Single(AuthoredCards.Book.On("10015"));
        Assert.Equal(2, AuthoredCards.Book.On("10029").Count());
        Assert.DoesNotContain("11001", AuthoredCards.Book.Authored);
    }
}
