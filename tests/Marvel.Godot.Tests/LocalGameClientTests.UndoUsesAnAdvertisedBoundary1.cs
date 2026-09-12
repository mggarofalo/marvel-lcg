using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;
public sealed class LocalGameClientUndoUsesAnAdvertisedBoundaryTests : LocalGameClientTestBase
{
    [Fact]
    public async Task UndoUsesAnAdvertisedBoundaryAndReturnsTheReplayedTable()
    {
        var client = new LocalGameClient(new InProcessTransport(Host()));
        ClientEntryResult opened = await client.OpenSessionAsync("undo-table", Specification(), TestContext.Current.CancellationToken);
        ClientResolutionResult kept = await client.ResolveAsync(opened.Session!, VisibleDecision(opened.Response!.Prompt!), TestContext.Current.CancellationToken);
        Affordance changeForm = Assert.Single(kept.Response!.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        ClientResolutionResult changed = await client.ResolveAsync(opened.Session!, new EngineDecision(changeForm.Id, []), TestContext.Current.CancellationToken);
        ClientResolutionResult undone = await client.UndoAsync(opened.Session!, changed.Response!.History!.Cursor - 1, TestContext.Current.CancellationToken);
        Assert.True(undone.Succeeded, undone.Error?.Message);
        Assert.Empty(undone.Response!.Events);
        Assert.Equal(changed.Response.Revision + 1, undone.Response.Revision);
        Assert.Contains(undone.Response.World!.Areas.SelectMany(area => area.Cards), card => card.Face?.Title == "Peter Parker");
        Assert.DoesNotContain(undone.Response.World.Areas.SelectMany(area => area.Cards), card => card.Face?.Title == "Spider-Man");
    }

    [Fact]
    public async Task UndoRefusesAHistoryBoundaryTheServerDidNotAdvertise()
    {
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);
        ClientResolutionResult result = await client.UndoAsync(new ClientSession("game", "capability"), 0, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.NotSent, result.MutationDisposition);
        Assert.Equal("history_unavailable", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OpeningATwoSeatSessionPreservesHeroOrderAndSeparatesBearerMaterial()
    {
        SetupChoices choices = Choices();
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("source", "table-alpha", new GameSpecification("rhino", ["spider_man", "captain_marvel"], null, Seed: 7)));
        var transport = new ScriptedTransport(complete with { RequestId = "local-open", Capability = "owner-secret", Invitations = [new SeatInvitation(1, "seat-secret")], });
        var selection = DefaultSelection(choices)with
        {
            HeroKeys = ["captain_marvel", "spider_man"],
        };
        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync("table-alpha", choices, selection, TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(new ClientSession("table-alpha", "owner-secret"), result.Session);
        Assert.Equal(["captain_marvel", "spider_man"], Assert.Single(transport.Requests).Game?.Heroes);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        SeatInvitation invitation = Assert.Single(result.Invitations);
        Assert.Equal(1, invitation.Seat);
        Assert.Equal("seat-secret", invitation.Invitation);
    }

    [Fact]
    public async Task AttachAcceptsAWaitingViewAndSendsTheInvitationExactlyOnce()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(complete with { RequestId = "local-attach", Capability = "attached-secret", Prompt = null, });
        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync("shared-table", "one-time-secret", TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(new ClientSession("shared-table", "attached-secret"), result.Session);
        Assert.Null(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Attach, request.Operation);
        Assert.Equal("one-time-secret", request.Capability);
    }

    [Fact]
    public async Task RestrictedSessionInvitationAttachesOnceToItsAuthorizedSeat()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilityIssuer("owner", "invitation", "guest"), new RestrictedVisibilityPolicy(0));
        var client = new LocalGameClient(new InProcessTransport(host));
        ClientEntryResult owner = await client.OpenSessionAsync("shared-table", new GameSpecification("rhino", ["spider_man", "captain_marvel"], null, Seed: 7), TestContext.Current.CancellationToken);
        SeatInvitation invitation = Assert.Single(owner.Invitations);
        ClientEntryResult guest = await client.AttachAsync("shared-table", invitation.Invitation, TestContext.Current.CancellationToken);
        ClientEntryResult reused = await client.AttachAsync("shared-table", invitation.Invitation, TestContext.Current.CancellationToken);
        Assert.True(owner.Succeeded, owner.Error?.Message);
        Assert.True(guest.Succeeded, guest.Error?.Message);
        Assert.Equal(1, invitation.Seat);
        Assert.Equal(new ClientSession("shared-table", "guest"), guest.Session);
        Assert.Null(guest.Response?.Prompt);
        Assert.Equal("invitation_unavailable", reused.Error?.Code);
    }

