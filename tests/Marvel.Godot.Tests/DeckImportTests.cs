using System.Text;
using Marvel.Client;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckImportTests
{
    [Fact]
    public void JsonPropertyOrderDoesNotChangeExactImportedCopies()
    {
        var left = Read("""{"name":"Panther","hero_code":"01040a","slots":{"01043d":2,"01043a":1,"unsupported":1}}""");
        var right = Read("""{"slots":{"unsupported":1,"01043a":1,"01043d":2},"hero_code":"01040a","name":"Panther"}""");
        Assert.Equal("01040a", left.HeroCode);
        Assert.Equal(["01043a", "01043d", "01043d", "unsupported"], left.Cards);
        Assert.Equal(left.Cards, right.Cards);
        var roundtrip = MarvelCdbDeckJson.Read(MarvelCdbDeckJson.Write(left));
        Assert.Equal(left.Name, roundtrip.Name);
        Assert.Equal(left.HeroCode, roundtrip.HeroCode);
        Assert.Equal(left.Cards, roundtrip.Cards);
    }

    [Theory]
    [InlineData("{\"01043d\":0}")]
    [InlineData("{\"01043d\":-1}")]
    [InlineData("{\"01043d\":1.5}")]
    [InlineData("{\"01043d\":\"2\"}")]
    [InlineData("{\"01043d\":501}")]
    [InlineData("{\"01043d\":2,\"01043d\":1}")]
    [InlineData("{\"01043a,01043d\":1}")]
    [InlineData("[]")]
    public void InvalidSlotRepresentationIsRejectedWithoutRepair(string slots) =>
        Assert.Throws<InvalidDataException>(() => Read("{\"hero_code\":\"01040a\",\"slots\":" + slots + "}"));

    [Fact]
    public void DuplicateIdentityAndExtraDeckLimitSlotsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => Read("""{"hero_code":"01040a","hero_code":"01001a","slots":{}}"""));
        Assert.Throws<InvalidDataException>(() => Read("""{"hero_code":"01040a","slots":{},"ignoreDeckLimitSlots":{"01001":1}}"""));
        Assert.Throws<InvalidDataException>(() => MarvelCdbDeckJson.Read(new byte[MarvelCdbDeckJson.MaximumBytes + 1]));
    }

    [Theory]
    [InlineData("https://marvelcdb.com/decklist/view/1/panther", "https://marvelcdb.com/api/public/decklist/1.json")]
    [InlineData("https://www.marvelcdb.com/api/public/decklist/123.json", "https://marvelcdb.com/api/public/decklist/123.json")]
    public void OnlyThePublishedIdDeterminesTheAcquisitionEndpoint(string url, string endpoint) =>
        Assert.Equal(endpoint, MarvelCdbDeckImporter.Endpoint(url).AbsoluteUri);

    [Theory]
    [InlineData("http://marvelcdb.com/decklist/view/1")]
    [InlineData("https://marvelcdb.com.evil.example/decklist/view/1")]
    [InlineData("https://marvelcdb.com@localhost/decklist/view/1")]
    [InlineData("https://marvelcdb.com:444/decklist/view/1")]
    [InlineData("https://marvelcdb.com/deck/view/1")]
    [InlineData("file:///decklist/view/1")]
    public void UnrelatedOrPrivateUrlsAreRejected(string url) =>
        Assert.Throws<InvalidDataException>(() => MarvelCdbDeckImporter.Endpoint(url));

    private static DeckDraft Read(string json) => MarvelCdbDeckJson.Read(Encoding.UTF8.GetBytes(json));
}
