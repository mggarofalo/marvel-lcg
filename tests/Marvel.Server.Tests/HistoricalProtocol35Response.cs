using System.Text;
using System.Text.Json.Nodes;
using Marvel.Protocol;

namespace Marvel.Server.Tests;

/// <summary>Comparison-only projection of hash-pinned protocol-35 response fixtures.</summary>
internal static class HistoricalProtocol35Response
{
    internal static EngineResponse Read(byte[] original)
    {
        var response = JsonNode.Parse(original)?.AsObject()
            ?? throw new InvalidDataException("the historical response is not an object");
        if (response["version"]?.GetValue<int>() != 35)
            throw new InvalidDataException("the historical comparison requires protocol 35");
        if (response["world"] is JsonObject world)
        {
            if (world.ContainsKey("encounter_groups"))
                throw new InvalidDataException("protocol 35 did not describe encounter groups");
            // These pinned Core/RoRS fixtures predate the multi-villain descriptor.
            // Normalize only the parsed comparison copy; archive bytes, gameplay
            // proofs, and the production decoder's required-field checks are unchanged.
            world.Add("encounter_groups", new JsonArray());
        }
        return EngineJson.ReadResponse(Encoding.UTF8.GetBytes(response.ToJsonString()));
    }
}