    [Fact]
    public async Task UnavailableInvitationIsMappedWithoutEchoingOrRetryingItsSecret()
    {
        const string secret = "expired-seat-secret";
        var transport = new ScriptedTransport(new EngineResponse(EngineProtocol.Version, "local-attach", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError("session_not_found", secret)));
        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync("shared-table", secret, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal("invitation_unavailable", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task UnexpectedAttachFailuresAlsoDoNotEchoInvitationDiagnostics()
    {
        const string secret = "one-time-seat-secret";
        var transport = new ScriptedTransport(new EngineResponse(EngineProtocol.Version, "local-attach", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError(secret, secret)));
        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync("shared-table", secret, TestContext.Current.CancellationToken);
        Assert.Equal("attach_failed", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task AttachPersistenceFailureKeepsItsBoundedOperationalClassification()
    {
        const string secret = "one-time-seat-secret";
        var transport = new ScriptedTransport(new EngineResponse(EngineProtocol.Version, "local-attach", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError("save_failed", "private storage diagnostic")));
        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync("shared-table", secret, TestContext.Current.CancellationToken);
        Assert.Equal("save_failed", result.Error?.Code);
        Assert.DoesNotContain("private", result.Error?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData("", "invitation", "invalid_game_id")]
    [InlineData("   ", "invitation", "invalid_game_id")]
    [InlineData("game", "", "invalid_invitation")]
    [InlineData("game", "   ", "invalid_invitation")]
    public async Task InvalidAttachIdentifiersAreRejectedBeforeTransport(string gameId, string invitation, string expectedCode)
    {
        var transport = new CapturingTransport();
        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync(gameId, invitation, TestContext.Current.CancellationToken);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OversizedEntryIdentifiersAreRejectedWithoutEchoingThem()
    {
        string secret = new('s', EngineProtocol.MaximumIdentifierLength + 1);
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);
        ClientEntryResult open = await client.OpenSessionAsync(secret, Specification(), TestContext.Current.CancellationToken);
        ClientEntryResult attach = await client.AttachAsync("game", secret, TestContext.Current.CancellationToken);
        Assert.Equal("invalid_game_id", open.Error?.Code);
        Assert.Equal("invalid_invitation", attach.Error?.Code);
        Assert.DoesNotContain(secret, open.Error?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, attach.Error?.Message, StringComparison.Ordinal);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task ExplicitSessionResolveAndRecoveryKeepTheSessionGameId()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-resolve", Capability = null, Error = new EngineError("stale_decision", "The prompt changed."), Prompt = null, World = null, Events = [], }, current with { RequestId = "local-recover", Capability = null, Events = [] });
        var session = new ClientSession("shared-table", current.Capability!);
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(session, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("stale_decision", result.Error?.Code);
        Assert.Equal(["shared-table", "shared-table"], transport.Requests.Select(request => request.GameId));
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ResolveKeepsAnAuthoritativeWaitingViewForTheOtherSeat()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-resolve", Capability = null, Prompt = null, Revision = current.Revision + 1, });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.True(result.HasAuthoritativeView);
        Assert.Null(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task RecoveryKeepsAnAuthoritativeWaitingViewForTheOtherSeat()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(new IOException("response lost"), current with { RequestId = "local-recover", Capability = null, Prompt = null, Events = [], });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.True(result.HasAuthoritativeView);
        Assert.Null(result.Response?.Prompt);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task InvalidSessionIsRejectedBeforeResolveTransport()
    {
        var transport = new CapturingTransport();
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("game", ""), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal("invalid_session", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task OpenRejectsUnsupportedPlayerCountsBeforeTransport(int playerCount)
    {
        string[] heroes = ["spider_man", "captain_marvel", "black_panther"];
        var transport = new CapturingTransport();
        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync("table", new GameSpecification("rhino", heroes.Take(playerCount).ToList(), null, Seed: 7), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_selection", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OpenRejectsDuplicateHeroesBeforeTransport()
    {
        var transport = new CapturingTransport();
        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync("table", new GameSpecification("rhino", ["spider_man", "spider_man"], null, Seed: 7), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_selection", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }
}
