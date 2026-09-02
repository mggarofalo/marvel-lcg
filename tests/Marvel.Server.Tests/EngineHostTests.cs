using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngineHostTests
{
    [Fact]
    public void ADecisionForAnEarlierRevisionIsRejectedWithoutAdvancingTheGame()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "revision-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        EngineDecision mulligan = TakeOnly(opened);

        EngineResponse advanced = host.Exchange(EngineRequest.ResolveGame(
            "mulligan",
            "revision-table",
            opened.Capability!,
            mulligan,
            opened.Revision));
        EngineResponse beforeStale = host.Exchange(EngineRequest.SyncGame(
            "before-stale", "revision-table", opened.Capability!));
        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame(
            "stale",
            "revision-table",
            opened.Capability!,
            mulligan,
            opened.Revision));
        EngineResponse afterStale = host.Exchange(EngineRequest.SyncGame(
            "after-stale", "revision-table", opened.Capability!));

        Assert.Null(advanced.Error);
        Assert.Equal(opened.Revision + 1, advanced.Revision);
        Assert.Equal("stale_decision", stale.Error?.Code);
        Assert.Empty(stale.Events);
        Assert.Equal(beforeStale.Revision, afterStale.Revision);
        Assert.Equal(
            EngineJson.Write(beforeStale with { RequestId = "same" }),
            EngineJson.Write(afterStale with { RequestId = "same" }));
    }

    [Fact]
    public void AuthoredSetupChoicesAreAvailableBeforeAGameExists()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));

        EngineResponse response = host.Exchange(EngineRequest.ReadSetup("choices"));

        Assert.Null(response.Error);
        Assert.Equal(string.Empty, response.GameId);
        Assert.Null(response.World);
        Assert.Empty(response.Events);
        SetupChoices choices = Assert.IsType<SetupChoices>(response.Setup);
        Assert.Equal(
            ["spider_man", "captain_marvel", "she_hulk", "iron_man", "black_panther"],
            choices.Heroes.Select(choice => choice.Key));
        Assert.Equal("Spider-Man", choices.Heroes[0].Name);
        Assert.Equal(
            ["rhino", "rhino_expert", "klaw", "klaw_expert", "ultron", "ultron_expert"],
            choices.Scenarios.Select(choice => choice.Key));
        Assert.False(choices.Scenarios[0].Expert);
        Assert.True(choices.Scenarios[1].Expert);
        Assert.Equal(["bomb_scare"], choices.Scenarios[0].RecommendedModularSets);
        Assert.Equal(
            [
                "bomb_scare", "masters_of_evil", "under_attack",
                "legions_of_hydra", "the_doomsday_chair",
            ],
            choices.ModularSets.Select(choice => choice.Key));
        Assert.Equal("The Doomsday Chair", choices.ModularSets[^1].Name);
    }

    [Fact]
    public void InProcessSetupResponsesCannotMutateTheAuthoredCatalog()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        SetupChoices first = Assert.IsType<SetupChoices>(
            host.Exchange(EngineRequest.ReadSetup("first")).Setup);
        IList<string> exposed = Assert.IsAssignableFrom<IList<string>>(
            first.Scenarios.Single(choice => choice.Key == "rhino")
                .RecommendedModularSets);

        Assert.Throws<NotSupportedException>(exposed.Clear);

        SetupChoices second = Assert.IsType<SetupChoices>(
            host.Exchange(EngineRequest.ReadSetup("second")).Setup);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "catalog-isolation",
            new GameSpecification(
                "rhino", ["spider_man"], ModularSets: null, Seed: 7)));
        Assert.Equal(
            ["bomb_scare"],
            second.Scenarios.Single(choice => choice.Key == "rhino")
                .RecommendedModularSets);
        Assert.Null(opened.Error);
    }

    [Fact]
    public void SetupDiscoveryRejectsFieldsThatCouldNameOrMutateAGame()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineRequest valid = EngineRequest.ReadSetup("bad");
        EngineRequest[] malformed =
        {
            valid with { GameId = "game" },
            valid with { Capability = "capability" },
            valid with
            {
                Game = new GameSpecification("rhino", ["spider_man"], [], 7),
            },
            valid with { Decision = EngineDecision.Decline },
            valid with { Viewer = new ViewerClaim(Watch: true) },
            valid with { ExpectedRevision = 0 },
        };

        foreach (EngineRequest request in malformed)
        {
            EngineResponse response = host.Exchange(request);
            Assert.Equal("invalid_request", response.Error?.Code);
            Assert.Null(response.Setup);
        }
    }

    [Fact]
    public void HostsWithoutSetupDiscoveryReportItAsUnavailable()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        EngineResponse response = host.Exchange(EngineRequest.ReadSetup("choices"));

        Assert.Equal("setup_unavailable", response.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Theory]
    [InlineData("unus", "spider_man", null, "no campaign named 'unus'")]
    [InlineData("rhino", "sp_dr", null, "no hero named 'sp_dr'")]
    [InlineData("rhino", "spider_man", "sinister_syndicate",
        "no encounter set named 'sinister_syndicate'")]
    public void ExpansionProductsAreRejectedAtTheDatasetBoundary(
        string scenario, string hero, string? modular, string message)
    {
        // Product names resolve before WorldSetup creates a board or seeds its
        // RNG. Keeping the complete printed card catalog therefore cannot make
        // an unsupported expansion game partly start.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(
            scenario, [hero], modular is null ? null : [modular], Seed: 7);

        var refused = Assert.Throws<KeyNotFoundException>(() => factory.Create(specification));

        Assert.Contains(message, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalContentOpensAndResolvesThroughTheHost()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "request-1",
            "game-1",
            new GameSpecification("rhino", ["spider_man"], ModularSets: null, Seed: 7)));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Prompt);
        Assert.Equal("request-1", opened.RequestId);
        Assert.Equal("game-1", opened.GameId);

        var resolved = host.Exchange(EngineRequest.ResolveGame(
            "request-2", "game-1", RequiredCapability(opened), TakeOnly(opened)));

        Assert.Null(resolved.Error);
        Assert.NotNull(resolved.Prompt);
        Assert.Equal("request-2", resolved.RequestId);
    }

    [Fact]
    public void SeparateCapabilitiesMayUseTheSameClientChosenGameId()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("first-capability", "second-capability"));
        var request = EngineRequest.OpenGame(
            "first",
            "chosen-id",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7));

        var first = host.Exchange(request);
        var duplicate = host.Exchange(request with { RequestId = "again" });

        Assert.Null(first.Error);
        Assert.Null(duplicate.Error);
        Assert.NotEqual(first.Capability, duplicate.Capability);
    }

    [Fact]
    public void InvalidCommandsFailBeforeTheyReachAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var wrongVersion = host.Exchange(new EngineRequest(
            99, "version", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));
        var unknownGame = host.Exchange(EngineRequest.ResolveGame(
            "resolve", "missing", "missing-capability", EngineDecision.Decline));

        Assert.Equal("unsupported_version", wrongVersion.Error?.Code);
        Assert.Equal("session_not_found", unknownGame.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void InvalidAdditionalSeatGrantsFailBeforeTheyReachAFactory()
    {
        Func<int, ViewScope> seat = index =>
            new RestrictedVisibilityPolicy(index).Authorize(null, players: 2);
        ViewScope everySeat = new PermissiveVisibilityPolicy().Authorize(
            new ViewerClaim(HotSeat: true), players: 2);
        (string Name, IReadOnlyList<SeatScope>? Grants)[] hostile =
        {
            ("missing collection", null),
            ("empty grant", new SeatScope[] { null! }),
            ("negative seat", [new SeatScope(-1, seat(0))]),
            ("seat past player count", [new SeatScope(2, seat(0))]),
            ("duplicate seat", [new SeatScope(1, seat(1)), new SeatScope(1, seat(1))]),
            ("primary seat repeated", [new SeatScope(0, seat(0))]),
            ("different seat in scope", [new SeatScope(1, seat(0))]),
            ("more than one seat in scope", [new SeatScope(1, everySeat)]),
            ("empty scope", [new SeatScope(1, ViewScope.None)]),
        };

        foreach ((string name, IReadOnlyList<SeatScope>? grants) in hostile)
        {
            var factory = new UnusedFactory();
            var host = new EngineHost(
                factory,
                visibility: new HostileVisibilityPolicy(grants));

            EngineResponse response = host.Exchange(EngineRequest.OpenGame(
                name,
                "game",
                new GameSpecification(
                    "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

            Assert.Equal("invalid_request", response.Error?.Code);
            Assert.Equal(0, factory.Calls);
        }
    }

    [Fact]
    public void MissingPrimaryScopeFailsBeforeItReachesAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory, visibility: new NullPrimaryVisibilityPolicy());

        EngineResponse response = host.Exchange(EngineRequest.OpenGame(
            "missing-primary",
            "game",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

        Assert.Equal("invalid_request", response.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void AdditionalSeatGrantsAreSnapshottedBeforeValidationAndIssuance()
    {
        ViewScope zero = new RestrictedVisibilityPolicy(0).Authorize(null, players: 2);
        ViewScope one = new RestrictedVisibilityPolicy(1).Authorize(null, players: 2);
        var grants = new ChangingSeatGrants(
            [new SeatScope(1, one)],
            [new SeatScope(0, zero)]);
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("owner", "invitation"),
            new HostileVisibilityPolicy(grants));

        EngineResponse response = host.Exchange(EngineRequest.OpenGame(
            "snapshot",
            "game",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

        Assert.Null(response.Error);
        Assert.Equal(1, Assert.Single(response.Invitations!).Seat);
        Assert.Equal(1, grants.Enumerations);
    }

    [Fact]
    public void EngineFailuresDoNotReturnInternalDiagnostics()
    {
        var host = new EngineHost(new FailingFactory("secret-card-identity"));

        EngineResponse failed = host.Exchange(EngineRequest.OpenGame(
            "open",
            "game",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));

        Assert.Equal("engine_error", failed.Error?.Code);
        Assert.DoesNotContain(
            "secret-card-identity", failed.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionOneIsRejectedBeforeItCanOpenAGame()
    {
        // The current protocol adds play-area topology kinds to the event
        // union and per-target allocation capacities. A version 1 client
        // cannot deserialize those responses, so it is rejected before the
        // factory can create mutable game state.
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var rejected = host.Exchange(new EngineRequest(
            1, "old-client", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));

        Assert.Equal(7, EngineProtocol.Version);
        Assert.Equal(EngineProtocol.Version, rejected.Version);
        Assert.Equal("unsupported_version", rejected.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void ClosingAGameReleasesItsClientChosenId()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);
        var opened = host.Exchange(
            EngineRequest.OpenGame("open", "reusable", specification));
        Assert.Null(opened.Error);

        Assert.Null(host.Exchange(
            EngineRequest.CloseGame(
                "close", "reusable", RequiredCapability(opened))).Error);
        Assert.Null(host.Exchange(
            EngineRequest.OpenGame("reopen", "reusable", specification)).Error);
    }

    [Fact]
    public void AnInvalidDecisionIsRejectedBeforeMutationAndTheSessionRemainsAvailable()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "fail-closed",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        Assert.Null(opened.Error);
        string capability = RequiredCapability(opened);

        var rejected = host.Exchange(EngineRequest.ResolveGame(
            "bad", "fail-closed", capability, new EngineDecision(999, [])));
        var after = host.Exchange(EngineRequest.ResolveGame(
            "after", "fail-closed", capability, TakeOnly(opened)));

        Assert.Equal("invalid_decision", rejected.Error?.Code);
        Assert.Null(after.Error);
    }

    [Fact]
    public void RestrictedHostsDoNotTrustWatchOrHotSeatClaims()
    {
        var specification = new GameSpecification(
            "rhino", ["spider_man", "captain_marvel"], [], Seed: 7);
        var seatZeroHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(0));
        var seatOneHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(1));

        EngineResponse zero = seatZeroHost.Exchange(EngineRequest.OpenGame(
            "zero", "game", specification, new ViewerClaim(Watch: true)));
        EngineResponse one = seatOneHost.Exchange(EngineRequest.OpenGame(
            "one", "game", specification, new ViewerClaim(HotSeat: true)));

        Assert.Null(zero.Error);
        Assert.Null(one.Error);
        Assert.All(Hand(zero, 0), card => Assert.NotNull(card.Face));
        Assert.All(Hand(zero, 1), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 0), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 1), card => Assert.NotNull(card.Face));
    }

    [Fact]
    public void RestrictedSeatsReceiveOnlyTheirTurnOptionsAndMaySubmitTheirOwnOffTurnAction()
    {
        var factory = new OffTurnActionFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0));
        var specification = new GameSpecification(
            "rhino", ["captain_marvel", "spider_man"], [], Seed: 7);

        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "actions", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame(
            "attach", "actions", Assert.Single(opened.Invitations!).Invitation));
        EngineResponse afterZero = host.Exchange(EngineRequest.ResolveGame(
            "zero-mulligan", "actions", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame(
            "one-mulligan-menu", "actions", RequiredCapability(attached)));
        EngineResponse afterOne = host.Exchange(EngineRequest.ResolveGame(
            "one-mulligan", "actions", RequiredCapability(attached),
            TakeOnly(oneMulligan), oneMulligan.Revision));

        Assert.Null(afterZero.Prompt);
        Assert.Equal(1, afterOne.Prompt?.Player);
        EngineResponse active = host.Exchange(EngineRequest.SyncGame(
            "active", "actions", RequiredCapability(opened)));
        EngineResponse other = host.Exchange(EngineRequest.SyncGame(
            "other", "actions", RequiredCapability(attached)));

        Prompt activeMenu = Assert.IsType<Prompt>(active.Prompt);
        Prompt otherMenu = Assert.IsType<Prompt>(other.Prompt);
        Assert.Equal(0, activeMenu.Player);
        Assert.DoesNotContain(
            activeMenu.Affordances, option => option.AnchorPlayer == 1);
        Assert.Equal(1, otherMenu.Player);
        Assert.False(otherMenu.Cancellable);
        Assert.All(otherMenu.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(1, option.AnchorPlayer);
        });
        Affordance auntMay = Assert.Single(
            otherMenu.Affordances,
            option => option.AnchorId == factory.AuntMay.ObjectId);

        Affordance activeOnly = Assert.Single(
            activeMenu.Affordances, option => option.Verb == Game.ChangeForm);
        EngineResponse forgedTurnOption = host.Exchange(EngineRequest.ResolveGame(
            "forged-basic", "actions", RequiredCapability(attached),
            new EngineDecision(activeOnly.Id, []), active.Revision));
        EngineResponse stolenAction = host.Exchange(EngineRequest.ResolveGame(
            "stolen-action", "actions", RequiredCapability(opened),
            new EngineDecision(auntMay.Id, []), active.Revision));
        Assert.Equal("not_your_turn", forgedTurnOption.Error?.Code);
        Assert.Equal("not_your_turn", stolenAction.Error?.Code);

        EngineResponse acted = host.Exchange(EngineRequest.ResolveGame(
            "act", "actions", RequiredCapability(attached),
            new EngineDecision(auntMay.Id, []), other.Revision));
        Assert.Null(acted.Error);
        Assert.False(factory.AuntMay.Ready);
        Assert.Equal(1, factory.Game.State.Seats[1].IdentityCard.Damage);

        EngineResponse staleActive = host.Exchange(EngineRequest.ResolveGame(
            "stale-active", "actions", RequiredCapability(opened),
            new EngineDecision(activeOnly.Id, []), active.Revision));
        Assert.Equal("stale_decision", staleActive.Error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACompetingOffTurnActionIsStaleEvenWhenTheWinningCommandRemovesIt(
        bool winningCommandEndsGame)
    {
        var factory = new VanishingOffTurnActionFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            winningCommandEndsGame);
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0));
        var specification = new GameSpecification(
            "rhino", ["captain_marvel", "spider_man"], [], Seed: 7);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "race", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame(
            "attach", "race", Assert.Single(opened.Invitations!).Invitation));
        _ = host.Exchange(EngineRequest.ResolveGame(
            "zero-mulligan", "race", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame(
            "one-mulligan-menu", "race", RequiredCapability(attached)));
        _ = host.Exchange(EngineRequest.ResolveGame(
            "one-mulligan", "race", RequiredCapability(attached),
            TakeOnly(oneMulligan), oneMulligan.Revision));

        EngineResponse active = host.Exchange(EngineRequest.SyncGame(
            "active", "race", RequiredCapability(opened)));
        EngineResponse offTurn = host.Exchange(EngineRequest.SyncGame(
            "off-turn", "race", RequiredCapability(attached)));
        Affordance healing = Assert.Single(
            active.Prompt!.Affordances,
            option => option.AnchorId == factory.ActiveSource.ObjectId);
        Affordance disappearing = Assert.Single(
            offTurn.Prompt!.Affordances,
            option => option.AnchorId == factory.OffTurnSource.ObjectId);
        Assert.Equal(active.Revision, offTurn.Revision);

        EngineResponse won = host.Exchange(EngineRequest.ResolveGame(
            "winner", "race", RequiredCapability(opened),
            new EngineDecision(healing.Id, []), active.Revision));
        EngineResponse lost = host.Exchange(EngineRequest.ResolveGame(
            "loser", "race", RequiredCapability(attached),
            new EngineDecision(disappearing.Id, []), offTurn.Revision));

        Assert.Null(won.Error);
        Assert.Equal(0, factory.Game.State.Seats[1].IdentityCard.Damage);
        Assert.Equal(winningCommandEndsGame, won.Prompt is null);
        Assert.Equal("stale_decision", lost.Error?.Code);
        EngineResponse current = host.Exchange(EngineRequest.SyncGame(
            "current", "race", RequiredCapability(attached)));
        Assert.Equal(won.Revision, current.Revision);
    }

    [Fact]
    public void FileReadingAndCheatOperationsHaveNoServedSurface()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        EngineResponse read = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "read", "read_file", "game"));
        EngineResponse cheat = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "cheat", "cheat", "game"));

        Assert.Equal("invalid_request", read.Error?.Code);
        Assert.Equal("invalid_request", cheat.Error?.Code);
        Assert.Equal(0, factory.Calls);
        Assert.DoesNotContain(
            typeof(EngineRequest).GetProperties(),
            property => property.Name.Contains("File", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Cheat", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<Marvel.View.CardDescriptor> Hand(
        EngineResponse response, int seat) =>
        Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == "HandsArea" && area.Owner == seat).Cards;

    private static string RequiredCapability(EngineResponse response) =>
        Assert.IsType<string>(response.Capability);

    private static EngineDecision TakeOnly(EngineResponse response) =>
        new(Assert.Single(Assert.IsType<Marvel.Rules.Prompts.Prompt>(response.Prompt)
            .Affordances).Id, []);

    private sealed class UnusedFactory : IGameFactory
    {
        public int Calls { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    private sealed class SequenceCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }

    private sealed class FailingFactory(string message) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) =>
            throw new InvalidOperationException(message);
    }

    private sealed class OffTurnActionFactory(IGameFactory inner) : IGameFactory
    {
        public Card AuntMay { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            Game = opened.Game;
            var world = Game.State;
            AuntMay = world.CreateCard(
                "01006",
                world.AreaOf(
                    DeckType.SupportsArea,
                    PlayArea.Of(1),
                    cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(5);
            return opened;
        }
    }

    private sealed class VanishingOffTurnActionFactory(
        IGameFactory inner,
        bool winningCommandEndsGame) : IGameFactory
    {
        public Card ActiveSource { get; private set; } = null!;

        public Card OffTurnSource { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            ActiveSource = world.CreateCard(
                "01006",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            OffTurnSource = world.CreateCard(
                "01006",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(1);
            Game = Game.Begin(
                world,
                world.Facts,
                new CrossSeatHealingActions(
                    ActiveSource,
                    OffTurnSource,
                    winningCommandEndsGame));
            return opened with { Game = Game };
        }
    }

    private sealed class CrossSeatHealingActions(
        Card active,
        Card offTurn,
        bool winningCommandEndsGame)
        : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Actions(World world, int player) =>
            player switch
            {
                0 => [new PendingAbility(active.ObjectId, AbilityType.Action, player)],
                1 when world.Seats[1].IdentityCard.Damage > 0 =>
                    [new PendingAbility(offTurn.ObjectId, AbilityType.Action, player)],
                _ => [],
            };

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(
                ability.Card,
                Game.ActionVerb,
                ability.Card,
                ability.Player,
                ability.Player == 0 ? "Heal player 2" : "Use player 2 action");

        public override IReadOnlyList<GameEvent> Act(
            World world,
            PendingAbility ability,
            IReadOnlyList<int> paying,
            IReadOnlyList<int> chosen,
            IReadOnlyDictionary<string, long>? values = null,
            IReadOnlyList<ResourceAllocation>? allocations = null)
        {
            var events = new List<GameEvent>();
            if (ability.Player == 0)
            {
                Damage.Heal(
                    world,
                    world.Facts,
                    world.Seats[1].IdentityCard,
                    amount: 1,
                    trigger: "test",
                    verb: Game.ActionVerb,
                    events);
                if (winningCommandEndsGame)
                {
                    world.Finish(Outcome.PlayersWin);
                }
            }

            return events;
        }
    }

    private sealed class HostileVisibilityPolicy(IReadOnlyList<SeatScope>? grants)
        : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) =>
            new RestrictedVisibilityPolicy(0).Authorize(null, players);

        public IReadOnlyList<SeatScope> AdditionalScopes(
            ViewerClaim? claim, int players) => grants!;
    }

    private sealed class NullPrimaryVisibilityPolicy : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) => null!;

        public IReadOnlyList<SeatScope> AdditionalScopes(
            ViewerClaim? claim, int players) => [];
    }

    private sealed class ChangingSeatGrants(
        IReadOnlyList<SeatScope> first,
        IReadOnlyList<SeatScope> later) : IReadOnlyList<SeatScope>
    {
        public int Enumerations { get; private set; }

        public int Count => first.Count;

        public SeatScope this[int index] => first[index];

        public IEnumerator<SeatScope> GetEnumerator()
        {
            Enumerations++;
            return (Enumerations == 1 ? first : later).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
