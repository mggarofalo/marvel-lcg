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
public sealed class LocalGameClientEngineRejectionsTests : LocalGameClientTestBase
{
    [Fact]
    public async Task EngineRejectionsAreBoundedBeforeDisplay()
    {
        string diagnostic = new('x', 400);
        var response = new EngineResponse(EngineProtocol.Version, "local-open", LocalGameSession.GameId, Capability: null, Prompt: null, Events: [], Error: new EngineError(diagnostic, diagnostic));
        ClientStartupResult startup = await new LocalGameClient(new FixedTransport(response)).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.False(startup.Succeeded);
        Assert.Equal(240, startup.Error?.Code.Length);
        Assert.Equal(240, startup.Error?.Message.Length);
    }

    [Fact]
    public async Task IncompleteSuccessfulOpenResponsesAreRejected()
    {
        var response = new EngineResponse(EngineProtocol.Version, "local-open", LocalGameSession.GameId, "capability", Prompt: null, Events: []);
        ClientStartupResult startup = await new LocalGameClient(new FixedTransport(response)).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task ASuccessfulResponseWithoutEventsIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        ClientStartupResult startup = await new LocalGameClient(new FixedTransport(complete with { Events = null! })).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task AnIncompletePromptIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        ClientStartupResult startup = await new LocalGameClient(new FixedTransport(complete with { Prompt = complete.Prompt!with { Affordances = null! }, })).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task AnEmptyPromptIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        ClientStartupResult startup = await new LocalGameClient(new FixedTransport(complete with { Prompt = complete.Prompt!with { Affordances = [] }, })).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task ATerminalWorldWithAStalePromptIsNeverRenderedAsAuthoritative()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        EngineResponse malformed = complete with
        {
            Capability = null,
            World = complete.World!with
            {
                Outcome = Outcome.PlayersWin
            },
        };
        var transport = new ScriptedTransport(malformed, malformed);
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(complete.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.False(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task IncompleteBoardCollectionsAreRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        WorldDescriptor world = Assert.IsType<WorldDescriptor>(complete.World);
        AreaDescriptor area = world.Areas[0];
        CardDescriptor readable = world.Areas.SelectMany(candidate => candidate.Cards.Concat(candidate.Removed)).First(card => card.Face is not null);
        AreaDescriptor withReadable = world.Areas.First(candidate => candidate.Cards.Contains(readable) || candidate.Removed.Contains(readable));
        WorldDescriptor[] incomplete = [world with
        {
            Players = null!
        }, world with
        {
            GameAreas = null!
        }, world with
        {
            Areas = [area with
            {
                Cards = null!
            }

            ]
        }, world with
        {
            Areas = [area with
            {
                Removed = null!
            }

            ]
        }, world with
        {
            Areas = [withReadable with
            {
                Cards = withReadable.Cards.Contains(readable) ? [readable with
                {
                    Face = readable.Face!with
                    {
                        Fields = null!
                    }
                }

                ] : [],
                Removed = withReadable.Removed.Contains(readable) ? [readable with
                {
                    Face = readable.Face!with
                    {
                        Fields = null!
                    }
                }

                ] : [],
            }, ],
        }, ];
        foreach (WorldDescriptor malformed in incomplete)
        {
            ClientStartupResult startup = await new LocalGameClient(new FixedTransport(complete with { World = malformed })).OpenAsync(Specification(), TestContext.Current.CancellationToken);
            Assert.Equal("invalid_response", startup.Error?.Code);
        }
    }

    [Fact]
    public async Task InvalidSelectionsSendNoOpenRequest()
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);
        ClientStartupResult invalidHero = await client.OpenAsync(choices, DefaultSelection(choices)with { HeroKeys = ["wolverine"] }, TestContext.Current.CancellationToken);
        ClientStartupResult invalidModular = await client.OpenAsync(choices, DefaultSelection(choices)with { Modular = ModularConfiguration.Selected, ModularKeys = ["mojo_mania"], }, TestContext.Current.CancellationToken);
        ClientStartupResult invalidSeed = await client.OpenAsync(choices, DefaultSelection(choices)with { Seed = "-1" }, TestContext.Current.CancellationToken);
        ClientStartupResult duplicateModular = await client.OpenAsync(choices, DefaultSelection(choices)with { Modular = ModularConfiguration.Selected, ModularKeys = ["bomb_scare", "bomb_scare"], }, TestContext.Current.CancellationToken);
        ClientStartupResult nullModular = await client.OpenAsync(choices, DefaultSelection(choices)with { ModularKeys = null! }, TestContext.Current.CancellationToken);
        ClientStartupResult nullSeed = await client.OpenAsync(choices, DefaultSelection(choices)with { Seed = null! }, TestContext.Current.CancellationToken);
        Assert.Equal("invalid_selection", invalidHero.Error?.Code);
        Assert.Equal("invalid_selection", invalidModular.Error?.Code);
        Assert.Equal("invalid_seed", invalidSeed.Error?.Code);
        Assert.Equal("invalid_selection", duplicateModular.Error?.Code);
        Assert.Equal("invalid_selection", nullModular.Error?.Code);
        Assert.Equal("invalid_seed", nullSeed.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Theory]
    [InlineData(ModularConfiguration.Recommended, null)]
    [InlineData(ModularConfiguration.None, "")]
    [InlineData(ModularConfiguration.Selected, "bomb_scare,masters_of_evil")]
    public async Task ModularConfigurationPreservesTheThreeProtocolMeanings(ModularConfiguration configuration, string? expected)
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();
        await new LocalGameClient(transport).OpenAsync(choices, DefaultSelection(choices)with { Modular = configuration, ModularKeys = expected is { Length: > 0 } ? Enumerable.Reverse(expected.Split(',')).ToArray() : [], Seed = uint.MaxValue.ToString(), }, TestContext.Current.CancellationToken);
        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Open, request.Operation);
        Assert.Equal(uint.MaxValue, request.Game?.Seed);
        if (expected is null)
        {
            Assert.Null(request.Game?.ModularSets);
        }
        else if (expected.Length == 0)
        {
            Assert.Empty(request.Game!.ModularSets!);
        }
        else
        {
            Assert.Equal(expected.Split(','), request.Game?.ModularSets);
        }
    }

    [Fact]
    public async Task BlankSeedUsesPreGameEntropyAsAnExplicitReplayableSeed()
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport, seedSource: () => 4294967290);
        await client.OpenAsync(choices, DefaultSelection(choices)with { Seed = "  " }, TestContext.Current.CancellationToken);
        Assert.Equal(4294967290u, Assert.Single(transport.Requests).Game?.Seed);
    }

    [Fact]
    public async Task TransportDiagnosticsDoNotEscapeToTheProduct()
    {
        ClientSetupResult setup = await new LocalGameClient(new FailingTransport("secret socket diagnostic")).ReadSetupAsync(TestContext.Current.CancellationToken);
        Assert.Equal("transport_unavailable", setup.Error?.Code);
        Assert.DoesNotContain("secret socket diagnostic", setup.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallerCancellationIsNotPresentedAsAStartupFailure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await new LocalGameClient(new InProcessTransport(Host())).ReadSetupAsync(cancelled.Token));
    }

    [Fact]
    public async Task MutationCancellationBeforeDispatchSendsNoRequest()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await client.OpenAsync(Specification(), cancelled.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await client.ResolveAsync("capability", EngineDecision.Decline, cancelled.Token));
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task CancellationAfterMutationDispatchStillConsumesItsResponse()
    {
        EngineResponse opened = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        using var cancellation = new CancellationTokenSource();
        var transport = new CommitAwareCancellingTransport(opened with { RequestId = "local-resolve", Capability = null, Revision = opened.Revision + 1, }, cancellation);
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(opened.Capability!, EngineDecision.Decline, cancellation.Token);
        Assert.True(result.Succeeded);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(transport.ReceivedToken.CanBeCanceled);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData(999, "local-open", "local-core-game", "unsupported_version")]
    [InlineData(EngineProtocol.Version, "other", "local-core-game", "invalid_response")]
    [InlineData(EngineProtocol.Version, "local-open", "other-game", "invalid_response")]
    public async Task OpenRejectsMismatchedResponseEnvelopes(int version, string requestId, string gameId, string expectedCode)
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        ClientStartupResult result = await new LocalGameClient(new FixedTransport(complete with { Version = version, RequestId = requestId, GameId = gameId })).OpenAsync(Specification(), TestContext.Current.CancellationToken);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task MismatchedResolveEnvelopeRecoversBySync()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "wrong", Capability = null }, current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(current.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }
}
