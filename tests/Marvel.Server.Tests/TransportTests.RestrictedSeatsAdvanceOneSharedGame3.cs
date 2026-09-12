using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class TransportRestrictedSeatsAdvanceOneSharedGameTests : TransportTestBase
{
    [Theory]
    [InlineData("omitted")]
    [InlineData("other-seat")]
    [InlineData("watch")]
    [InlineData("hot-seat")]
    public async Task RestrictedSeatsAdvanceOneSharedGameWithIndependentCapabilities(string viewerMode)
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root), new SequenceCapabilities("seat-zero", "seat-zero", "invite-one", "seat-one"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);
        ViewerClaim? viewer = RestrictedViewer(viewerMode);
        EngineResponse opened = await ExchangeOverSocket(server, EngineRequest.OpenGame("open", "shared", specification, viewer));
        SeatInvitation invitation = Assert.Single(opened.Invitations!);
        EngineResponse capabilityCannotAttach = await ExchangeOverSocket(server, EngineRequest.AttachGame("steal-seat", "shared", opened.Capability!));
        EngineResponse assertionCannotAttach = await ExchangeOverSocket(server, EngineRequest.AttachGame("assert-seat", "shared", invitation.Invitation)with { Viewer = new ViewerClaim(Seat: 1), });
        EngineResponse wrongGameCannotAttach = await ExchangeOverSocket(server, EngineRequest.AttachGame("wrong-game", "different", invitation.Invitation));
        EngineResponse attached = await ExchangeOverSocket(server, EngineRequest.AttachGame("attach", "shared", invitation.Invitation));
        Assert.Equal("session_not_found", ErrorCode(capabilityCannotAttach));
        Assert.Equal("invalid_request", ErrorCode(assertionCannotAttach));
        Assert.Equal("session_not_found", ErrorCode(wrongGameCannotAttach));
        Assert.Equal(1, invitation.Seat);
        Assert.Equal("seat-zero", opened.Capability);
        Assert.Equal("seat-one", attached.Capability);
        Assert.Equal(0, PromptPlayer(opened));
        Assert.Null(attached.Prompt);
        Assert.All(Hand(attached, 0), card => Assert.Null(card.Face));
        Assert.All(Hand(attached, 1), card => Assert.NotNull(card.Face));
        EngineResponse guestCannotAnswerOwner = await ExchangeOverSocket(server, EngineRequest.ResolveGame("guest-steal-owner", "shared", attached.Capability!, EngineDecision.Decline));
        EngineResponse ownerPromptRemains = await ExchangeOverSocket(server, EngineRequest.SyncGame("owner-still-pending", "shared", opened.Capability!));
        Assert.Equal("not_your_turn", ErrorCode(guestCannotAnswerOwner));
        Assert.Equal(0, PromptPlayer(ownerPromptRemains));
        EngineResponse forZero = await ExchangeOverSocket(server, EngineRequest.ResolveGame("zero-mulligan", "shared", opened.Capability!, TakeOnly(opened)));
        Assert.Null(forZero.Error);
        Assert.Null(forZero.Prompt);
        EngineResponse forbidden = await ExchangeOverSocket(server, EngineRequest.ResolveGame("steal-one", "shared", opened.Capability!, EngineDecision.Decline));
        Assert.Equal("stale_decision", ErrorCode(forbidden));
        EngineResponse forOne = await ExchangeOverSocket(server, EngineRequest.SyncGame("sync-one", "shared", attached.Capability!));
        Assert.Equal(1, PromptPlayer(forOne));
        EngineResponse replay = await ExchangeOverSocket(server, EngineRequest.AttachGame("replay", "shared", invitation.Invitation));
        Assert.Equal("session_not_found", ErrorCode(replay));
        EngineResponse afterOne = await ExchangeOverSocket(server, EngineRequest.ResolveGame("one-mulligan", "shared", attached.Capability!, TakeOnly(forOne), expectedRevision: forOne.Revision));
        Assert.Null(afterOne.Error);
        Assert.Equal(1, PromptPlayer(afterOne));
        Assert.All(afterOne.Prompt!.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(1, option.AnchorPlayer);
        });
        EngineResponse resumedZero = await ExchangeOverSocket(server, EngineRequest.SyncGame("sync-zero", "shared", opened.Capability!));
        Assert.Equal(0, PromptPlayer(resumedZero));
        Assert.Null((await ExchangeOverSocket(server, EngineRequest.CloseGame("close", "shared", opened.Capability!))).Error);
        EngineResponse afterClose = await ExchangeOverSocket(server, EngineRequest.SyncGame("after-close", "shared", attached.Capability!));
        Assert.Equal("session_not_found", ErrorCode(afterClose));
    }

    [Fact]
    public async Task OwnerCloseInvalidatesPendingSeatInvitations()
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root), new SequenceCapabilities("owner", "invitation"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);
        EngineResponse opened = await ExchangeOverSocket(server, EngineRequest.OpenGame("open", "shared", specification));
        SeatInvitation invitation = Assert.Single(opened.Invitations!);
        EngineResponse closed = await ExchangeOverSocket(server, EngineRequest.CloseGame("close", "shared", opened.Capability!));
        EngineResponse attachAfterClose = await ExchangeOverSocket(server, EngineRequest.AttachGame("attach", "shared", invitation.Invitation));
        Assert.Null(closed.Error);
        Assert.Equal("session_not_found", attachAfterClose.Error?.Code);
    }

    [Fact]
    public async Task GuestCloseRevokesOnlyTheGuestSession()
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root), new SequenceCapabilities("owner", "invitation", "guest"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);
        EngineResponse opened = await ExchangeOverSocket(server, EngineRequest.OpenGame("open", "shared", specification));
        SeatInvitation invitation = Assert.Single(opened.Invitations!);
        EngineResponse attached = await ExchangeOverSocket(server, EngineRequest.AttachGame("attach", "shared", invitation.Invitation));
        EngineResponse closed = await ExchangeOverSocket(server, EngineRequest.CloseGame("close-guest", "shared", attached.Capability!));
        EngineResponse guestAfterClose = await ExchangeOverSocket(server, EngineRequest.SyncGame("sync-guest", "shared", attached.Capability!));
        EngineResponse ownerAfterClose = await ExchangeOverSocket(server, EngineRequest.SyncGame("sync-owner", "shared", opened.Capability!));
        Assert.Null(closed.Error);
        Assert.Equal("session_not_found", guestAfterClose.Error?.Code);
        Assert.Null(ownerAfterClose.Error);
        Assert.Equal(0, ownerAfterClose.Prompt?.Player);
    }

    [Fact]
    public async Task HostedEliminationDeliversThePlayAreaTopologyChange()
    {
        var factory = new EliminatingFactory(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var server = new SocketEngineServer(new EngineHost(factory), IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);
        EngineResponse opened = await ExchangeOverSocket(server, EngineRequest.OpenGame("eliminate", "shared", specification));
        Assert.Null(opened.Error);
        var detached = Assert.Single(opened.Events.OfType<PlayAreaDetached>());
        Assert.Equal(0, detached.PlayArea);
        Assert.DoesNotContain(Assert.IsType<WorldDescriptor>(opened.World).GameAreas, area => area.PlayAreas.Contains(0));
        EngineResponse synced = await ExchangeOverSocket(server, EngineRequest.SyncGame("after-elimination", "shared", opened.Capability!));
        Assert.Null(synced.Error);
    }

    [Fact]
    public async Task HighwayRobberyReturnsFaceUpCardsOnlyToTheirOwnersView()
    {
        var factory = new HighwayRobberyFactory(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var host = new EngineHost(factory, new SequenceCapabilities("seat-zero", "invite-one", "seat-one"), new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man", "she_hulk"], ModularSets: [], Seed: 12345);
        EngineResponse forZero = await ExchangeOverSocket(server, EngineRequest.OpenGame("robbery", "shared", specification));
        SeatInvitation invitation = Assert.Single(forZero.Invitations!);
        EngineResponse forOne = await ExchangeOverSocket(server, EngineRequest.AttachGame("owner", "shared", invitation.Invitation));
        Assert.True(factory.ReturnedForSeatOne >= 0);
        Assert.All(Hand(forZero, 1), card =>
        {
            Assert.Null(card.Id);
            Assert.Null(card.Face);
        });
        Assert.DoesNotContain(forZero.Events.OfType<CardsMoved>().SelectMany(moved => moved.Cards), landing => landing.Card == factory.ReturnedForSeatOne);
        Assert.DoesNotContain(forZero.Events.OfType<CardDetached>(), detached => detached.Card == factory.ReturnedForSeatOne);
        var returned = Assert.Single(Hand(forOne, 1), card => card.Id == factory.ReturnedForSeatOne);
        Assert.NotNull(returned.Face);
        Assert.True(returned.FaceUp);
    }
}
