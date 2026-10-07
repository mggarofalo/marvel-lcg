using System.Net;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CardChoiceTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegalSetupSearchPublishesItsPurposeAndOnlyAuthorizedCandidates(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("owner", "invite", "guest"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, 0);
        var local = new InProcessTransport(host);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(server, request)
            : await local.ExchangeAsync(request, TestContext.Current.CancellationToken);
        const string game = "search-purpose";
        EngineResponse opened = await Exchange(EngineRequest.OpenGame("open", game,
            new GameSpecification("rhino", ["black_panther", "spider_man"], [], Seed: 31)));
        EngineResponse guest = await Exchange(EngineRequest.AttachGame("join", game,
            Assert.Single(opened.Invitations!).Invitation));
        await Exchange(EngineRequest.ResolveGame("keep", game,
            opened.Capability!, TakeOnly(opened), opened.Revision));
        EngineResponse guestOpening = await Exchange(EngineRequest.SyncGame("guest-opening", game, guest.Capability!));
        await Exchange(EngineRequest.ResolveGame("guest-keep", game, guest.Capability!,
            TakeOnly(guestOpening), guestOpening.Revision));
        EngineResponse search = await Exchange(EngineRequest.SyncGame("search", game, opened.Capability!));

        Assert.Null(search.Error);
        Assert.Equal(PublicDecisionKind.CardSearch, search.Prompt!.PublicKind);
        Assert.Equal(PublicDecisionKind.CardSearch, search.World!.Table!.PendingSituation!.Kind);
        Assert.False(search.Prompt.Cancellable);
        Assert.NotEmpty(search.Prompt.Affordances);
        Assert.All(search.Prompt.Affordances, offer => Assert.StartsWith("Add ", offer.CommitLabel));
        if (socket) Assert.False(search.Prompt.ExposesConcealedCandidates);
        EngineResponse waiting = await Exchange(EngineRequest.SyncGame("waiting", game, guest.Capability!));
        Assert.Null(waiting.Prompt);
        Assert.Equal(PublicDecisionKind.CardSearch, waiting.World!.Table!.PendingSituation!.Kind);
        Assert.All(Hand(waiting, 0), card => Assert.Null(card.Face));
        Assert.All(Assert.Single(waiting.World.Areas,
            area => area.Owner == 0 && area.Zone == "PlayerDeck").Cards, card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
    }
}
