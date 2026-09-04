using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngineBuildIdentityTests
{
    [Fact]
    public void RuntimeAndDatasetFactoryShareOneCompiledCompatibilityIdentity()
    {
        DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);

        Assert.Equal("0.1.0-dev.0", EngineBuildIdentity.ProductVersion);
        Assert.Equal("local", EngineBuildIdentity.Commit);
        Assert.Equal("engine-replay-v1", EngineBuildIdentity.ReplayContract);
        Assert.Equal("mt19937-iso-cxx", EngineBuildIdentity.RngContract);
        Assert.Equal("state-digest-v2", EngineBuildIdentity.StateDigest);
        Assert.Equal(SessionSave.CurrentSchema, EngineBuildIdentity.SaveSchema);
        Assert.Equal(EngineBuildIdentity.ProductVersion, factory.Compatibility.Application);
        Assert.Equal(EngineBuildIdentity.ReplayContract, factory.Compatibility.ReplayContract);
        Assert.Equal(EngineBuildIdentity.RngContract, factory.Compatibility.RngContract);
        Assert.Equal(EngineBuildIdentity.StateDigest, factory.Compatibility.StateDigest);
    }

    [Fact]
    public void AssemblyAndDisplayMetadataCarryTheDeveloperIdentity()
    {
        Version? assembly = typeof(EngineBuildIdentity).Assembly.GetName().Version;

        Assert.Equal(new Version(0, 1, 0, 0), assembly);
        Assert.Equal(
            "v0.1.0-dev.0 · engine engine-replay-v1 · protocol 12 · save 2",
            EngineBuildIdentity.Display);
    }
}
