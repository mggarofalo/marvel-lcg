using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PackagedHostedSmokeTests
{
    [Fact]
    public void ExactPackagedSmokeArgumentIsRequired()
    {
        Assert.True(PackagedHostedSmoke.IsRequested(
            ["--marvel-hosted-multiplayer-smoke"]));
        Assert.False(PackagedHostedSmoke.IsRequested(
            ["--script", "res://smoke/hosted_multiplayer_smoke.gd"]));
        Assert.False(PackagedHostedSmoke.IsRequested(
            ["--marvel-hosted-multiplayer-smoke-extra"]));
    }
}
