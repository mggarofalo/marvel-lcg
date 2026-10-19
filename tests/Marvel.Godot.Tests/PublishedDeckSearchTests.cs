using System.Net;
using Marvel.Client;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PublishedDeckSearchTests
{
    private const string Page = """
        <form id="decklist-search-form"><select name="hero"><option value="">Any hero</option><option value="01001a">Spider-Man</option></select>
        <select name="aspect"><option value="">Any aspect</option></select><select name="tag"><option value="">Any tag</option></select>
        <select name="sort"><option value="">Popularity</option></select>
        <label><input name="packs[]" value="1">Core Set</label></form>
        <div class="box"><h4><a href="/decklist/view/1/example">Example &amp; Friends</a></h4><a class="card-tip">Black Panther</a><a class="username">Builder</a></div>
        <ul class="pagination"><a href="/decklists/find/2?author=Builder">Next</a></ul>
        """;

    [Fact]
    public async Task PublicSearchPreservesFiltersAndDecodesResults()
    {
        using var search = new MarvelCdbDeckSearch(new ResponseHandler(Page));
        var page = await search.SearchAsync([new("author", "A & B"), new("cards[]", "01005"), new("packs[]", "1")], cancellationToken: TestContext.Current.CancellationToken);
        var deck = Assert.Single(page.Decks);
        Assert.Equal("Example & Friends", deck.Name);
        Assert.Equal("Builder", deck.Author);
        Assert.Equal("Black Panther", deck.Hero);
        Assert.Equal("https://marvelcdb.com/decklist/view/1/example", deck.Url);
        Assert.Equal("Core Set", page.Packs["1"]);
        Assert.Equal("Spider-Man", page.Filters["hero"]["01001a"]);
        Assert.True(page.HasNext);
        Assert.False(MarvelCdbDeckSearch.Parse(Page, 2).HasNext);
    }

    [Fact]
    public void FilterEncodingCannotChangeTheDestination()
    {
        var endpoint = MarvelCdbDeckSearch.Endpoint([new("author", "A&B#C"), new("cards[]", "01005"), new("cards[]", "01006")], 2);
        Assert.Equal("marvelcdb.com", endpoint.Host);
        Assert.Equal("/decklists/find/2", endpoint.AbsolutePath);
        Assert.Contains("author=A%26B%23C", endpoint.Query);
        Assert.Contains("cards%5B%5D=01006", endpoint.Query);
        Assert.Throws<ArgumentException>(() => MarvelCdbDeckSearch.Endpoint([new("redirect", "https://example.com")], 1));
    }

    [Fact]
    public async Task OversizeAndUnrecognizedResponsesFailWithoutInventingResults()
    {
        using var search = new MarvelCdbDeckSearch(new ResponseHandler(new string('x', 1024 * 1024 + 1)));
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => search.SearchAsync([], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("exceeds 1 MiB", error.Message);
        Assert.Throws<InvalidDataException>(() => MarvelCdbDeckSearch.Parse("<h1>Try again later</h1>", 1));
    }

    private sealed class ResponseHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
    }
}
