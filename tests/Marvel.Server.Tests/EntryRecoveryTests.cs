using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EntryRecoveryTests
{
    [Fact]
    public void LostOpenReplyRecoversSameAuthorityAcrossRestartAndRejectsChangedIntent()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        EngineRequest request = EngineRequest.OpenGame("first", "recover-open",
            new GameSpecification("rhino", ["spider_man"], [], 91));
        EngineResponse opened = host.Exchange(request);
        Assert.Null(opened.Error);
        var restarted = new EngineHost(factory, store: store);
        EngineResponse recovered = restarted.Exchange(request with { RequestId = "retry" });
        Assert.Null(recovered.Error);
        Assert.Equal(opened.Capability, recovered.Capability);
        Assert.Equal("retry", recovered.RequestId);
        Assert.Single(store.Load());
        Assert.Equal("entry_conflict", restarted.Exchange(request with
        {
            Game = request.Game! with { Seed = 92 },
        }).Error?.Code);
        string durable = StoredSessionJson.Write(Assert.Single(store.Load()));
        Assert.DoesNotContain(opened.Capability!, durable, StringComparison.Ordinal);
        Assert.DoesNotContain(request.Entry!.Secret, durable, StringComparison.Ordinal);
        Assert.Null(restarted.Exchange(EngineRequest.CloseGame("close", request.GameId, opened.Capability!)).Error);
        Assert.Equal("session_not_found", new EngineHost(factory, store: store).Exchange(request).Error?.Code);
        Assert.Single(store.Load());
    }

    [Fact]
    public void LostAttachReplyRecoversOnlyTheOriginalClaimAndRevocationIsDurable()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        EngineRequest opening = EngineRequest.OpenGame("open", "recover-seat",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], 91));
        EngineResponse opened = host.Exchange(opening);
        string invitation = Assert.Single(opened.Invitations!).Invitation;
        EngineRequest claim = EngineRequest.AttachGame("attach", opening.GameId, invitation);
        EngineResponse attached = host.Exchange(claim);
        Assert.Null(attached.Error);
        var restarted = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        EngineResponse recovered = restarted.Exchange(claim with { RequestId = "retry" });
        Assert.Null(recovered.Error);
        Assert.Equal(attached.Capability, recovered.Capability);
        Assert.Equal(1, recovered.World!.Table!.ViewedPrivateSeat);
        Assert.Equal("session_not_found", restarted.Exchange(claim with { Entry = EntryClaim.Create() }).Error?.Code);
        Assert.Empty(restarted.Exchange(opening).Invitations!);
        Assert.Null(restarted.Exchange(EngineRequest.CloseGame("leave", opening.GameId, attached.Capability!)).Error);
        Assert.Equal("session_not_found", new EngineHost(factory, store: store).Exchange(claim).Error?.Code);
    }
}
