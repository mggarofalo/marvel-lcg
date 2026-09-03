namespace Marvel.Release.Tests;

using Xunit;

public sealed class ReleaseVersionTests
{
    [Theory]
    [InlineData("0.1.0-preview.7", "1.1.0.7")]
    [InlineData("0.1.0", "1.1.0.65535")]
    [InlineData("0.1.1-preview.1", "1.1.1.1")]
    [InlineData("65534.65535.65535", "65535.65535.65535.65535")]
    public void ReleaseVersionsMapMonotonicallyToMsix(string value, string expected)
    {
        Assert.Equal(expected, ReleaseVersion.Parse(value).MsixVersion);
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("01.0.0")]
    [InlineData("0.1.0-preview.0")]
    [InlineData("0.1.0-preview.65535")]
    [InlineData("0.1.0-rc.1")]
    [InlineData("0.1.0+metadata")]
    [InlineData("65535.0.0")]
    [InlineData("0.65536.0")]
    public void InvalidReleaseVersionsFailClosed(string value)
    {
        Assert.Throws<ArgumentException>(() => ReleaseVersion.Parse(value));
    }

    [Fact]
    public void DeveloperBuildsCannotClaimTheReleasePackageIdentity()
    {
        ReleaseVersion version = ReleaseVersion.Parse("0.1.0-dev.12");

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => _ = version.MsixVersion);

        Assert.Contains("developer builds", failure.Message, StringComparison.Ordinal);
    }
}
