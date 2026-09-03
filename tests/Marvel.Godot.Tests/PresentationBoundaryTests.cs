using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void GodotUsesOnlyTheRulesTypesNeededByPresentationContracts()
    {
        using FileStream assembly = File.OpenRead(typeof(Main).Assembly.Location);
        using var executable = new PEReader(assembly);
        MetadataReader metadata = executable.GetMetadataReader();
        string[] forbidden = metadata.TypeReferences
            .Select(metadata.GetTypeReference)
            .Select(reference =>
                $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}")
            .Where(type => type.StartsWith("Marvel.Rules.", StringComparison.Ordinal))
            .Where(type => !IsPresentationContract(type))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(forbidden.Length == 0,
            "Marvel.Godot reached gameplay types outside its presentation contracts: "
                + string.Join(", ", forbidden));
    }

    [Fact]
    public void GodotDoesNotReachAuthoritativeEngineAssemblies()
    {
        string[] forbidden = typeof(Main).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is "Marvel.Core" or "Marvel.Cards" or "Marvel.Content"
                or "Marvel.Session")
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(forbidden.Length == 0,
            "Marvel.Godot reached authoritative engine assemblies: "
                + string.Join(", ", forbidden));
    }

    private static bool IsPresentationContract(string type) =>
        type.StartsWith("Marvel.Rules.Prompts.", StringComparison.Ordinal)
        || type.StartsWith("Marvel.Rules.Events.", StringComparison.Ordinal)
        || type is "Marvel.Rules.Play.Outcome"
            or "Marvel.Rules.Play.Resources"
            or "Marvel.Rules.Play.ResourceAllocation";
}
