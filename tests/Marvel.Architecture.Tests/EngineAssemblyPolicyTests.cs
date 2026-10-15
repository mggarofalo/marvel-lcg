using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Marvel.Testing;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class EngineAssemblyPolicyTests
{
    // Dependency direction is our architecture choice. Content supplies rules-owned
    // ports; hosts supply bytes and transport. Neither dependency points outward.
    [Theory]
    [InlineData("Marvel.Core", new string[0])]
    [InlineData("Marvel.Rules", new[] { "Marvel.Core" })]
    [InlineData("Marvel.Cards", new[] { "Marvel.Core", "Marvel.Rules" })]
    [InlineData("Marvel.Content", new[] { "Marvel.Rules" })]
    [InlineData("Marvel.Session", new[] { "Marvel.Core", "Marvel.Rules" })]
    public void EngineDependenciesPointInward(string name, string[] allowed) =>
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            Assembly.Load(new AssemblyName(name)), allowed);

    [Theory]
    [InlineData("Marvel.Core")]
    [InlineData("Marvel.Rules")]
    [InlineData("Marvel.Cards")]
    [InlineData("Marvel.Content")]
    [InlineData("Marvel.Session")]
    public void EngineCannotAcquireAmbientCapabilities(string name)
    {
        Assembly assembly = Assembly.Load(new AssemblyName(name));
        using var stream = File.OpenRead(assembly.Location);
        using var executable = new PEReader(stream);
        MetadataReader metadata = executable.GetMetadataReader();
        string[] violations = metadata.TypeReferences
            .Select(metadata.GetTypeReference)
            .Select(type => $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}")
            .Where(IsAmbientCapability)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(violations);
        Assert.Empty(metadata.MemberReferences.Select(metadata.GetMemberReference)
            .Where(member => member.Parent.Kind == HandleKind.TypeReference)
            .Where(member => IsAmbientMember(metadata, member))
            .Select(member => metadata.GetString(member.Name)));
    }

    [Theory]
    [InlineData("System.IO.File")]
    [InlineData("System.IO.FileStream")]
    [InlineData("System.IO.DirectoryInfo")]
    [InlineData("System.Net.Http.HttpClient")]
    [InlineData("System.Threading.Tasks.Task")]
    [InlineData("System.Threading.Thread")]
    [InlineData("System.DateTime")]
    [InlineData("System.Random")]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator")]
    public void CapabilityPolicyRejectsExternalState(string type) =>
        Assert.True(IsAmbientCapability(type));

    [Theory]
    [InlineData("System.IO.Stream")]
    [InlineData("System.IO.MemoryStream")]
    [InlineData("System.Text.Json.JsonDocument")]
    [InlineData("System.Security.Cryptography.SHA256")]
    public void CapabilityPolicyPermitsSuppliedBytesAndDeterministicHashing(string type) =>
        Assert.False(IsAmbientCapability(type));

    private static bool IsAmbientCapability(string type) =>
        type.StartsWith("System.Net.", StringComparison.Ordinal)
        || type.StartsWith("System.Threading.", StringComparison.Ordinal)
        || type is "System.IO.File" or "System.IO.Directory" or "System.IO.FileInfo"
            or "System.IO.DirectoryInfo" or "System.IO.FileStream" or "System.IO.FileSystemWatcher"
            or "System.DateTime" or "System.DateTimeOffset" or "System.TimeProvider"
            or "System.Random" or "System.Console"
            or "System.Diagnostics.Process" or "System.Diagnostics.Stopwatch"
            or "System.Security.Cryptography.RandomNumberGenerator";

    private static bool IsAmbientMember(MetadataReader metadata, MemberReference member)
    {
        TypeReference type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
        string owner = $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}";
        // Iterator state machines use the current thread id to reuse their enumerator.
        // It is compiler plumbing, not an environmental input to game decisions.
        return owner == "System.Environment"
            && metadata.GetString(member.Name) != "get_CurrentManagedThreadId"
            || owner == "System.Guid" && metadata.GetString(member.Name) == "NewGuid";
    }
}
