using System.Reflection;
using Marvel.Protocol;

namespace Marvel.Observability;

/// <summary>Product provenance shared by clients and services without loading an engine.</summary>
public static class BuildIdentity
{
    /// <summary>The build's product version.</summary>
    public static string ProductVersion { get; } = Metadata("MarvelProductVersion");
    /// <summary>The build's source commit.</summary>
    public static string Commit { get; } = Metadata("MarvelCommit");
    /// <summary>Readable product provenance.</summary>
    public static string Display => $"v{ProductVersion} · protocol {EngineProtocol.Version}";

    private static string Metadata(string key) => typeof(BuildIdentity).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == key).Value
        ?? throw new InvalidOperationException($"Build metadata {key} is missing.");
}
