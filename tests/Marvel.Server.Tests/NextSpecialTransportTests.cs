using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class NextSpecialTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EachTransportRestoresTheCurrentSpecialWithoutChoosingTheNextOne(bool socket)
    {
        var factory = new SpecialFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, new SequenceCapabilities("owner", "invite", "guest"),
            new RestrictedVisibilityPolicy(0), store: store);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        const string game = "next-special";
        EngineResponse opened = await Exchange(EngineRequest.OpenGame("open", game,
            new GameSpecification("rhino", ["black_panther", "spider_man"], [], Seed: 31)));
        string owner = opened.Capability!;
        EngineResponse guest = await Exchange(EngineRequest.AttachGame("join", game,
            Assert.Single(opened.Invitations!).Invitation));
        await Exchange(EngineRequest.ResolveGame("keep-owner", game, owner, TakeOnly(opened), opened.Revision));
        EngineResponse guestOpening = await Exchange(EngineRequest.SyncGame("guest-opening", game, guest.Capability!));
        await Exchange(EngineRequest.ResolveGame("keep-guest", game, guest.Capability!,
            TakeOnly(guestOpening), guestOpening.Revision));
        EngineResponse current = await Exchange(EngineRequest.SyncGame("setup", game, owner));
        Assert.Equal(PublicDecisionKind.CardSearch, current.Prompt!.PublicKind);
        current = await Exchange(EngineRequest.ResolveGame("search", game, owner,
            new EngineDecision(current.Prompt.Affordances[0].Id, []), current.Revision));
        Affordance change = Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == Game.ChangeForm);
        current = await Exchange(EngineRequest.ResolveGame("hero", game, owner,
            new EngineDecision(change.Id, []), current.Revision));
        Affordance play = Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == factory.Played && offer.PlaysCard);
        var draft = new DecisionComposer(current.Prompt);
        draft.SelectAffordance(play.Id);
        draft.ToggleResource(factory.Resource);
        Assert.True(draft.TryBuild(out EngineDecision? payment, out string? error), error);
        current = await Exchange(EngineRequest.ResolveGame("play", game, owner, payment!, current.Revision));
        Assert.Null(current.Error);
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, current.Prompt!.PublicKind);
        Assert.Equal(3, current.Prompt.Affordances.Count);
        Assert.All(current.Prompt.Affordances, offer => Assert.Null(offer.Targets));
        EngineResponse waiting = await Exchange(EngineRequest.SyncGame("waiting", game, guest.Capability!));
        Assert.Null(waiting.Prompt);
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, waiting.World!.Table!.PendingSituation!.Kind);
        Assert.All(Hand(waiting, 0), card => Assert.Null(card.Face));
        Assert.Contains("next Special ability", PendingSituationPresentation.Heading(waiting.World));
        Affordance claws = Assert.Single(current.Prompt.Affordances, offer => offer.AnchorId == factory.Claws);
        current = await Exchange(EngineRequest.ResolveGame("claws", game, owner,
            new EngineDecision(claws.Id, []), current.Revision));
        Assert.Null(current.Error);
        Assert.NotEqual(PublicDecisionKind.SpecialAbilityNext, current.Prompt!.PublicKind);
        EngineResponse pendingTarget = current;
        host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        current = await Exchange(EngineRequest.SyncGame("restore", game, owner));
        Assert.Equal(EngineJson.Write(pendingTarget with { RequestId = "same", Events = [] }),
            EngineJson.Write(current with { RequestId = "same", Events = [] }));
        Affordance target = Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == factory.Villain);
        EngineResponse stale = await Exchange(EngineRequest.ResolveGame("stale", game, owner,
            new EngineDecision(target.Id, []), current.Revision - 1));
        Assert.Equal("stale_decision", stale.Error?.Code);
        current = await Exchange(EngineRequest.ResolveGame("target", game, owner,
            new EngineDecision(target.Id, []), current.Revision));
        Assert.Null(current.Error);
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, current.Prompt!.PublicKind);
        Assert.Equal(2, current.Prompt.Affordances.Count);
        Assert.DoesNotContain(current.Prompt.Affordances, offer => offer.AnchorId == factory.Claws);
        Assert.Equal(2, factory.World.Cards[factory.Villain].Damage);
        host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        EngineResponse restored = await Exchange(EngineRequest.SyncGame("restore-next", game, owner));
        Assert.Equal(EngineJson.Write(current with { RequestId = "same", Events = [] }),
            EngineJson.Write(restored with { RequestId = "same", Events = [] }));

        // Replay v7 recorded a complete permutation; v8 must reject that trace
        // before treating one of its entries as a current next-card answer.
        StoredSession saved = Assert.Single(store.Load());
        SessionSave oldSave = saved.Save with
        {
            Compatibility = saved.Save.Compatibility with { ReplayContract = "engine-replay-v7" },
        };
        SessionCompatibilityException mismatch = Assert.Throws<SessionCompatibilityException>(() =>
            SessionReplay.Verify(oldSave, factory.Compatibility,
                _ => throw new InvalidOperationException("an incompatible trace must not execute")));
        Assert.Equal("replay_identity_mismatch", mismatch.Category);
        store.Commit(saved with { Save = oldSave });
        host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        EngineResponse incompatible = await Exchange(EngineRequest.SyncGame("old-replay", game, owner));
        Assert.Equal("session_not_found", incompatible.Error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviousProtocolIsRejectedBeforeOpeningAGame(bool socket)
    {
        var host = new EngineHost(new UnusedFactory());
        EngineRequest request = EngineRequest.OpenGame("old", "old-protocol",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 1)) with { Version = 23 };
        EngineResponse response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal("unsupported_version", response.Error?.Code);
        Assert.Null(response.World);
    }

    private sealed class SpecialFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public int Played { get; private set; }
        public int Resource { get; private set; }
        public int Claws { get; private set; }
        public int Villain { get; private set; }
        public World World { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World = opened.Game.State;
            var upgrades = World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
            foreach (string face in new[] { "01047", "01046", "01049" })
            {
                Card card = World.Cards.Single(card => card.FaceId == face && card.Owner == 0);
                Marvel.Rules.State.World.MoveToTop(card, upgrades);
                if (face == "01047") Claws = card.ObjectId;
            }
            Played = World.CreateCard("01043a", World.Seats[0].Hand).ObjectId;
            Resource = World.CreateCard("01044", World.Seats[0].Hand).ObjectId;
            Villain = World.TheCardIn(DeckType.VillainArea)!.ObjectId;
            return opened;
        }
    }
}
