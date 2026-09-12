using Marvel.Decisions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public abstract class EngineHostTestBase
{
    protected static EngineResponse ResolveAction(EngineHost host, EngineResponse opened, EngineResponse current, int source, string requestId)
    {
        Affordance action = Assert.Single(Assert.IsType<Prompt>(current.Prompt).Affordances, option => option.AnchorId == source);
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(requestId, "rewrite-example", RequiredCapability(opened), new EngineDecision(action.Id, []), current.Revision));
        Assert.Null(resolved.Error);
        return resolved;
    }

    protected static IReadOnlyList<Marvel.View.CardDescriptor> Hand(EngineResponse response, int seat) => Assert.Single(Assert.IsType<WorldDescriptor>(response.World).Areas, area => area.Zone == "HandsArea" && area.Owner == seat).Cards;
    protected static string RequiredCapability(EngineResponse response) => Assert.IsType<string>(response.Capability);
    protected static EngineDecision TakeOnly(EngineResponse response) => new(Assert.Single(Assert.IsType<Marvel.Rules.Prompts.Prompt>(response.Prompt).Affordances).Id, []);
    protected static void AssertQuarantined(IDurableGameFactory factory, StoredSession stored, string gameId, string capability)
    {
        var restarted = new EngineHost(factory, store: new FixedSessionStore(stored));
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("quarantined", gameId, capability)).Error?.Code);
    }

    protected static EngineDecision PayFirstPlayableCard(Prompt prompt)
    {
        foreach (Affordance option in prompt.Affordances.Where(option => string.Equals(option.Verb, "Play", StringComparison.Ordinal)))
        {
            IReadOnlyList<int> targets = option.Targets is null ? [] : option.Targets.Legal.Take(option.Targets.Min).ToList();
            foreach (CostOption cost in option.CostOptions.Where(cost => cost.Target == 0 || cost.Target == option.AnchorId || targets.Contains(cost.Target)))
            {
                var values = cost.VariableRequests.ToDictionary(request => request.Name, request => request.Min, StringComparer.Ordinal);
                int[] generators = cost.Generators.Select(generator => generator.Effect).Distinct().ToArray();
                IReadOnlyList<ResourceAllocation>? allocations = ResourcePayment.Allocate(cost, generators, values);
                if (allocations is not null && ResourcePayment.Allows(cost, generators, values, allocations))
                {
                    return new EngineDecision(option.Id, targets, generators, values, allocations);
                }
            }
        }

        throw new Xunit.Sdk.XunitException("the deterministic hand has no payable card play");
    }

    protected static EngineDecision PayCard(Prompt prompt, int anchor)
    {
        Affordance option = Assert.Single(prompt.Affordances, option => string.Equals(option.Verb, "Play", StringComparison.Ordinal) && option.AnchorId == anchor);
        IReadOnlyList<int> targets = option.Targets is null ? [] : option.Targets.Legal.Take(option.Targets.Min).ToList();
        foreach (CostOption cost in option.CostOptions.Where(cost => cost.Target == 0 || cost.Target == option.AnchorId || targets.Contains(cost.Target)))
        {
            var values = cost.VariableRequests.ToDictionary(request => request.Name, request => request.Min, StringComparer.Ordinal);
            int[] generators = cost.Generators.Select(generator => generator.Effect).Distinct().ToArray();
            IReadOnlyList<ResourceAllocation>? allocations = ResourcePayment.Allocate(cost, generators, values);
            if (allocations is not null && ResourcePayment.Allows(cost, generators, values, allocations))
            {
                return new EngineDecision(option.Id, targets, generators, values, allocations);
            }
        }

        throw new Xunit.Sdk.XunitException("the requested card has no legal payment");
    }

    protected sealed class UnusedFactory : IGameFactory
    {
        public int Calls { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    protected sealed class FailingSessionStore(ISessionStore inner, int failAtCommit) : ISessionStore
    {
        private int commits;
        public IReadOnlyList<StoredSession> Load() => inner.Load();
        public string? Commit(StoredSession session)
        {
            commits++;
            if (commits == failAtCommit)
            {
                throw new IOException("simulated interrupted write");
            }

            return inner.Commit(session);
        }
    }

    protected sealed class FixedSessionStore(params StoredSession[] sessions) : ISessionStore
    {
        public IReadOnlyList<StoredSession> Load() => sessions;
        public string? Commit(StoredSession session) => throw new InvalidOperationException("not used");
    }

    protected sealed class VersionedFactory(IDurableGameFactory inner, string application) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility with
        {
            Application = application,
        };

        public OpenedGame Create(GameSpecification specification) => inner.Create(specification);
    }

    protected sealed class MigrationSessionStore(params StoredSession[] sessions) : ISessionStore
    {
        private readonly List<StoredSession> sessions = [..sessions];
        public int Commits { get; private set; }

        public IReadOnlyList<StoredSession> Load() => [..sessions];
        public string? Commit(StoredSession session)
        {
            SessionSaveJson.Validate(session.Save);
            int index = sessions.FindIndex(existing => existing.Save.Session.StorageId == session.Save.Session.StorageId);
            if (index < 0)
            {
                throw new InvalidOperationException("replacement session was not loaded");
            }

            sessions[index] = session;
            Commits++;
            return null;
        }
    }

    protected sealed class SequenceCapabilities(params string[] capabilities) : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);
        public string Issue() => capabilities.Dequeue();
    }

    protected sealed class FailingFactory(string message) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) => throw new InvalidOperationException(message);
    }

    protected sealed class OffTurnActionFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card AuntMay { get; private set; } = null!;
        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            Game = opened.Game;
            var world = Game.State;
            AuntMay = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(5);
            return opened;
        }
    }

    protected sealed class NestedContinuationFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        private readonly AbilityBook abilities = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"seq":[
                {"dealDamage":{"cards":"you","amount":1}},
                {"choose":{"options":[
                  {"dealDamage":{"cards":"you","amount":1}},
                  {"dealDamage":{"cards":"you","amount":2}}
                ]}},
                {"dealDamage":{"cards":"you","amount":4}}
              ]}
            }]}]}
            """);
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card Source { get; private set; } = null!;
        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            Source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            var runner = new AbilityRunner(abilities);
            Game = Game.Begin(world, world.Facts, runner);
            return opened with
            {
                Game = Game
            };
        }
    }

    protected sealed class InHandActionFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card AuntMay { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            AuntMay = world.CreateCard("01006", world.Seats[0].Hand);
            world.Seats[0].IdentityCard.TakeDamage(5);
            return opened;
        }
    }

    protected sealed class VanishingOffTurnActionFactory(IDurableGameFactory inner, bool winningCommandEndsGame) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card ActiveSource { get; private set; } = null!;
        public Card OffTurnSource { get; private set; } = null!;
        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            ActiveSource = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            OffTurnSource = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(1);
            Game = Game.Begin(world, world.Facts, new CrossSeatHealingActions(ActiveSource, OffTurnSource, winningCommandEndsGame));
            return opened with
            {
                Game = Game
            };
        }
    }

    protected sealed class ExcessHealingFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card AttackSource { get; private set; } = null!;
        public Card HealSource { get; private set; } = null!;
        public Card UpgradeSource { get; private set; } = null!;
        public Card Victim { get; private set; } = null!;
        public Card ExcessMeter { get; private set; } = null!;
        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            Area supports = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
            AttackSource = world.CreateCard("01006", supports);
            HealSource = world.CreateCard("01006", supports);
            UpgradeSource = world.CreateCard("01006", supports);
            Area minions = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: World.Scenario);
            Victim = world.CreateCard("01101", minions);
            ExcessMeter = world.CreateCard("01184", minions);
            world.Seats[0].IdentityCard.TakeDamage(5);
            Game = Game.Begin(world, world.Facts, new ExcessHealingActions(AttackSource, HealSource, UpgradeSource, Victim, ExcessMeter));
            return opened with
            {
                Game = Game
            };
        }
    }

    protected sealed class ExcessHealingActions(Card attack, Card heal, Card upgrade, Card victim, Card excessMeter) : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Actions(World world, int player)
        {
            if (player != 0)
            {
                return[];
            }

            var actions = new List<PendingAbility>();
            if (attack.Ready && DeckTypes.IsInPlay(victim.Area.Type))
            {
                actions.Add(new PendingAbility(attack.ObjectId, AbilityType.Action, player));
            }

            if (heal.Ready && excessMeter.Damage > 0)
            {
                actions.Add(new PendingAbility(heal.ObjectId, AbilityType.Action, player));
            }

            if (upgrade.Ready)
            {
                actions.Add(new PendingAbility(upgrade.ObjectId, AbilityType.Action, player));
            }

            return actions;
        }

        public override Affordance Describe(World world, PendingAbility ability) => new(ability.Card, Game.ActionVerb, ability.Card, ability.Player, ability.Card == attack.ObjectId ? "Attack and record excess" : ability.Card == heal.ObjectId ? "Heal for recorded excess" : "Increase the later attack");
        public override IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null, IReadOnlyList<ResourceAllocation>? allocations = null)
        {
            Card source = world.Cards[ability.Card];
            source.Exhaust();
            var events = new List<GameEvent>
            {
                new FieldSet(source.ObjectId, "is_exhaust", 0, 1),
            };
            if (source == upgrade)
            {
                return events;
            }

            if (source == attack)
            {
                long amount = upgrade.Ready ? 4 : 6;
                Damage.AttackResult result = DamageAttacks.Attack(world, world.Facts, world.Seats[0].IdentityCard, victim, amount, "rewrite-example", Game.ActionVerb, events, retaliate: false);
                long before = DamagePlacement.Health(world, world.Facts, excessMeter) - excessMeter.Damage;
                excessMeter.TakeDamage(result.Excess);
                events.Add(new FieldSet(excessMeter.ObjectId, "health", before, before - result.Excess));
                return events;
            }

            DamageRecovery.Heal(world, world.Facts, world.Seats[0].IdentityCard, excessMeter.Damage, "rewrite-example", Game.ActionVerb, events);
            return events;
        }
    }

    protected sealed class CrossSeatHealingActions(Card active, Card offTurn, bool winningCommandEndsGame) : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Actions(World world, int player) => player switch
        {
            0 => [new PendingAbility(active.ObjectId, AbilityType.Action, player)],
            1 when world.Seats[1].IdentityCard.Damage > 0 => [new PendingAbility(offTurn.ObjectId, AbilityType.Action, player)],
            _ => [],
        };
        public override Affordance Describe(World world, PendingAbility ability) => new(ability.Card, Game.ActionVerb, ability.Card, ability.Player, ability.Player == 0 ? "Heal player 2" : "Use player 2 action");
        public override IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null, IReadOnlyList<ResourceAllocation>? allocations = null)
        {
            var events = new List<GameEvent>();
            if (ability.Player == 0)
            {
                DamageRecovery.Heal(world, world.Facts, world.Seats[1].IdentityCard, amount: 1, trigger: "test", verb: Game.ActionVerb, events);
                if (winningCommandEndsGame)
                {
                    world.Finish(Outcome.PlayersWin);
                }
            }

            return events;
        }
    }

    protected sealed class HostileVisibilityPolicy(IReadOnlyList<SeatScope>? grants) : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) => new RestrictedVisibilityPolicy(0).Authorize(null, players);
        public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players) => grants!;
    }

    protected sealed class NullPrimaryVisibilityPolicy : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) => null!;
        public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players) => [];
    }

    protected sealed class ChangingSeatGrants(IReadOnlyList<SeatScope> first, IReadOnlyList<SeatScope> later) : IReadOnlyList<SeatScope>
    {
        public int Enumerations { get; private set; }
        public int Count => first.Count;

        public SeatScope this[int index] => first[index];
        public IEnumerator<SeatScope> GetEnumerator()
        {
            Enumerations++;
            return (Enumerations == 1 ? first : later).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
