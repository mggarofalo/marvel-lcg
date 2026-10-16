using Marvel.Transport;
using Marvel.Protocol;
using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RecipientSocketCommitmentTests : TransportTestBase
{
    [Theory]
    [InlineData("01091", false)]
    [InlineData("01091", true)]
    [InlineData("01092", false)]
    [InlineData("01092", true)]
    public async Task LegalRecipientCommitmentSurvivesBothTransportBoundaries(string face, bool socket)
    {
        var (host, current, source) = InstalledSupport(face);
        string capability = current.Capability!;
        var server = new SocketEngineServer(host, IPAddress.Loopback, 0);
        var local = new InProcessTransport(host);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(server, request)
            : await local.ExchangeAsync(request, TestContext.Current.CancellationToken);
        EngineResponse offered = await Exchange(EngineRequest.SyncGame("wire-offer", "recipient-wire", capability));
        Assert.Null(offered.Error);
        var activation = Assert.Single(offered.Prompt!.Affordances, option => option.AnchorId == source && option.DirectRecipientCommit);
        Assert.Equal(2, activation.Targets!.Legal.Count);
        int recipient = activation.Targets.Legal[1];
        Assert.Contains("Exhaust", activation.Targets.Details![recipient]);
        int before = Hand(offered, 1).Count;
        var composer = new DecisionComposer(offered.Prompt);
        composer.SelectAffordance(activation.Id);
        composer.SelectTargets([recipient]);
        Assert.True(composer.TryBuild(out var answer, out var error), error);
        EngineResponse committed = await Exchange(EngineRequest.ResolveGame("wire-commit", "recipient-wire", capability, answer!, offered.Revision));
        Assert.Null(committed.Error);
        Assert.Equal(offered.Revision + 1, committed.Revision);
        Assert.Equal(PublicDecisionKind.PlayerAction, committed.Prompt!.PublicKind);
        Assert.False(committed.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == source).Ready);
        Assert.Equal(before + (face == "01091" ? 1 : 0), Hand(committed, 1).Count);
        var stale = await Exchange(EngineRequest.ResolveGame("wire-stale", "recipient-wire", capability, answer!, offered.Revision));
        Assert.NotNull(stale.Error);
        Assert.Equal(committed.Revision, (await Exchange(EngineRequest.SyncGame("wire-final", "recipient-wire", capability))).Revision);
    }

    private static (EngineHost Host, EngineResponse Current, int Source) InstalledSupport(string face)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        for (uint seed = 1; seed <= 64; seed++)
        {
            var host = new EngineHost(factory);
            var opened = host.Exchange(EngineRequest.OpenGame("open", "recipient-wire",
                new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: seed)));
            Assert.Null(opened.Error);
            if (!Hand(opened, 0).Any(card => card.Face?.Id == face)) continue;
            var current = opened;
            for (int seat = 0; seat < 2; seat++)
                current = host.Exchange(EngineRequest.ResolveGame($"keep-{seat}", "recipient-wire", opened.Capability!, TakeOnly(current), current.Revision));
            int source = Hand(current, 0).Single(card => card.Face?.Id == face).Id!.Value;
            var offer = Assert.Single(current.Prompt!.Affordances, option => option.AnchorId == source && option.PlaysCard);
            var composer = new DecisionComposer(current.Prompt);
            composer.SelectAffordance(offer.Id);
            foreach (var generator in Assert.Single(offer.CostOptions).Generators) composer.ToggleResource(generator.Effect);
            Assert.True(composer.TryBuild(out var payment, out var error), error);
            current = host.Exchange(EngineRequest.ResolveGame("play", "recipient-wire", opened.Capability!, payment!, current.Revision));
            Assert.Null(current.Error);
            return (host, current with { Capability = opened.Capability }, source);
        }
        throw new Xunit.Sdk.XunitException("No legal support opening found in deterministic seed range.");
    }
}
