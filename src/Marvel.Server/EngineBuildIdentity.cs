using System.Reflection;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Non-game identities carried by one compiled engine build.</summary>
public static class EngineBuildIdentity
{
    /// <summary>The deterministic replay contract implemented by this build.</summary>
    public const string ReplayContract = "engine-replay-v1";

    /// <summary>The seeded random-stream contract implemented by this build.</summary>
    public const string RngContract = "mt19937-iso-cxx";

    /// <summary>The canonical hidden-state serialization implemented by this build.</summary>
    public const string StateDigest = "state-digest-v2";

    /// <summary>The session schema written by this build.</summary>
    public const int SaveSchema = SessionSave.CurrentSchema;

    /// <summary>The exact product version selected by the build.</summary>
    public static string ProductVersion { get; } = Metadata("MarvelProductVersion");

    /// <summary>The source commit selected by the build.</summary>
    public static string Commit { get; } = Metadata("MarvelCommit");

    /// <summary>A bounded presentation of the complete desktop build identity.</summary>
    public static string Display =>
        $"v{ProductVersion} · engine {ReplayContract} · protocol {EngineProtocol.Version} · save {SaveSchema}";

    private static string Metadata(string key) =>
        typeof(EngineBuildIdentity).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            .Value
        ?? throw new InvalidOperationException($"assembly metadata {key} is missing");
}
