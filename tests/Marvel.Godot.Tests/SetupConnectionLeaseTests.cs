using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SetupConnectionLeaseTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndingAnEntryAttemptReleasesOnlyUnadoptedConnections(bool adopted)
    {
        await using var transport = new QueuedInProcessTransport(Host());
        using var client = new LocalGameClient(transport);
        using (var attempt = new SetupConnectionLease(client))
        {
            Assert.True((await client.ReadSetupAsync(TestContext.Current.CancellationToken)).Succeeded);
            if (adopted) attempt.TransferToSession();
        }
        ClientSetupResult result = await client.ReadSetupAsync(TestContext.Current.CancellationToken);
        Assert.Equal(adopted, result.Succeeded);
        if (!adopted)
        {
            Assert.Equal("transport_unavailable", result.Error?.Code);
            await transport.ShutdownCompletion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }
}
