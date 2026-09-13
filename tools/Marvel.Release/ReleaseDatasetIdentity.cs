using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Server;

namespace Marvel.Release;

internal sealed record ReleaseDatasetIdentity(
    [property: JsonPropertyName("cards_sha256")] string CardsSha256,
    [property: JsonPropertyName("setup_sha256")] string SetupSha256,
    [property: JsonPropertyName("abilities_sha256")] string AbilitiesSha256);
