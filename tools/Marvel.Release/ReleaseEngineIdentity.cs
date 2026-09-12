using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Server;

namespace Marvel.Release;

internal sealed record ReleaseEngineIdentity(
    [property: JsonPropertyName("replay_contract")] string ReplayContract,
    [property: JsonPropertyName("rng_contract")] string RngContract,
    [property: JsonPropertyName("state_digest")] string StateDigest,
    [property: JsonPropertyName("protocol")] int Protocol,
    [property: JsonPropertyName("save_schema")] int SaveSchema);
