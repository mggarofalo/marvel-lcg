using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RecipientPersistenceTests : EngineHostTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitAndEmptyRecipientTracesRestartWithStrictReplay(bool legacyEmpty)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var current = host.Exchange(EngineRequest.OpenGame("open", "recipient-restart",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 1)));
        string capability = RequiredCapability(current);
        EngineResponse Answer(EngineDecision answer)
        {
            var next = host.Exchange(EngineRequest.ResolveGame("answer", "recipient-restart", capability, answer, current.Revision));
            Assert.Null(next.Error);
            return next;
        }
        current = Answer(TakeOnly(current));
        current = Answer(TakeOnly(current));
        int support = Hand(current, 0).Single(card => card.Face?.Id == "01091").Id!.Value;
        current = Answer(PayCard(current.Prompt!, support));
        var action = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == support && offer.DirectRecipientCommit);
        current = Answer(new EngineDecision(action.Id, legacyEmpty ? [] : [action.Targets!.Legal[^1]]));
        host = Restart(factory, store, capability, current);
        if (legacyEmpty)
        {
            Assert.NotEqual(PublicDecisionKind.PlayerAction, current.Prompt!.PublicKind);
            current = Answer(new EngineDecision(current.Prompt.Affordances[^1].Id, []));
        }
        Assert.Equal(PublicDecisionKind.PlayerAction, current.Prompt!.PublicKind);
        Restart(factory, store, capability, current);
    }

    [Fact]
    public void PreviousReplayContractIsRejectedWithoutOverwritingItsSave()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "previous-recipient",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        var stored = Assert.Single(store.Load());
        store.Commit(stored with { Save = stored.Save with
        {
            Compatibility = stored.Save.Compatibility with { ReplayContract = "engine-replay-v11" },
        } });
        var restarted = new EngineHost(factory, store: store);
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame(
            "sync", "previous-recipient", RequiredCapability(opened))).Error?.Code);
        Assert.Equal("engine-replay-v11", Assert.Single(store.Load()).Save.Compatibility.ReplayContract);
    }

    private static EngineHost Restart(DatasetGameFactory factory, MemorySessionStore store,
        string capability, EngineResponse expected)
    {
        var host = new EngineHost(factory, store: store);
        var actual = host.Exchange(EngineRequest.SyncGame("restart", "recipient-restart", capability));
        Assert.Null(actual.Error);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.Prompt!.PublicKind, actual.Prompt!.PublicKind);
        Assert.Equal(Hand(expected, 1).Count, Hand(actual, 1).Count);
        return host;
    }
}
