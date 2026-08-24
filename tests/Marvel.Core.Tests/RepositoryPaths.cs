namespace Marvel.Core.Tests;

/// <summary>Finds the repository's committed datasets from the test binary.</summary>
/// <remarks>
/// The cross-language fixtures live in <c>datasets/</c> at the repository root
/// and are shared with the Python engine, so they cannot be copied next to the
/// test assembly without becoming a second copy that can drift from the first.
/// The test walks up to find them instead.
/// </remarks>
internal static class RepositoryPaths
{
    /// <summary>The repository root, found by walking up from the test binary.</summary>
    public static string Root { get; } = Find();

    /// <summary>A path under <c>datasets/</c>.</summary>
    public static string Dataset(params string[] parts) =>
        Path.Combine([Root, "datasets", .. parts]);

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
