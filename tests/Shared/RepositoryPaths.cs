namespace Marvel.Tests;

/// <summary>Finds the repository's committed datasets from the test binary.</summary>
/// <remarks>
/// <para>
/// The cross-language fixtures live in <c>datasets/</c> at the repository root
/// and are shared with the Python engine, so they cannot be copied next to the
/// test assembly without becoming a second copy that can drift from the first.
/// The test walks up to find them instead.
/// </para>
/// <para>
/// Linked into every test project rather than duplicated per project, for the
/// same reason the datasets themselves are not copied. There were two identical
/// copies before <c>Marvel.Content.Tests</c> would have made a third.
/// </para>
/// </remarks>
internal static class RepositoryPaths
{
    /// <summary>The repository root, found by walking up from the test binary.</summary>
    public static string Root { get; } = Find();

    /// <summary>A path under <c>datasets/</c>.</summary>
    public static string Dataset(params string[] parts) =>
        Path.Combine([Root, "datasets", .. parts]);

    /// <summary>A path anywhere under the repository root.</summary>
    /// <remarks>
    /// For the documents a test holds the code against — <c>docs/card-dsl.md</c>
    /// says what the ability vocabulary is, and a vocabulary nobody wrote down
    /// is one nobody can author against.
    /// </remarks>
    public static string Repository(params string[] parts) =>
        Path.Combine([Root, .. parts]);

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            // `datasets/` alone is not distinctive enough to stop on; the
            // solution file beside it is what identifies the root.
            if (Directory.Exists(Path.Combine(directory.FullName, "datasets"))
                && File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"no repository root above {AppContext.BaseDirectory}");
    }
}
