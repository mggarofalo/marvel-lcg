using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicPendingSituationTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegalTwoSeatCoreTableDistinguishesPrimaryOwnerFromOffTurnActionsAndReplacesBothOnSync(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("owner", "invite", "guest"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, 0);
        var local = new InProcessTransport(host);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(server, request)
            : await local.ExchangeAsync(request, TestContext.Current.CancellationToken);
        const string game = "public-owner";
        EngineResponse opened = await Exchange(EngineRequest.OpenGame("open", game,
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 1)));
        EngineResponse attached = await Exchange(EngineRequest.AttachGame("join", game,
            Assert.Single(opened.Invitations!).Invitation));
        Assert.Null(attached.Prompt);
        Assert.Equal(0, attached.World!.Table!.PromptOwner);
        Assert.Equal(PublicDecisionKind.OpeningHand, attached.World.Table.PendingSituation!.Kind);
        await Exchange(EngineRequest.ResolveGame("keep-zero", game, opened.Capability!, TakeOnly(opened), opened.Revision));
        EngineResponse guestOpening = await Exchange(EngineRequest.SyncGame("guest-opening", game, attached.Capability!));
        Assert.Equal(1, guestOpening.World!.Table!.PromptOwner);
        EngineResponse offTurn = await Exchange(EngineRequest.ResolveGame("keep-one", game,
            attached.Capability!, TakeOnly(guestOpening), guestOpening.Revision));

        Assert.Null(offTurn.Error);
        Assert.Equal(1, offTurn.Prompt!.Player);
        Assert.False(offTurn.Prompt.Cancellable);
        Assert.All(offTurn.Prompt.Affordances, offer => Assert.Equal(Game.ActionVerb, offer.Verb));
        Assert.Equal(0, offTurn.World!.Table!.PromptOwner);
        Assert.Equal(0, offTurn.World.Table.ActivePlayer);
        Assert.Equal(1, offTurn.World.Table.ViewedPrivateSeat);
        Assert.Equal(PublicDecisionKind.PlayerAction, offTurn.World.Table.PendingSituation!.Kind);
        Assert.Contains("Spider-Man's turn", offTurn.Prompt.Description);
        Assert.Contains("does not end the active player's turn", offTurn.Prompt.Description);
        Assert.DoesNotContain("Take actions or end your turn", offTurn.Prompt.Description);
        Assert.All(Hand(offTurn, 0), card => Assert.Null(card.Face));
        Assert.All(offTurn.World.PlayerSummaries, summary => Assert.Empty(summary.OfferedDefenders));

        EngineResponse active = await Exchange(EngineRequest.SyncGame("active", game, opened.Capability!));
        EngineResponse ended = await Exchange(EngineRequest.ResolveGame("end", game,
            opened.Capability!, EngineDecision.Decline, active.Revision));
        Assert.Null(ended.Error);
        EngineResponse refreshed = await Exchange(EngineRequest.SyncGame("refresh", game, attached.Capability!));
        Assert.Equal(1, refreshed.Prompt!.Player);
        Assert.Equal(1, refreshed.World!.Table!.PromptOwner);
        Assert.Equal(1, refreshed.World.Table.ActivePlayer);
        Assert.True(refreshed.Prompt.Cancellable);
        Assert.DoesNotContain("Spider-Man's turn", refreshed.Prompt.Description);
    }
}
