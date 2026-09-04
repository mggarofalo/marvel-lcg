using Marvel.Behavior.Run;
using Marvel.Tests;

namespace Marvel.Content.Tests.Behavior;

internal static class CoreTranscriptCorpus
{
    private static readonly Lazy<IReadOnlyList<TranscriptResult>> Results = new(
        () => new CoreTranscriptSuite(RepositoryPaths.Root).RunPassingCorpus());

    public static IReadOnlyList<TranscriptResult> All => Results.Value;
}
