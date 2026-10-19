using Marvel.Desktop;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DesktopSaveIsolationTests
{
    [Fact]
    public void TestProcessChoosesTemporaryStorageBeforeDesktopComposition()
    {
        string configured = Assert.IsType<string>(Environment.GetEnvironmentVariable("MARVEL_SAVE_DIRECTORY"));
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "marvel-desktop-tests"), configured, StringComparison.Ordinal);
        Assert.Equal(Path.GetFullPath(configured), DesktopSaveDirectory.Resolve());
        Assert.Equal(Path.GetFullPath(Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "MarvelLCG", "Saves")), DesktopSaveDirectory.Resolve(""));
    }

    [Fact]
    public void SeparateRootsCanRunTogetherButSameRootRetainsExclusiveWriter()
    {
        string firstRoot = TemporaryRoot();
        string secondRoot = TemporaryRoot();
        using var first = new DesktopPersistentHosts();
        using var second = new DesktopPersistentHosts();
        var endpoint = first.Get(RepositoryPaths.Root, ClientComposition.ProcessLog, firstRoot);
        Assert.Same(endpoint, first.Get(RepositoryPaths.Root, ClientComposition.ProcessLog, firstRoot));
        Assert.NotSame(endpoint, first.Get(RepositoryPaths.Root, ClientComposition.ProcessLog, secondRoot));
        Assert.True(File.Exists(Path.Combine(firstRoot, "Campaigns", ".writer")));
        Assert.True(File.Exists(Path.Combine(secondRoot, "Campaigns", ".writer")));
        Assert.Throws<DesktopStorageException>(() => second.Get(RepositoryPaths.Root, ClientComposition.ProcessLog, firstRoot));
        first.Dispose();
        Assert.NotNull(second.Get(RepositoryPaths.Root, ClientComposition.ProcessLog, firstRoot));
    }

    [Fact]
    public async Task BusyStorageReportsRecoveryWithoutFallbackAndCanRetryAfterRelease()
    {
        string root = TemporaryRoot();
        using (var lease = new FileCampaignStore(Path.Combine(root, "Campaigns")))
        {
            var blocked = LocalClientConnectionFactory.Connect(RepositoryPaths.Root, root);
            Assert.False(blocked.Succeeded);
            Assert.Null(blocked.Client);
            Assert.Equal("save_storage_unavailable", blocked.Error?.Code);
            Assert.Contains("Close another game", blocked.Error!.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(root, blocked.Error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Core Set", blocked.Error.Message, StringComparison.Ordinal);
        }
        var recovered = LocalClientConnectionFactory.Connect(RepositoryPaths.Root, root);
        Assert.True(recovered.Succeeded, recovered.Error?.Message);
        await using var client = recovered.Client!;
        Assert.True((await client.ReadSetupAsync(TestContext.Current.CancellationToken)).Succeeded);
    }

    [Fact]
    public void InvalidExplicitSavePathDoesNotBecomeAContentErrorOrFallback()
    {
        var connection = LocalClientConnectionFactory.Connect(RepositoryPaths.Root, "invalid\0save");
        Assert.False(connection.Succeeded);
        Assert.Null(connection.Client);
        Assert.Equal("save_storage_unavailable", connection.Error?.Code);
    }

    private static string TemporaryRoot() => Path.Combine(Path.GetTempPath(), "marvel-save-isolation", Guid.NewGuid().ToString("N"));
}
