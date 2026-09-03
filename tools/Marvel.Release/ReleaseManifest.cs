using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Server;

namespace Marvel.Release;

internal sealed record ReleaseManifest(
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("schema")] int Schema,
    [property: JsonPropertyName("product_version")] string ProductVersion,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("commit")] string Commit,
    [property: JsonPropertyName("assembly_version")] string AssemblyVersion,
    [property: JsonPropertyName("engine")] ReleaseEngineIdentity Engine,
    [property: JsonPropertyName("datasets")] ReleaseDatasetIdentity Datasets)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    public static ReleaseManifest Create(
        ReleaseVersion version,
        string commit,
        string dataRoot)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (commit.Length != 40
            || commit.Any(character =>
                character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException("commit must be 40 lowercase hexadecimal characters");
        }

        if (!string.Equals(
                EngineBuildIdentity.ProductVersion,
                version.Value,
                StringComparison.Ordinal)
            || !string.Equals(EngineBuildIdentity.Commit, commit, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "compiled build identity does not match the requested release identity");
        }

        DatasetGameFactory factory = DatasetGameFactory.Load(dataRoot);
        return new ReleaseManifest(
            "marvel-release",
            1,
            version.Value,
            version.Channel.ToString().ToLowerInvariant(),
            commit,
            version.AssemblyVersion,
            new ReleaseEngineIdentity(
                EngineBuildIdentity.ReplayContract,
                EngineBuildIdentity.RngContract,
                EngineBuildIdentity.StateDigest,
                EngineProtocol.Version,
                EngineBuildIdentity.SaveSchema),
            new ReleaseDatasetIdentity(
                factory.Compatibility.CardsSha256,
                factory.Compatibility.SetupSha256,
                factory.Compatibility.AbilitiesSha256));
    }

    public string Json() => JsonSerializer.Serialize(this, Options) + "\n";
}

internal sealed record ReleaseEngineIdentity(
    [property: JsonPropertyName("replay_contract")] string ReplayContract,
    [property: JsonPropertyName("rng_contract")] string RngContract,
    [property: JsonPropertyName("state_digest")] string StateDigest,
    [property: JsonPropertyName("protocol")] int Protocol,
    [property: JsonPropertyName("save_schema")] int SaveSchema);

internal sealed record ReleaseDatasetIdentity(
    [property: JsonPropertyName("cards_sha256")] string CardsSha256,
    [property: JsonPropertyName("setup_sha256")] string SetupSha256,
    [property: JsonPropertyName("abilities_sha256")] string AbilitiesSha256);
