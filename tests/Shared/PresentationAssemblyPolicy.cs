using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Marvel.Testing;

internal static class PresentationAssemblyPolicy
{
    public static void MatchesReviewedMarvelAssemblies(
        Assembly assembly,
        params string[] reviewed)
    {
        string[] actual = Reviewed(
            assembly.GetReferencedAssemblies().Select(reference => reference.Name!),
            "Marvel.");
        Assert.Equal(reviewed.Order(StringComparer.Ordinal), actual);
    }

    public static void MatchesReviewedMarvelTypes(
        Assembly assembly,
        params string[] reviewed)
    {
        using FileStream stream = File.OpenRead(assembly.Location);
        using var executable = new PEReader(stream);
        MetadataReader metadata = executable.GetMetadataReader();
        string[] referenced = metadata.TypeReferences
            .Select(metadata.GetTypeReference)
            .Select(reference =>
                $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}")
            .ToArray();
        string[] actual = Reviewed(referenced, "Marvel.");
        Assert.Equal(reviewed.Order(StringComparer.Ordinal), actual);
    }

    public static string[] UnexpectedMarvelAssemblies(
        IEnumerable<string> referenced,
        IEnumerable<string> allowed) =>
        Unexpected(referenced, allowed, "Marvel.");

    public static string[] UnexpectedMarvelTypes(
        IEnumerable<string> referenced,
        IEnumerable<string> allowed) =>
        Unexpected(referenced, allowed, "Marvel.");

    private static string[] Unexpected(
        IEnumerable<string> referenced,
        IEnumerable<string> allowed,
        string prefix)
    {
        var accepted = allowed.ToHashSet(StringComparer.Ordinal);
        return referenced
            .Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
            .Where(value => !accepted.Contains(value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] Reviewed(IEnumerable<string> referenced, string prefix) =>
        referenced
            .Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
