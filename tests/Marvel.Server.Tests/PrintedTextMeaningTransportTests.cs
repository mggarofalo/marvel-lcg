using System.Net;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Protocol;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Transport;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PrintedTextMeaningTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualRobotMeaningSurvivesBothTransports(bool socket)
    {
        // Research-only actual card projection; this does not admit the unfinished pack.
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 1, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        Card robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var visible = WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        EngineRequest request = EngineRequest.SyncGame("meaning", "robot", "capability");
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId,
            null, null, [], World: visible);
        var endpoint = new EchoEndpoint(request, expected);

        EngineResponse received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(42, received.Version);
        CardPersistentDescriptor facts = received.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Id == robot.ObjectId).Persistent!;
        Assert.Equal(robot.ObjectId, facts.Source.CardId);
        CardPersistentAbilityDescriptor action = Assert.Single(facts.Abilities, ability => ability.Trigger.Timing == "Action");
        Assert.Equal("this", action.Trigger.Subject);
        Assert.Equal("B", Assert.Single(action.Costs).Resources);
        CardPersistentEffectDescriptor effect = Assert.Single(action.Effects);
        Assert.Equal(("BlankPrintedText", "Source", "EndOfPhase"), (effect.Operation, effect.Target, effect.Until));
        Assert.Null(effect.Amount);
        Assert.Null(effect.Field);
        Assert.Equal(EngineJson.Write(expected), EngineJson.Write(received));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviousProtocolIsRejectedBeforeGameCreation(bool socket)
    {
        var host = new EngineHost(new UnusedFactory());
        EngineRequest request = EngineRequest.OpenGame("old", "robot",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)) with { Version = 39 };
        EngineResponse received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal("unsupported_version", received.Error?.Code);
        Assert.Equal(42, received.Version);
        Assert.Null(received.World);
    }
}
