using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class HistoricalProtocol35ResponseTests
{
    [Theory]
    [InlineData(34)]
    [InlineData(36)]
    [InlineData(42)]
    public void OtherHistoricalVersionsCannotUseTheProjectionNormalization(int version)
    {
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes($"{{\"version\":{version}}}")));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[{\"active\":true}]")]
    public void UnexpectedGroupDataCannotBeErasedByTheComparisonReader(string groups)
    {
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes("{\"version\":35,\"world\":{\"encounter_groups\":" + groups + "}}")));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[42]")]
    public void UnexpectedOfferContextCannotBeErasedByTheComparisonReader(string context)
    {
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes("{\"version\":35,\"prompt\":{\"affordances\":[{\"context_card_ids\":"
                + context + "}]}}")));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[42]")]
    [InlineData("[{\"owner\":0,\"deck\":4,\"discard\":5}]")]
    public void UnexpectedInvocationDataCannotBeErasedByTheComparisonReader(string decks)
    {
        var response = JsonNode.Parse(ArchivedResponse("before"))!.AsObject();
        response["world"]!["invocation_decks"] = JsonNode.Parse(decks);
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes(response.ToJsonString())));
    }

    [Theory]
    [InlineData("opened")]
    [InlineData("guest")]
    [InlineData("before")]
    [InlineData("control-continued")]
    public void PinnedHistoricalResponsesGainOnlyEmptyInvocationMeaningWithoutChangingTheirBytes(string name)
    {
        byte[] original = ArchivedResponse(name);
        byte[] retained = (byte[])original.Clone();
        var response = HistoricalProtocol35Response.Read(original);
        Assert.Equal(35, response.Version);
        Assert.Empty(response.World!.InvocationDecks);
        Assert.Equal(retained, original);
        Assert.False(JsonNode.Parse(original)!["world"]!.AsObject().ContainsKey("invocation_decks"));
    }

    [Fact]
    public void CurrentDecoderStillRejectsOmittedInvocationMeaning()
    {
        var response = HistoricalProtocol35Response.Read(ArchivedResponse("before"));
        var current = JsonNode.Parse(EngineJson.Write(response with { Version = EngineProtocol.Version }))!;
        Assert.True(current["world"]!.AsObject().Remove("invocation_decks"));
        var failure = Assert.Throws<JsonException>(() => EngineJson.ReadResponse(
            Encoding.UTF8.GetBytes(current.ToJsonString())));
        Assert.Equal("response Invocation decks contain invalid references", failure.Message);
    }

    [Fact]
    public void ComparisonNormalizationStillRejectsUnknownWorldMembers()
    {
        var response = JsonNode.Parse(ArchivedResponse("before"))!;
        response["world"]!["unknown_comparison_field"] = new JsonArray();
        Assert.Throws<JsonException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes(response.ToJsonString())));
    }

    private static byte[] ArchivedResponse(string name)
    {
        byte[] archive = File.ReadAllBytes(Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests",
            "Fixtures", "core-session-91098022.zip"));
        Assert.Equal("63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC",
            Convert.ToHexString(SHA256.HashData(archive)));
        using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
        using var source = zip.GetEntry("responses/" + name + ".json")!.Open();
        using var copy = new MemoryStream();
        source.CopyTo(copy);
        return copy.ToArray();
    }
}
