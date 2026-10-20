using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignWriterLeaseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriterLeaseIsPrivateAndExclusiveForNewAndExistingFiles(bool existing)
    {
        string root = Path.Combine(Path.GetTempPath(), $"marvel-campaign-lease-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, ".writer");
        try
        {
            if (existing)
            {
                File.WriteAllText(path, "");
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite
                        | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }
            using (var first = new FileCampaignStore(root))
            {
                Assert.Throws<IOException>(() => new FileCampaignStore(root));
                if (!OperatingSystem.IsWindows())
                    Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
            }
            using var reopened = new FileCampaignStore(root);
            Assert.Empty(reopened.Load());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
