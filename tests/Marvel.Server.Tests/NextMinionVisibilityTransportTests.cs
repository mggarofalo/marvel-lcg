using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class NextMinionVisibilityTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FacedownDroneChoicesNeverNameTheirUnderlyingPlayerCards(bool socket, bool reverse)
    {
        // rr:minion.3: "one minion at a time and in an order of the engaged
        // player's choosing". Ultron's Drone profile supplies its public identity.
        // The fixture rearranges existing owned Core cards; no cards are invented.
        var factory = new DroneFactory(DatasetGameFactory.Load(RepositoryPaths.Root), reverse);
        var host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0));
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        EngineResponse current = await Exchange(EngineRequest.OpenGame("open", "drones",
            new GameSpecification("ultron", ["spider_man"], [], Seed: 31)));
        string capability = current.Capability!;
        for (int step = 0; current.Prompt?.PublicKind != PublicDecisionKind.MinionActivationOrder && step < 20; step++)
        {
            Prompt prompt = Assert.IsType<Prompt>(current.Prompt);
            // Explicit fixture policy keeps the opening hand and ends the turn,
            // declining optional opportunities on the way to the minion decision.
            EngineDecision answer = prompt.Cancellable ? EngineDecision.Decline : TakeOnly(current);
            current = await Exchange(EngineRequest.ResolveGame($"step-{step}", "drones", capability,
                answer, current.Revision));
            Assert.Null(current.Error);
        }
        AssertSafe(Assert.IsType<Prompt>(factory.Game.Pending), factory.Game.State);
        AssertSafe(Assert.IsType<Prompt>(current.Prompt), factory.Game.State);
        ViewScope authorized = new RestrictedVisibilityPolicy(0).Authorize(null, 1);
        VisibleResult projected = WorldProjection.For(factory.Game.State, factory.Game.Pending, [], authorized);
        AssertSafe(Assert.IsType<Prompt>(projected.Prompt), factory.Game.State);
        CardDescriptor[] visibleDrones = [.. current.World!.Areas.SelectMany(area => area.Cards)
            .Where(card => factory.Drones.Contains(card.Id ?? -1))];
        Assert.Equal(2, visibleDrones.Length);
        foreach (CardDescriptor drone in visibleDrones)
        {
            Assert.Equal("Drone", drone.Face!.Title);
            Assert.Equal("effective-drone", drone.Face.Id);
            Assert.Null(drone.Face.ArtFaceId);
        }
    }

    private static void AssertSafe(Prompt prompt, World world)
    {
        Assert.Equal(PublicDecisionKind.MinionActivationOrder, prompt.PublicKind);
        Assert.Equal(2, prompt.Affordances.Count);
        foreach (Affordance offer in prompt.Affordances)
        {
            Assert.Equal("effective-drone", offer.Label);
            Assert.Equal("Activate Drone next", offer.DisplayLabel);
            Assert.Equal("Activate Drone next", offer.CommitLabel);
            string visible = string.Join(" ", prompt.Label, prompt.DisplayQuestion, prompt.Description,
                offer.Label, offer.DisplayLabel, offer.CommitLabel, offer.Description);
            foreach (string face in new[] { "01002", "01003" })
            {
                Assert.DoesNotContain(face, visible, StringComparison.Ordinal);
                Assert.DoesNotContain(world.Facts.Title(face), visible, StringComparison.Ordinal);
            }
        }
    }

    private sealed class DroneFactory(IGameFactory inner, bool reverse) : IGameFactory
    {
        public Game Game { get; private set; } = null!;
        public List<int> Drones { get; } = [];

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            Game = opened.Game;
            World world = Game.State;
            Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused);
            Card[] existing = [.. world.Cards.Where(card => card.InstanceState.Profile?.Id == "effective-drone")];
            EffectiveCardProfile profile = Assert.Single(existing).InstanceState.Profile!;
            foreach (Card card in existing)
            {
                World.MoveToTop(card, world.Seats[0].Deck);
                card.TurnFaceUp();
            }
            var events = new List<GameEvent>();
            foreach (string face in reverse ? new[] { "01003", "01002" } : new[] { "01002", "01003" })
            {
                Card physical = world.Cards.First(card => card.Owner == 0 && card.FaceId == face);
                World.MoveToTop(physical, world.Seats[0].Deck);
                Card drone = FacedownMinions.EngageTop(world, 0, profile, "fixture", "Create_Drone", events)!;
                Drones.Add(drone.ObjectId);
            }
            return opened;
        }
    }
}
