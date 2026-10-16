using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ProtocolRecoveryContractTests
{
    [Fact]
    public void RecoveryDispositionIsRequiredAndUnknownValuesAreRejected()
    {
        var response = new EngineResponse(EngineProtocol.Version, "request", "game", null, null, [],
            Error: new EngineError("candidate_failed", "prior state retained"));
        JsonNode document = JsonNode.Parse(EngineJson.Write(response))!;
        document["error"]!.AsObject().Remove("disposition");
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(System.Text.Encoding.UTF8.GetBytes(document.ToJsonString())));
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(EngineJson.Write(response with
        {
            Error = response.Error! with { Disposition = (EngineFailureDisposition)99 },
        })));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("null")]
    public void MalformedAffordanceValuesProduceProtocolErrors(string invalid)
    {
        string json = System.Text.Encoding.UTF8.GetString(EngineJson.Write(
            EngineRequest.ResolveGame("answer", "game", "capability", new Decision(7, []))));
        json = json.Replace("\"affordance\":7", "\"affordance\":" + invalid, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => EngineJson.ReadRequest(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[\"\"]")]
    public void InvalidDeckConstructorValuesProduceProtocolErrors(string cards)
    {
        JsonNode request = JsonNode.Parse(EngineJson.Write(EngineRequest.OpenGame("open", "table",
            new GameSpecification("rhino", ["spider_man"], [], 91))))!;
        request["game"]!["player_decks"] = JsonNode.Parse("[{\"cards\":" + cards + "}]");
        Assert.Throws<JsonException>(() => EngineJson.ReadRequest(
            System.Text.Encoding.UTF8.GetBytes(request.ToJsonString())));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{}")]
    public void InvalidVersionValuesProduceProtocolErrors(string version) =>
        Assert.Throws<JsonException>(() => EngineJson.ReadResponseVersion(
            System.Text.Encoding.UTF8.GetBytes("{\"version\":" + version + "}")));

    [Fact]
    public void TheWireCarriesOnlyTheFiveEngineAnswerFields()
    {
        var decision = new Decision(7, [3, 2], [11]);
        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(
            EngineRequest.ResolveGame("answer", "game", "capability", decision)));
        Assert.Equal(["affordance", "targets", "resources", "values", "allocations"],
            document.RootElement.GetProperty("decision").EnumerateObject().Select(property => property.Name));
        EngineRequest copy = EngineJson.ReadRequest(System.Text.Encoding.UTF8.GetBytes(document.RootElement.GetRawText()));
        Assert.Equal([3, 2], copy.Decision!.Targets);
        Assert.Equal([11], copy.Decision.Resources);
    }
}
