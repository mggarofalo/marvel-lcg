using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardSearchTests
{
    [Theory]
    [InlineData("t:event o<4 x:\"8 damage\"", "01005")]
    [InlineData("\"Swinging Web Kick\"", "01005")]
    [InlineData("01005", "01005")]
    [InlineData("k:attack f:hero m:spider_man", "01005")]
    public void QueriesMatchCanonicalCards(string query, string expected)
    {
        var catalog = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverDecks();
        Assert.Contains(CardSearch.Find(catalog.Cards, query), card => card.Face == expected);
    }

    [Fact]
    public void AlternativesNegationAndStrictNumericBoundsCompose()
    {
        var card = new DeckCard("test", "Example", "Event", "Justice", ["Attack"], "Deal damage", "3")
        { SearchFields = new Dictionary<string, string> { ["f"] = "justice", ["u"] = "0", ["qt"] = "2", ["l"] = "Artist Name", ["v"] = "A flavor phrase" } };
        Assert.Single(CardSearch.Find([card], "f:justice|aggression o>2 o<4 u:0 qt:2 l:\"Artist Name\" v:flavor"));
        Assert.Empty(CardSearch.Find([card], "f!justice|aggression"));
        Assert.Empty(CardSearch.Find([card], "o<3"));
        Assert.Empty(CardSearch.Find([card], "at:0"));
        Assert.Empty(CardSearch.Find([card], "k:att"));
        Assert.Single(CardSearch.Find([card], "x!heal"));
    }

    [Theory]
    [InlineData("zz:foo")]
    [InlineData("x:\"unfinished")]
    [InlineData("o:many")]
    [InlineData("x>damage")]
    [InlineData("r:not-a-date")]
    public void MalformedQueriesAreExplained(string query) => Assert.Throws<FormatException>(() => CardSearch.Find([], query));
}
