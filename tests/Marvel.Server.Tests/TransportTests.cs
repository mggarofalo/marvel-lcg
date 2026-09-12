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
public abstract class TransportTestBase
{
    protected static string? ErrorCode(EngineResponse response) => response.Error?.Code;
    protected static int? PromptPlayer(EngineResponse response) => response.Prompt?.Player;
    protected static ViewerClaim? RestrictedViewer(string mode) => mode switch
    {
        "omitted" => null,
        "other-seat" => new ViewerClaim(Seat: 1),
        "watch" => new ViewerClaim(Watch: true),
        "hot-seat" => new ViewerClaim(HotSeat: true),
        _ => throw new InvalidOperationException($"unknown test mode {mode}"),
    };
    protected static async Task<EngineResponse> ExchangeOverSocket(SocketEngineServer server, EngineRequest request)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineResponse response = await transport.ExchangeAsync(request, TestContext.Current.CancellationToken);
        await serving;
        return response;
    }

    protected static IReadOnlyList<Marvel.View.CardDescriptor> Hand(EngineResponse response, int seat) => Assert.Single(Assert.IsType<WorldDescriptor>(response.World).Areas, area => area.Zone == "HandsArea" && area.Owner == seat).Cards;
    protected static EngineDecision TakeOnly(EngineResponse response) => new(Assert.Single(Assert.IsType<Marvel.Rules.Prompts.Prompt>(response.Prompt).Affordances).Id, []);
    protected sealed class EchoEndpoint(EngineRequest expectedRequest, EngineResponse response) : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            Assert.Equal(EngineJson.Write(expectedRequest), EngineJson.Write(request));
            return response;
        }
    }

    protected sealed class CountingEndpoint : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    protected sealed class SequenceEndpoint(params EngineResponse[] responses) : IEngineEndpoint
    {
        private readonly Queue<EngineResponse> responses = new(responses);
        public EngineResponse Exchange(EngineRequest request) => responses.Dequeue();
    }

    protected sealed class SequenceCapabilities(params string[] capabilities) : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);
        public string Issue() => capabilities.Dequeue();
    }

    protected sealed class UnusedFactory : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) => throw new InvalidOperationException("should not be called");
    }

    protected sealed class EliminatingFactory(IGameFactory inner) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            var events = opened.SetupEvents.ToList();
            Elimination.Eliminate(opened.Game.State, opened.Game.State.Facts, player: 0, trigger: "test", events);
            return opened with
            {
                SetupEvents = events
            };
        }
    }

    protected sealed class HighwayRobberyFactory(IGameFactory inner) : IGameFactory
    {
        public int ReturnedForSeatOne { get; private set; } = -1;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            var world = opened.Game.State;
            var events = opened.SetupEvents.ToList();
            Card scheme = world.CreateCard("01166", world.AreaOf(DeckType.SideSchemesArea));
            events.AddRange(world.Abilities.WhenRevealed(world, scheme, player: 0));
            ReturnedForSeatOne = Assert.Single(world.Areas.Where(area => area.Host == scheme.ObjectId).SelectMany(area => area.Cards), card => card.Owner == 1).ObjectId;
            world.Agenda.Add(new PhaseStep(Steps.DealAttackDamage, 1, 4, Plan: true));
            world.Agenda.Begin(world, world.Facts);
            Defeat.Scheme(world, world.Facts, scheme, "test", events);
            return opened with
            {
                SetupEvents = events
            };
        }
    }

    protected static string ResponseTypeNames() => string.Join(",", typeof(EngineResponse).GetProperties().Select(property => property.PropertyType.FullName));
}
