using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SessionTransactionTests
{
    [Fact]
    public void ProjectionFailureLeavesTheLiveSessionAndStoreUntouched()
    {
        (HostedSession live, SessionTransaction transaction) = Transaction();
        bool committed = false;

        Assert.Throws<InvalidOperationException>(() => transaction.CommitAndPublish(
            live,
            [],
            _ =>
            {
                committed = true;
                return null;
            },
            (_, _) => throw new InvalidOperationException("projection failed")));

        Assert.False(committed);
        Assert.Equal(0, live.Revision);
    }

    [Fact]
    public void PersistenceFailureLeavesTheLiveSessionUntouched()
    {
        (HostedSession live, SessionTransaction transaction) = Transaction();
        bool projected = false;

        Assert.Throws<IOException>(() => transaction.CommitAndPublish(
            live,
            [],
            _ => throw new IOException("commit failed"),
            (_, _) =>
            {
                projected = true;
                return Response();
            }));

        Assert.True(projected);
        Assert.Equal(0, live.Revision);
    }

    [Fact]
    public void SuccessfulTransactionCommitsBeforePublishingTheCandidate()
    {
        (HostedSession live, SessionTransaction transaction) = Transaction();
        var order = new List<string>();

        EngineResponse response = transaction.CommitAndPublish(
            live,
            [],
            stored =>
            {
                order.Add("commit");
                Assert.Equal(0, live.Revision);
                Assert.Equal(1, stored.Save.Revision);
                return "generation";
            },
            (_, _) =>
            {
                order.Add("project");
                return Response();
            });

        Assert.Null(response.Error);
        Assert.Equal(["project", "commit"], order);
        Assert.Equal(1, live.Revision);
    }

    private static (HostedSession Live, SessionTransaction Transaction) Transaction()
    {
        DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(
            "rhino", ["spider_man"], [], Seed: 73);
        OpenedGame opened = factory.Create(specification);
        SessionSave save = SessionSave.Open(
            factory.Compatibility,
            new string('a', 32),
            "transaction-table",
            new SessionSetup("rhino", ["spider_man"], [], 73),
            opened.Game,
            opened.SetupEvents);
        var live = new HostedSession("transaction-table", opened.Game, save);
        return (live, new SessionTransaction(opened.Game, save with { Revision = 1 }));
    }

    private static EngineResponse Response() => new(
        EngineProtocol.Version,
        "request",
        "transaction-table",
        Capability: null,
        Prompt: null,
        Events: []);
}
