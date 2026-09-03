using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Marvel.Testing;

internal static class PresentationAssemblyPolicy
{
    public static void AllowsOnlyMarvelAssemblies(
        Assembly assembly,
        params string[] allowed)
    {
        string[] unexpected = UnexpectedMarvelAssemblies(
            assembly.GetReferencedAssemblies().Select(reference => reference.Name!),
            allowed);
        Assert.True(unexpected.Length == 0,
            $"{assembly.GetName().Name} reached unreviewed Marvel assemblies: "
                + string.Join(", ", unexpected));
    }

    public static void AllowsOnlyMarvelTypes(
        Assembly assembly,
        params string[] allowed)
    {
        using FileStream stream = File.OpenRead(assembly.Location);
        using var executable = new PEReader(stream);
        MetadataReader metadata = executable.GetMetadataReader();
        string[] referenced = metadata.TypeReferences
            .Select(metadata.GetTypeReference)
            .Select(reference =>
                $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}")
            .ToArray();
        string[] unexpected = UnexpectedMarvelTypes(referenced, allowed);
        Assert.True(unexpected.Length == 0,
            $"{assembly.GetName().Name} reached unreviewed Marvel types: "
                + string.Join(", ", unexpected));
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
}
