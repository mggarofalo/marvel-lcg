using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

internal sealed record TranscriptResult(
    string Obligation,
    string Scenario,
    string Digest,
    IReadOnlyList<GameEvent> Events);

internal sealed record TranscriptBinding(
    string Name,
    TranscriptStepKind Kind,
    Regex Pattern,
    Action<TranscriptContext, TranscriptStep, Match> Execute);

internal sealed record BoundTranscriptStep(
    TranscriptStep Step,
    TranscriptBinding Binding,
    Match Match);

internal sealed class TranscriptContext
{
    public TranscriptContext(
        string obligation,
        SetupCatalog setup,
        CardCatalog cards,
        AbilityBook abilities)
    {
        Obligation = obligation;
        Setup = setup;
        Cards = cards;
        Abilities = abilities;
    }

    public string Obligation { get; }

    public SetupCatalog Setup { get; }

    public CardCatalog Cards { get; }

    public AbilityBook Abilities { get; }

    public CanonicalCoreScene? Scene { get; set; }

    public List<GameEvent> Events { get; } = [];

    public string CurrentPrompt { get; set; } = "<none>";

    public string? ExpectedException { get; set; }

    public RulesNotImplementedException? PendingException { get; set; }

    public string? ExceptionDigest { get; set; }

    public bool ExceptionObserved { get; set; }

    public (int Seat, string From, string To)? LastFormChange { get; set; }

    public IReadOnlySet<int>? LastCardOptions { get; set; }

    public bool? LastAvailability { get; set; }

    public string? LastInspectedFace { get; set; }

    public Prompt? PendingPrompt { get; set; }

    public World World => Scene?.World
        ?? throw new TranscriptException("a canonical Core scene has not been constructed");
}

internal sealed class CoreTranscriptRunner
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);
    private readonly IReadOnlyList<TranscriptBinding> bindings;
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityBook abilities;

    public CoreTranscriptRunner(string root)
        : this(root, null)
    {
    }

    internal CoreTranscriptRunner(
        string root, IReadOnlyList<TranscriptBinding>? bindingOverride)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        setup = SetupCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "setup", "setup.json")));
        cards = CardCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "cards", "cards.json")));
        abilities = AbilityCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "abilities", "abilities.json")));
        bindings = bindingOverride ?? DefaultVocabulary();
    }

    public TranscriptResult Execute(TranscriptScenario scenario) => Execute(scenario, null);

    internal TranscriptResult Execute(
        TranscriptScenario scenario, string? expectedException)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var context = new TranscriptContext(scenario.Obligation, setup, cards, abilities)
        {
            ExpectedException = expectedException,
        };
        IReadOnlyList<BoundTranscriptStep> bound = BindAll(context, scenario);
        foreach (BoundTranscriptStep current in bound)
        {
            TranscriptStep step = current.Step;
            if (context.ExceptionObserved)
            {
                throw Failure(context, scenario, step, TranscriptFailureKind.Validation,
                    "unused step after the expected exception was observed", null);
            }

            if (context.PendingException is not null
                && current.Binding.Name != "cataloged-exception")
            {
                throw Failure(context, scenario, step, TranscriptFailureKind.Validation,
                    "the step after an unimplemented decision must observe its cataloged exception",
                    null);
            }

            try
            {
                current.Binding.Execute(context, step, current.Match);
            }
            catch (RulesNotImplementedException error)
                when (step.Kind == TranscriptStepKind.When
                    && context.ExpectedException is not null)
            {
                if (context.Scene is null)
                {
                    throw Failure(context, scenario, step,
                        TranscriptFailureKind.Execution,
                        "unimplemented decision was reached before a legal scene existed",
                        error);
                }

                context.PendingException = error;
                context.ExceptionDigest = context.World.Digest().Fingerprint();
            }
            catch (TranscriptAssertionException error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Assertion, error.Message, error);
            }
            catch (TranscriptException error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Execution, error.Message, error);
            }
            catch (Exception error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Execution,
                    $"{error.GetType().Name}: {error.Message}", error);
            }
        }

        if (context.Scene is null)
        {
            throw new TranscriptException(
                $"{scenario.Location}: {scenario.Obligation}: scenario never constructs a scene");
        }

        if (context.ExpectedException is not null && !context.ExceptionObserved)
        {
            throw new TranscriptException(
                $"{scenario.Location}: expected '{context.ExpectedException}', but the scenario completed");
        }

        return new TranscriptResult(
            scenario.Obligation,
            $"{scenario.Location.Path}::{scenario.Name}",
            context.ExceptionDigest ?? context.World.Digest().Fingerprint(),
            [.. context.Events]);
    }

    private List<BoundTranscriptStep> BindAll(
        TranscriptContext context, TranscriptScenario scenario)
    {
        var bound = new List<BoundTranscriptStep>();
        foreach (TranscriptStep step in scenario.Steps)
        {
            var matches = bindings
                .Where(binding => binding.Kind == step.Kind)
                .Select(binding => (Binding: binding, Match: binding.Pattern.Match(step.Text)))
                .Where(candidate => candidate.Match.Success)
                .ToList();
            if (matches.Count != 1)
            {
                string reason = matches.Count == 0
                    ? $"unknown {step.Kind} step '{step.Text}'"
                    : $"ambiguous {step.Kind} step '{step.Text}'; matched "
                      + string.Join(", ", matches.Select(candidate => candidate.Binding.Name));
                TranscriptFailureKind kind = matches.Count == 0
                    ? TranscriptFailureKind.UnknownStep
                    : TranscriptFailureKind.AmbiguousStep;
                throw Failure(context, scenario, step, kind, reason, null);
            }

            bound.Add(new BoundTranscriptStep(step, matches[0].Binding, matches[0].Match));
        }

        return bound;
    }

    internal static IReadOnlyList<TranscriptBinding> DefaultVocabulary() =>
    [
        Bind("core-scene", TranscriptStepKind.Given,
            "a canonical Core scene is dealt", DealScene),
        Bind("stack-player-deck", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards", StackPlayerDeck),
        Bind("stack-player-deck-empty-discard", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards with all other deck cards in hand",
            StackPlayerDeckWithEmptyDiscard),
        Bind("stack-player-deck-leave", TranscriptStepKind.Given,
            @"these cards are next on seat (?<seat>\d+)'s player deck",
            StackPlayerDeckLeavingRemainder),
        Bind("set-player-hand", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s hand contains exactly these cards", SetPlayerHand),
        Bind("set-card-readiness", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            SetCardReadiness),
        Bind("set-card-damage", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) damage",
            SetCardDamage),
        Bind("set-card-counters", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<type>[a-z-]+) counters?",
            SetCardCounters),
        Bind("set-identity-face", TranscriptStepKind.Given,
            @"seat (?<seat>\d+) shows identity face (?<face>\d+[a-z]?)",
            SetIdentityFace),
        Bind("place-ally", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an ally controlled by seat (?<seat>\d+)",
            PlaceAlly),
        Bind("place-support", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a support controlled by seat (?<seat>\d+)",
            PlaceSupport),
        Bind("engage-minion", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a minion engaged with seat (?<seat>\d+)",
            EngageMinion),
        Bind("place-side-scheme", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is a side scheme in play",
            PlaceSideScheme),
        Bind("attach-identity-upgrade", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an upgrade attached to seat (?<seat>\d+)'s identity",
            AttachIdentityUpgrade),
        Bind("give-card-status", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has a (?<status>stunned|confused|tough) status card",
            GiveCardStatus),
        Bind("stack-encounter-deck-discard", TranscriptStepKind.Given,
            "the encounter deck contains only these next cards with all other deck cards in the encounter discard pile",
            StackEncounterDeckWithDiscard),
        Bind("stack-encounter-deck-dealt", TranscriptStepKind.Given,
            @"the encounter deck contains only these next cards with all other deck cards dealt facedown to seat (?<seat>\d+)",
            StackEncounterDeckWithDealtCards),
        Bind("stack-encounter-deck-leave", TranscriptStepKind.Given,
            "these cards are next on the encounter deck", StackEncounterDeckLeavingRemainder),
        Bind("draw-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) draws (?<count>\d+) cards?", DrawCards),
        Bind("discard-player-deck", TranscriptStepKind.When,
            @"seat (?<seat>\d+) discards the top (?<count>\d+) cards? of their player deck",
            DiscardPlayerDeck),
        Bind("discard-from-hand", TranscriptStepKind.When,
            @"seat (?<seat>\d+) discards card (?<face>\d+[a-z]?) copy (?<copy>\d+) from hand",
            DiscardFromHand),
        Bind("deal-encounter-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) is dealt (?<count>\d+) encounter cards?",
            DealEncounterCards),
        Bind("discard-encounter-deck", TranscriptStepKind.When,
            @"the top (?<count>\d+) cards? of the encounter deck (?:is|are) discarded",
            DiscardEncounterDeck),
        Bind("phase-discard-none", TranscriptStepKind.When,
            @"seat (?<seat>\d+) keeps every card during the optional end-of-player-phase discard",
            PhaseDiscardNone),
        Bind("phase-discard-selected", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses these cards for the end-of-player-phase discard",
            PhaseDiscardSelected),
        Bind("phase-draw", TranscriptStepKind.When,
            "the end-of-player-phase draw step resolves", PhaseDraw),
        Bind("phase-ready", TranscriptStepKind.When,
            "the end-of-player-phase ready step resolves", PhaseReady),
        Bind("villain-damages-card", TranscriptStepKind.When,
            @"the villain deals (?<count>\d+) damage to card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            VillainDamagesCard),
        Bind("identity-defeated", TranscriptStepKind.When,
            @"seat (?<seat>\d+)'s identity is defeated", IdentityDefeated),
        Bind("change-form", TranscriptStepKind.When,
            @"seat (?<seat>\d+) changes form by flipping their identity", ChangeForm),
        Bind("inflict-status", TranscriptStepKind.When,
            @"an ability (?<action>stuns|confuses) card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            InflictStatus),
        Bind("card-deals-damage", TranscriptStepKind.When,
            @"card (?<source>\d+[a-z]?) copy (?<sourceCopy>\d+) deals (?<count>\d+) damage to card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            CardDealsDamage),
        Bind("place-threat", TranscriptStepKind.When,
            @"(?<count>\d+) threat is placed on card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            PlaceThreat),
        Bind("basic-attack", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic attack against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            BasicAttack),
        Bind("basic-thwart", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic thwart against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            BasicThwart),
        Bind("basic-recovery", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic recovery",
            BasicRecovery),
        Bind("ally-power", TranscriptStepKind.When,
            @"card (?<ally>\d+[a-z]?) copy (?<allyCopy>\d+) uses its basic (?<power>attack|thwart) against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            AllyPower),
        Bind("basic-attack-targets", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks for their basic attack targets",
            BasicAttackTargets),
        Bind("basic-thwart-targets", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks for their basic thwart targets",
            BasicThwartTargets),
        Bind("basic-recovery-availability", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks whether basic recovery is available",
            BasicRecoveryAvailability),
        Bind("villain-phase-decline", TranscriptStepKind.When,
            @"villain phase (?<round>\d+) resolves with every optional choice declined",
            ResolveVillainPhase),
        Bind("villain-phase-defender", TranscriptStepKind.When,
            @"villain phase (?<round>\d+) resolves with card (?<face>\d+[a-z]?) copy (?<copy>\d+) defending the first attack",
            ResolveVillainPhaseWithDefender),
        Bind("minion-enters-play", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) enters play as a minion engaged with seat (?<seat>\d+)",
            MinionEntersPlay),
        Bind("support-enters-play", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) enters play as a support controlled by seat (?<seat>\d+)",
            SupportEntersPlay),
        Bind("request-printed-characteristics", TranscriptStepKind.When,
            @"the printed characteristics of card (?<face>\d+[a-z]?) copy (?<copy>\d+) are requested",
            RequestPrintedCharacteristics),
        Bind("initiate-action-with-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action paying with these cards",
            InitiateActionWithPayment),
        Bind("initiate-action-without-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action without payment",
            InitiateActionWithoutPayment),
        Bind("choose-pending-card", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses card (?<face>\d+[a-z]?) copy (?<copy>\d+) for the pending action",
            ChoosePendingCard),
        Bind("hand-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in hand", HandCount),
        Bind("player-deck-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their player deck", PlayerDeckCount),
        Bind("player-discard-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their discard pile", PlayerDiscardCount),
        Bind("encounter-queue-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) facedown encounter cards?", EncounterCount),
        Bind("facedown-encounter-queue-card", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is facedown in seat (?<seat>\d+)'s encounter queue",
            FacedownEncounterQueueCard),
        Bind("encounter-deck-count", TranscriptStepKind.Then,
            @"the encounter deck has (?<count>\d+) cards?", EncounterDeckCount),
        Bind("encounter-discard-count", TranscriptStepKind.Then,
            @"the encounter discard pile has (?<count>\d+) cards?", EncounterDiscardCount),
        Bind("acceleration-token-count", TranscriptStepKind.Then,
            @"the main scheme has (?<count>\d+) acceleration tokens?", AccelerationTokenCount),
        Bind("card-readiness", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            CardReadiness),
        Bind("card-player-discard", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s discard pile",
            CardInPlayerDiscard),
        Bind("card-top-player-discard", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is faceup on top of seat (?<seat>\d+)'s discard pile",
            CardOnTopOfPlayerDiscard),
        Bind("card-top-encounter-discard", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is faceup on top of the encounter discard pile",
            CardOnTopOfEncounterDiscard),
        Bind("card-resolving", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is faceup in the resolving area",
            CardResolving),
        Bind("player-discard-order", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s discard pile has these cards from top to bottom",
            PlayerDiscardOrder),
        Bind("event-card-order", TranscriptStepKind.Then,
            @"the (?<verb>[A-Za-z_]+) events moved these cards in order",
            EventCardOrder),
        Bind("not-eliminated", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is not eliminated", NotEliminated),
        Bind("game-unfinished", TranscriptStepKind.Then,
            "the game is unfinished", GameUnfinished),
        Bind("event-emitted", TranscriptStepKind.Then,
            @"a (?<verb>[A-Za-z_]+) event with trigger (?<trigger>.+) was emitted", EventEmitted),
        Bind("event-count", TranscriptStepKind.Then,
            @"(?<count>\d+) (?<verb>[A-Za-z_]+) events? (?:was|were) emitted", EventCount),
        Bind("event-order", TranscriptStepKind.Then,
            @"a (?<first>[A-Za-z_]+) event was emitted before a (?<second>[A-Za-z_]+) event",
            EventOrder),
        Bind("players-lose", TranscriptStepKind.Then,
            "the players lose the game", PlayersLose),
        Bind("villain-wins", TranscriptStepKind.Then,
            "the villain wins the game", VillainWins),
        Bind("players-win", TranscriptStepKind.Then,
            "the players win the game", PlayersWin),
        Bind("seat-eliminated", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is eliminated", SeatEliminated),
        Bind("first-player", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has the first player token", FirstPlayer),
        Bind("minion-engaged", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is engaged with seat (?<seat>\d+)",
            MinionEngaged),
        Bind("card-damage", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) damage",
            CardDamage),
        Bind("card-counters", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<type>[a-z-]+) counters?",
            CardCounters),
        Bind("card-target-availability", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<availability>available|unavailable) as a target",
            CardTargetAvailability),
        Bind("basic-recovery-result", TranscriptStepKind.Then,
            @"basic recovery is (?<availability>available|unavailable)",
            BasicRecoveryResult),
        Bind("card-removed", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is removed from the game",
            CardRemoved),
        Bind("card-is-villain", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is the faceup villain",
            CardIsVillain),
        Bind("card-is-main-scheme", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is the faceup main scheme",
            CardIsMainScheme),
        Bind("player-order", TranscriptStepKind.Then,
            @"the player order is (?<order>[\d,]+)", PlayerOrder),
        Bind("per-player-count", TranscriptStepKind.Then,
            @"the per-player count is (?<count>\d+)", PerPlayerCount),
        Bind("card-event-order", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) had a (?<first>[A-Za-z_]+) event before an (?<second>[A-Za-z_]+) event",
            CardEventOrder),
        Bind("seat-form", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is in (?<form>hero|alter-ego) form", SeatForm),
        Bind("form-transition", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) changed from (?<from>hero|alter-ego) to (?<to>hero|alter-ego) form",
            FormTransition),
        Bind("card-status", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has a (?<status>stunned|confused|tough) status card",
            CardStatus),
        Bind("upgrade-attached-identity", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) remains attached to seat (?<seat>\d+)'s identity",
            UpgradeAttachedIdentity),
        Bind("status-count", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<status>stunned|confused|tough) status cards?",
            StatusCount),
        Bind("card-afflicted", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<status>stunned|confused)",
            CardAfflicted),
        Bind("printed-characteristics", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) exposes these printed characteristics",
            PrintedCharacteristics),
        Bind("pending-card-offered", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is offered by the pending action",
            PendingCardOffered),
        Bind("cataloged-exception", TranscriptStepKind.Then,
            "the engine raises the cataloged unimplemented rule exception", CatalogedException),
    ];

    private static TranscriptBinding Bind(
        string name,
        TranscriptStepKind kind,
        string pattern,
        Action<TranscriptContext, TranscriptStep, Match> execute) =>
        new(name, kind, new Regex(
            $"\\A{pattern}\\z",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            PatternTimeout), execute);

    private static void DealScene(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        TranscriptTable table = step.Table
            ?? throw new TranscriptException($"{step.Location}: expected a table");
        string[] required = ["campaign", "heroes", "seed"];
        string[] allowed = [.. required, "modular sets"];
        var unused = table.Header.Except(allowed, StringComparer.Ordinal).ToList();
        var missing = required.Except(table.Header, StringComparer.Ordinal).ToList();
        if (unused.Count > 0 || missing.Count > 0)
        {
            string detail = string.Join("; ", new[]
            {
                unused.Count == 0 ? null : $"unused columns: {string.Join(", ", unused)}",
                missing.Count == 0 ? null : $"missing columns: {string.Join(", ", missing)}",
            }.Where(value => value is not null));
            throw new TranscriptException(
                $"{step.Location}: {detail}");
        }

        bool hasModularSets = table.Header.Contains("modular sets", StringComparer.Ordinal);

        if (table.Rows.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: expected exactly one table row; found {table.Rows.Count}");
        }

        IReadOnlyDictionary<string, string> row = table.Rows[0];
        if (!uint.TryParse(row["seed"], NumberStyles.None, CultureInfo.InvariantCulture,
                out uint seed))
        {
            throw new TranscriptException($"{step.Location}: seed must be an unsigned integer");
        }

        IReadOnlyList<string> heroes = [.. row["heroes"]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        if (heroes.Count == 0)
        {
            throw new TranscriptException($"{step.Location}: heroes must not be empty");
        }

        context.Scene = CanonicalCoreScene.Deal(
            new CoreSceneRequest(
                context.Obligation,
                row["campaign"],
                heroes,
                seed,
                hasModularSets
                    ? [.. row["modular sets"].Split(
                        ',', StringSplitOptions.TrimEntries
                             | StringSplitOptions.RemoveEmptyEntries)]
                    : null),
            context.Setup,
            context.Cards,
            new AbilityRunner(context.Abilities));
    }

    private static void StackPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Discard);

    private static void StackPlayerDeckWithEmptyDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Hand);

    private static void StackPlayerDeckLeavingRemainder(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Leave);

    private static void StackPlayerDeck(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        PlayerDeckRemainder remainder)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "next card", "copy");
        context.SceneRequired(step).Apply(new StackPlayerDeck(
            seat,
            [.. table.Rows.Select(row => new SceneCard(
                row["next card"], TableNumber(row, "copy", step)))],
            remainder));
    }

    private static void SetPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        context.SceneRequired(step).Apply(new SetPlayerHand(
            Seat(match, step),
            [.. table.Rows.Select(row => new SceneCard(
                row["card"], TableNumber(row, "copy", step)))]));
    }

    private static void SetCardReadiness(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneReady(
            SceneCard(match, step),
            string.Equals(match.Groups["state"].Value, "ready", StringComparison.Ordinal)));

    private static void SetCardDamage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneDamage(
            SceneCard(match, step), Number(match, "count", step)));

    private static void SetCardCounters(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneCounters(
            SceneCard(match, step),
            match.Groups["type"].Value,
            Number(match, "count", step)));

    private static void SetIdentityFace(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneForm(
            Seat(match, step), match.Groups["face"].Value));

    private static void PlaceAlly(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Ally, Seat(match, step))));

    private static void PlaceSupport(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Support, Seat(match, step))));

    private static void EngageMinion(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.EngagedMinion, Seat(match, step))));

    private static void PlaceSideScheme(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step), new SceneDestination(SceneZone.SideScheme)));

    private static void AttachIdentityUpgrade(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Upgrade, Seat(match, step))));

    private static void GiveCardStatus(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new GiveSceneStatus(
            SceneCard(match, step), match.Groups["status"].Value));

    private static void StackEncounterDeckWithDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        StackEncounterDeck(
            context, step, EncounterDeckRemainder.Discard, seat: 0);
    }

    private static void StackEncounterDeckWithDealtCards(
        TranscriptContext context, TranscriptStep step, Match match) =>
        StackEncounterDeck(
            context,
            step,
            EncounterDeckRemainder.Dealt,
            Seat(match, step));

    private static void StackEncounterDeckLeavingRemainder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        StackEncounterDeck(
            context, step, EncounterDeckRemainder.Leave, seat: 0);
    }

    private static void StackEncounterDeck(
        TranscriptContext context,
        TranscriptStep step,
        EncounterDeckRemainder remainder,
        int seat)
    {
        TranscriptTable table = Table(step, "next card", "copy");
        context.SceneRequired(step).Apply(new StackEncounterDeck(
            [.. table.Rows.Select(row => new SceneCard(
                row["next card"], TableNumber(row, "copy", step)))],
            remainder,
            seat));
    }

    private static void DrawCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Draw.Cards(context.World, seat, count, "behavioral transcript", context.Events);
    }

    private static void DiscardPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = PlayerDeck.DiscardTop(
            context.World, seat, count, "behavioral transcript", context.Events);
    }

    private static void DiscardFromHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        var reference = new SceneCard(
            match.Groups["face"].Value, Number(match, "copy", step));
        Card card = context.SceneRequired(step).Find(reference);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Hand))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} is not in seat {seat + 1}'s hand");
        }

        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Discard.Card(context.World, card, "behavioral transcript", context.Events);
    }

    private static void DealEncounterCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        for (int index = 0; index < count; index++)
        {
            _ = Deal.EncounterCard(
                context.World, seat, "behavioral transcript", context.Events);
        }
    }

    private static void DiscardEncounterDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = EncounterDeck.DiscardTop(
            context.World,
            Number(match, "count", step),
            "behavioral transcript",
            context.Events);
    }

    private static void PhaseDiscardNone(
        TranscriptContext context, TranscriptStep step, Match match) =>
        PhaseDiscard(context, step, Seat(match, step), []);

    private static void PhaseDiscardSelected(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        PhaseDiscard(
            context,
            step,
            Seat(match, step),
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(
                new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    private static void PhaseDiscard(
        TranscriptContext context,
        TranscriptStep step,
        int seat,
        IReadOnlyList<int> cards)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DiscardToHandSize(
            context.World, context.Cards, seat, cards, context.Events);
    }

    private static void PhaseDraw(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DrawToHandSize(context.World, context.Cards, context.Events);
    }

    private static void PhaseReady(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.ReadyCards(context.World, context.Events);
    }

    private static void VillainDamagesCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = Damage.Deal(
            context.World,
            context.Cards,
            villain,
            target,
            Number(match, "count", step),
            "behavioral transcript",
            "Damage",
            context.Events);
    }

    private static void IdentityDefeated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Elimination.Eliminate(
            context.World,
            context.Cards,
            Seat(match, step),
            "behavioral transcript",
            context.Events);
    }

    private static void ChangeForm(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        int seatIndex = Seat(match, step);
        Seat seat = context.World.Seats[seatIndex];
        string from = FormName(Forms.Of(context.World, seat, context.Cards));
        _ = Forms.Change(seat, context.Cards);
        string to = FormName(Forms.Of(context.World, seat, context.Cards));
        context.LastFormChange = (seatIndex, from, to);
    }

    private static void InflictStatus(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string status = match.Groups["action"].Value == "stuns"
            ? Statuses.Stunned
            : Statuses.Confused;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = Statuses.Inflict(context.World, context.Cards, target, status);
    }

    private static void CardDealsDamage(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card source = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["source"].Value,
            Number(match, "sourceCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = Damage.Deal(
            context.World,
            context.Cards,
            source,
            target,
            Number(match, "count", step),
            "behavioral transcript",
            "Damage",
            context.Events);
    }

    private static void PlaceThreat(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Place(
            context.World,
            context.Cards,
            context.World.Abilities,
            scheme,
            Number(match, "count", step),
            "behavioral transcript",
            context.Events);
        FinishAgenda(context, step);
    }

    private static void BasicAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowers.BasicAttack(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        FinishAgenda(context, step);
    }

    private static void BasicThwart(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowers.BasicThwart(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        FinishAgenda(context, step);
    }

    private static void BasicRecovery(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowers.BasicRecovery(
            context.World, context.Cards, Seat(match, step), context.Events);
    }

    private static void AllyPower(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card ally = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["ally"].Value,
            Number(match, "allyCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string verb = match.Groups["power"].Value == "attack"
            ? BasicPowers.AttackVerb
            : BasicPowers.ThwartVerb;
        BasicPowers.AllyPower(context.World, context.Cards, ally, target, verb, context.Events);
        FinishAgenda(context, step);
    }

    private static void BasicAttackTargets(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastAvailability = null;
        context.LastCardOptions = BasicPowers.Attackable(
                context.World, context.Cards, Seat(match, step))
            .Select(card => card.ObjectId)
            .ToHashSet();
    }

    private static void BasicThwartTargets(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastAvailability = null;
        context.LastCardOptions = BasicPowers.Thwartable(
                context.World, context.Cards, Seat(match, step))
            .Select(card => card.ObjectId)
            .ToHashSet();
    }

    private static void BasicRecoveryAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastCardOptions = null;
        context.LastAvailability = BasicPowers.CanRecover(
            context.World, context.Cards, Seat(match, step));
    }

    private static void ResolveVillainPhase(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgenda(context, step);
    }

    private static void ResolveVillainPhaseWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));

        bool defended = false;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 answers");
            }

            Decision decision = Decision.Decline;
            if (!defended && asked.Asking == Question.Defender)
            {
                if (!asked.Affordances.Any(option => option.AnchorId == defender.ObjectId))
                {
                    throw new TranscriptException(
                        $"{step.Location}: card {defender.ObjectId} was not offered as a defender");
                }

                decision = Decision.Take(defender.ObjectId);
                defended = true;
            }

            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                decision,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }

        if (!defended)
        {
            throw new TranscriptException(
                $"{step.Location}: the villain phase offered no defender window");
        }
    }

    private static void FinishAgenda(TranscriptContext context, TranscriptStep step)
    {
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 declines");
            }

            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                Decision.Decline,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }
    }

    private static void MinionEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        int seat = Seat(match, step);
        Card minion = context.SceneRequired(step).Find(SceneCard(match, step));
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.EngagedMinion, seat)));
        Reveal.Quickstrike(context.World, context.Cards, minion, seat, round: 1);
        FinishAgenda(context, step);
    }

    private static void SupportEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Support, Seat(match, step))));
    }

    private static void RequestPrintedCharacteristics(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        context.LastInspectedFace = match.Groups["face"].Value;
    }

    private static void InitiateActionWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    private static void InitiateActionWithoutPayment(
        TranscriptContext context, TranscriptStep step, Match match) =>
        InitiateAction(context, step, match, []);

    private static void InitiateAction(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        IReadOnlyList<int> payments)
    {
        int seat = Seat(match, step);
        Card source = context.SceneRequired(step).Find(SceneCard(match, step));
        var runner = (AbilityRunner)context.World.Abilities;
        var action = runner.Actions(context.World, seat)
            .Single(candidate => candidate.Card == source.ObjectId);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.Events.AddRange(runner.Act(context.World, action, payments, []));
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, runner, context.Events));
    }

    private static void ChoosePendingCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        var offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == target.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {target.ObjectId} is not offered by '{asked.Label}'");
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void SetPendingPrompt(TranscriptContext context, Prompt? prompt)
    {
        context.PendingPrompt = prompt;
        context.CurrentPrompt = prompt?.Label ?? "<none>";
    }

    private static void HandCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Hand.Cards.Count,
            "cards in hand", step);

    private static void PlayerDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Deck.Cards.Count,
            "cards in the player deck", step);

    private static void PlayerDiscardCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DiscardPile,
                PlayArea.Of(seat),
                cardOwner: seat).Cards.Count,
            "cards in the discard pile", step);
    }

    private static void EncounterCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DealtEncounterCardsDeck,
                PlayArea.Of(Seat(match, step))).Cards.Count,
            "facedown encounter cards", step);

    private static void FacedownEncounterQueueCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.FaceUp
            || card.Area.Type != DeckType.DealtEncounterCardsDeck
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} facedown in seat "
                + $"{seat + 1}'s encounter queue; was {card.Area}, faceup={card.FaceUp}");
        }
    }

    private static void EncounterDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDeck).Cards.Count,
            "cards in the encounter deck", step);

    private static void EncounterDiscardCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDiscardPile).Cards.Count,
            "cards in the encounter discard pile", step);

    private static void AccelerationTokenCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        long actual = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        Equal(Number(match, "count", step), checked((int)actual),
            "acceleration tokens on the main scheme", step);
    }

    private static void CardReadiness(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = string.Equals(
            match.Groups["state"].Value, "ready", StringComparison.Ordinal);
        if (card.Ready != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be "
                + $"{match.Groups["state"].Value}");
        }
    }

    private static void CardInPlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Area expected = context.World.AreaOf(
            DeckType.DiscardPile,
            PlayArea.Of(seat),
            cardOwner: seat);
        if (!ReferenceEquals(card.Area, expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s "
                + $"discard pile; was {card.Area}");
        }
    }

    private static void CardOnTopOfPlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        AssertFaceupTop(
            context,
            step,
            match,
            context.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat));
    }

    private static void CardOnTopOfEncounterDiscard(
        TranscriptContext context, TranscriptStep step, Match match) =>
        AssertFaceupTop(
            context, step, match, context.World.AreaOf(DeckType.EncounterDiscardPile));

    private static void CardResolving(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.RevealingArea || !card.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {card.ObjectId} in the resolving area; "
                + $"was {card.Area}, faceup={card.FaceUp}");
        }
    }

    private static void AssertFaceupTop(
        TranscriptContext context, TranscriptStep step, Match match, Area area)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = area.Cards.Count == 0 ? null : area.Cards[^1];
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} on top of "
                + $"{area}; was {(actual is null ? "<empty>" : actual.ObjectId)}");
        }
    }

    private static void PlayerDiscardOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "card", "copy");
        int[] expected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        int[] actual = [.. context.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat)
            .Cards.AsEnumerable().Reverse().Take(expected.Length)
            .Select(card => card.ObjectId)];
        if (!actual.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected discard top order "
                + $"{string.Join(',', expected)}; was {string.Join(',', actual)}");
        }
    }

    private static void EventCardOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        int[] expected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        int[] actual = [.. context.Events
            .OfType<CardsMoved>()
            .Where(moved => moved.Verb == match.Groups["verb"].Value)
            .SelectMany(moved => moved.Cards)
            .Select(landing => landing.Card)];
        if (!actual.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["verb"].Value} card order "
                + $"{string.Join(',', expected)}; was {string.Join(',', actual)}");
        }
    }

    private static void NotEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} not to be eliminated");
        }
    }

    private static void GameUnfinished(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.Unfinished)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected an unfinished game; was {context.World.Result}");
        }
    }

    private static void CatalogedException(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.ExpectedException is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no cataloged exception is expected for this obligation");
        }

        string actual = context.PendingException is null
            ? "(none)"
            : $"{context.PendingException.GetType().Name}: {context.PendingException.Message}";
        if (!string.Equals(actual, context.ExpectedException, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected '{context.ExpectedException}'; reached '{actual}'");
        }

        context.ExceptionObserved = true;
    }

    private static void EventEmitted(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string verb = match.Groups["verb"].Value;
        string trigger = match.Groups["trigger"].Value;
        if (!context.Events.Any(gameEvent =>
                gameEvent.Verb == verb && gameEvent.Trigger == trigger))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no {verb} event with trigger {trigger} was emitted");
        }
    }

    private static void EventCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            context.Events.Count(gameEvent =>
                gameEvent.Verb == match.Groups["verb"].Value),
            $"{match.Groups["verb"].Value} events",
            step);

    private static void EventOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int first = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["first"].Value);
        int second = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["second"].Value);
        if (first < 0 || second <= first)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["first"].Value} before "
                + match.Groups["second"].Value);
        }
    }

    private static void PlayersLose(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.PlayersLose)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected PlayersLose; was {context.World.Result}");
        }
    }

    private static void VillainWins(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.VillainWins)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected VillainWins; was {context.World.Result}");
        }
    }

    private static void PlayersWin(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.PlayersWin)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected PlayersWin; was {context.World.Result}");
        }
    }

    private static void SeatEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (!context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} to be eliminated");
        }
    }

    private static void FirstPlayer(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Seat(match, step), context.World.FirstPlayer, "first-player seat", step);

    private static void MinionEngaged(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.EngagedEnemiesArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} engaged with seat "
                + $"{seat + 1}; was {card.Area}");
        }
    }

    private static void CardDamage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            checked((int)context.SceneRequired(step).Find(SceneCard(match, step)).Damage),
            "damage on the card",
            step);

    private static void CardCounters(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string type = match.Groups["type"].Value;
        string key = type == "threat" ? "k_threat" : $"c_{type}";
        Equal(
            Number(match, "count", step),
            checked((int)context.SceneRequired(step).Find(SceneCard(match, step))
                .Tokens.GetValueOrDefault(key)),
            $"{type} counters on the card",
            step);
    }

    private static void CardTargetAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        if (context.LastCardOptions is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no target query has been made");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = match.Groups["availability"].Value == "available";
        bool actual = context.LastCardOptions.Contains(card.ObjectId);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be "
                + $"{match.Groups["availability"].Value} as a target");
        }
    }

    private static void BasicRecoveryResult(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        bool expected = match.Groups["availability"].Value == "available";
        if (context.LastAvailability != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected basic recovery to be "
                + $"{match.Groups["availability"].Value}; was "
                + (context.LastAvailability is null
                    ? "not queried"
                    : context.LastAvailability.Value ? "available" : "unavailable"));
        }
    }

    private static void CardRemoved(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.RemovedArea)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} removed; was {card.Area}");
        }
    }

    private static void CardIsVillain(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = context.World.TheCardIn(DeckType.VillainArea);
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} as villain; was "
                + (actual is null ? "<none>" : $"{actual.ObjectId}, faceup={actual.FaceUp}"));
        }
    }

    private static void CardIsMainScheme(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = context.World.TheCardIn(DeckType.MainSchemesArea);
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} as main scheme; was "
                + (actual is null ? "<none>" : $"{actual.ObjectId}, faceup={actual.FaceUp}"));
        }
    }

    private static void PlayerOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int[] expected = [.. match.Groups["order"].Value
            .Split(',')
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture) - 1)];
        if (!context.World.PlayerOrder.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected player order "
                + $"{string.Join(',', expected.Select(seat => seat + 1))}; was "
                + string.Join(',', context.World.PlayerOrder.Select(seat => seat + 1)));
        }
    }

    private static void PerPlayerCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step), context.World.Players,
            "players counted by the per-player icon", step);

    private static void CardEventOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int card = context.SceneRequired(step).Find(SceneCard(match, step)).ObjectId;
        int first = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["first"].Value
            && EventLands(gameEvent, card));
        int second = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["second"].Value
            && EventLands(gameEvent, card));
        if (first < 0 || second <= first)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["first"].Value} before "
                + $"{match.Groups["second"].Value} for card {card}");
        }
    }

    private static bool EventLands(GameEvent gameEvent, int card) =>
        gameEvent is CardsMoved moved
        && moved.Cards.Any(landing => landing.Card == card);

    private static void SeatForm(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        string expected = match.Groups["form"].Value == "alter-ego"
            ? Forms.AlterEgo
            : Forms.Hero;
        if (!Forms.In(context.World, context.World.Seats[seat], context.Cards, expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} in {expected} form");
        }
    }

    private static void FormTransition(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string expectedFrom = match.Groups["from"].Value;
        string expectedTo = match.Groups["to"].Value;
        var expected = (Seat(match, step), expectedFrom, expectedTo);
        if (context.LastFormChange != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected form transition {expected}; was "
                + (context.LastFormChange?.ToString() ?? "<none>"));
        }
    }

    private static string FormName(IReadOnlySet<string> forms) => forms.Single() switch
    {
        Forms.AlterEgo => "alter-ego",
        Forms.Hero => "hero",
        string form => form,
    };

    private static void CardStatus(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!Statuses.Has(context.World, card, match.Groups["status"].Value))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to have "
                + $"{match.Groups["status"].Value} status");
        }
    }

    private static void UpgradeAttachedIdentity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Card identity = context.World.Seats[seat].IdentityCard;
        if (card.Area.Type != DeckType.UpgradesArea
            || card.Area.Host != identity.ObjectId
            || card.Area.PlayArea != identity.Area.PlayArea)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} attached to "
                + $"seat {seat + 1}'s identity; was {card.Area}");
        }
    }

    private static void StatusCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Equal(
            Number(match, "count", step),
            Statuses.Count(context.World, card, match.Groups["status"].Value),
            $"{match.Groups["status"].Value} status cards",
            step);
    }

    private static void CardAfflicted(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string status = match.Groups["status"].Value;
        if (!Statuses.Afflicted(context.World, context.Cards, card, status))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be {status}");
        }
    }

    private static void PrintedCharacteristics(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string face = match.Groups["face"].Value;
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!string.Equals(context.LastInspectedFace, face, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: printed characteristics for {face} were not requested");
        }
        TranscriptTable table = Table(step, "field", "value");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string field = row["field"];
            string actual = field switch
            {
                "name" => context.Cards.Title(face),
                "subtitle" => context.Cards.Subtitle(face),
                "type" => PrintedType(context.Cards.Kind(face)),
                "traits" => string.Join('/', context.Cards.Traits(face)
                    .Select(trait => trait.Replace('_', ' '))),
                _ when field.StartsWith("attribute:", StringComparison.Ordinal) =>
                    context.Cards.Attributes(face).TryGetValue(
                        field["attribute:".Length..], out string? value)
                        ? value
                        : "<absent>",
                _ => throw new TranscriptException(
                    $"{step.Location}: unknown printed field '{field}'"),
            };
            if (!string.Equals(row["value"], actual, StringComparison.Ordinal))
            {
                throw new TranscriptAssertionException(
                    $"{step.Location}: expected {face} {field} '{row["value"]}'; was '{actual}'");
            }
        }
    }

    private static void PendingCardOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending action prompt");
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!asked.Affordances.Any(offer => offer.AnchorId == card.ObjectId))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} is not offered by '{asked.Label}'");
        }
    }

    private static string PrintedType(CardKind kind) => kind switch
    {
        CardKind.EncounterVillain => "Villain",
        CardKind.EncounterSideScheme => "SideScheme",
        _ => kind.ToString(),
    };

    private static void Equal(int expected, int actual, string observation, TranscriptStep step)
    {
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} {observation}; was {actual}");
        }
    }

    private static int Seat(Match match, TranscriptStep step)
    {
        int oneBased = Number(match, "seat", step);
        if (oneBased <= 0)
        {
            throw new TranscriptException($"{step.Location}: seat numbers begin at 1");
        }

        return oneBased - 1;
    }

    private static int Number(Match match, string group, TranscriptStep step)
    {
        if (!int.TryParse(
                match.Groups[group].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int value))
        {
            throw new TranscriptException(
                $"{step.Location}: '{match.Groups[group].Value}' is not a {group}");
        }

        return value;
    }

    private static SceneCard SceneCard(Match match, TranscriptStep step) => new(
        match.Groups["face"].Value,
        Number(match, "copy", step));

    private static int TableNumber(
        IReadOnlyDictionary<string, string> row, string column, TranscriptStep step)
    {
        if (!int.TryParse(
                row[column], NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            throw new TranscriptException(
                $"{step.Location}: '{row[column]}' is not a non-negative {column}");
        }

        return value;
    }

    private static IReadOnlyDictionary<string, string> OneRow(
        TranscriptStep step, params string[] columns)
    {
        TranscriptTable table = Table(step, columns);
        if (table.Rows.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: expected exactly one table row; found {table.Rows.Count}");
        }

        return table.Rows[0];
    }

    private static TranscriptTable Table(TranscriptStep step, params string[] columns)
    {
        TranscriptTable table = step.Table
            ?? throw new TranscriptException($"{step.Location}: step requires a table");
        var unused = table.Header.Except(columns, StringComparer.Ordinal).ToList();
        var missing = columns.Except(table.Header, StringComparer.Ordinal).ToList();
        if (unused.Count > 0 || missing.Count > 0)
        {
            string detail = string.Join("; ", new[]
            {
                unused.Count == 0 ? null : $"unused columns: {string.Join(", ", unused)}",
                missing.Count == 0 ? null : $"missing columns: {string.Join(", ", missing)}",
            }.Where(value => value is not null));
            throw new TranscriptException($"{step.Location}: {detail}");
        }

        return table;
    }

    private static TranscriptException Failure(
        TranscriptContext context,
        TranscriptScenario scenario,
        TranscriptStep step,
        TranscriptFailureKind kind,
        string reason,
        Exception? inner)
    {
        string digest = context.Scene is null
            ? "<scene not constructed>"
            : context.World.Digest().Fingerprint();
        string recent = context.Events.Count == 0
            ? "<none>"
            : string.Join(Environment.NewLine,
                context.Events.TakeLast(5).Select(gameEvent =>
                    $"  - {gameEvent.GetType().Name}: {gameEvent}"));
        string message = $"""
            obligation: {scenario.Obligation}
            feature: {scenario.Location.Path}
            line: {step.Location.Line}
            step: {step.Kind} {step.Text}
            reason: {reason}
            world-digest: {digest}
            current-prompt: {context.CurrentPrompt}
            recent-events:
            {recent}
            """;
        return new TranscriptException(kind, message, inner, digest);
    }
}

internal static class TranscriptContextExtensions
{
    public static CanonicalCoreScene SceneRequired(
        this TranscriptContext context, TranscriptStep step) =>
        context.Scene ?? throw new TranscriptException(
            $"{step.Location}: a canonical Core scene must be dealt first");
}

internal sealed class CoreTranscriptSuite
{
    private readonly string root;
    private readonly CatalogEvidence catalog;
    private readonly CoreTranscriptRunner runner;

    public CoreTranscriptSuite(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        runner = new CoreTranscriptRunner(this.root);
        catalog = ReadCatalog(Path.Combine(
            this.root, "specs", "behavior", "catalog.json"));
    }

    public IReadOnlyList<TranscriptResult> RunPassingCorpus()
    {
        string directory = Path.Combine(root, "specs", "behavior", "core");
        var paths = Directory.EnumerateFiles(directory, "*.feature")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        if (paths.Count == 0)
        {
            throw new TranscriptException("specs/behavior/core contains no executable features");
        }

        var results = new List<TranscriptResult>();
        var executed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
            TranscriptFeature feature = TranscriptParser.Parse(root, path);
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                CatalogObligation obligation = ValidateAuthority(
                    scenario, requireCompletionEvidence: true);
                string reference = Reference(scenario);
                if (!executed.Add(reference))
                {
                    throw new TranscriptException(
                        $"{scenario.Location}: duplicate executed scenario reference '{reference}'");
                }
                if (obligation.Implementation == "supported")
                {
                    results.Add(runner.Execute(scenario));
                }
                else
                {
                    results.Add(RunUnimplemented(scenario, obligation));
                }
            }
        }

        ValidateScenarioCompleteness(ExpectedScenarioReferences(), executed);

        return results;
    }

    public TranscriptException RunQuarantine()
    {
        string path = Path.Combine(root, "specs", "self-test", "quarantine.feature");
        TranscriptFeature feature = TranscriptParser.Parse(
            root, path, "the executable runner rejects a false hand count");
        try
        {
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                _ = ValidateAuthority(scenario, requireCompletionEvidence: false);
                _ = runner.Execute(scenario);
            }
        }
        catch (TranscriptException expected)
            when (expected.Kind == TranscriptFailureKind.Assertion)
        {
            return expected;
        }
        catch (TranscriptException wrongFailure)
        {
            throw new TranscriptException(
                TranscriptFailureKind.Validation,
                "quarantine did not reach its deliberately false assertion",
                wrongFailure);
        }

        throw new TranscriptException(
            "specs/self-test/quarantine.feature passed; its false assertion no longer proves the runner");
    }

    internal void ValidateForPassing(TranscriptScenario scenario) =>
        _ = ValidateAuthority(scenario, requireCompletionEvidence: true);

    private CatalogObligation ValidateAuthority(
        TranscriptScenario scenario, bool requireCompletionEvidence)
    {
        CatalogObligation obligation = ValidateNamedAuthority(
            scenario, scenario.Obligation, requireCompletionEvidence, "primary");
        foreach (string covered in scenario.CoveredObligations)
        {
            CatalogObligation secondary = ValidateNamedAuthority(
                scenario, covered, requireCompletionEvidence, "covered");
            if (!string.Equals(
                    secondary.Implementation, obligation.Implementation,
                    StringComparison.Ordinal))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: covered obligation '{covered}' is "
                    + $"{secondary.Implementation}, but the primary obligation is "
                    + $"{obligation.Implementation}");
            }
        }

        if (requireCompletionEvidence)
        {
            string reference = Reference(scenario);
            var declared = scenario.CoveredObligations
                .Prepend(scenario.Obligation)
                .ToHashSet(StringComparer.Ordinal);
            var linked = catalog.Obligations.Values
                .Where(candidate => candidate.Scenarios.Contains(
                    reference, StringComparer.Ordinal))
                .Select(candidate => candidate.Id)
                .ToHashSet(StringComparer.Ordinal);
            if (!declared.SetEquals(linked))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: catalog coverage differs from declared coverage; "
                    + $"declared [{string.Join(", ", declared.Order())}], linked "
                    + $"[{string.Join(", ", linked.Order())}]");
            }
        }

        return obligation;
    }

    private CatalogObligation ValidateNamedAuthority(
        TranscriptScenario scenario,
        string obligationId,
        bool requireCompletionEvidence,
        string role)
    {
        if (!catalog.Obligations.TryGetValue(
                obligationId, out CatalogObligation? obligation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: stale or missing {role} obligation '{obligationId}'");
        }

        if (scenario.Authorities.Count == 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: scenario has no direct authority tags");
        }

        var missing = scenario.Authorities
            .Where(authority => !catalog.Sources.ContainsKey(authority))
            .ToList();
        if (missing.Count > 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: missing direct authorities: {string.Join(", ", missing)}");
        }

        var outsideCore = scenario.Authorities
            .Where(authority => catalog.Sources.TryGetValue(
                authority, out CatalogSource? source)
                && source.Disposition == "outside-core")
            .ToList();
        if (outsideCore.Count > 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: outside-Core direct authorities: "
                + string.Join(", ", outsideCore));
        }

        if (!scenario.Authorities.Contains(obligation.Source, StringComparer.Ordinal))
        {
            throw new TranscriptException(
                $"{scenario.Location}: {role} obligation derives from '{obligation.Source}', "
                + "which is not a direct authority tag");
        }

        if (!string.Equals(obligation.Disposition, "executable", StringComparison.Ordinal)
            || obligation.Implementation is not ("supported" or "unimplemented"))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' is "
                + $"{obligation.Disposition}/{obligation.Implementation ?? "(none)"}, "
                + "not executable with a completed implementation status");
        }

        if (!requireCompletionEvidence)
        {
            return obligation;
        }

        string reference = Reference(scenario);
        if (!obligation.Scenarios.Contains(reference, StringComparer.Ordinal))
        {
            throw new TranscriptException(
                $"{scenario.Location}: catalog does not link scenario '{reference}'");
        }

        if (string.IsNullOrWhiteSpace(obligation.Mutation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' has no mutation evidence");
        }

        if (obligation.Implementation == "unimplemented"
            && string.IsNullOrWhiteSpace(obligation.Exception))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' names no expected exception");
        }

        return obligation;
    }

    private TranscriptResult RunUnimplemented(
        TranscriptScenario scenario, CatalogObligation obligation) =>
        runner.Execute(scenario, obligation.Exception);

    private IReadOnlySet<string> ExpectedScenarioReferences() =>
        CompletedScenarioReferences(catalog.Obligations.Values);

    internal static IReadOnlySet<string> CompletedScenarioReferences(
        IEnumerable<CatalogObligation> obligations)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (CatalogObligation obligation in obligations.Where(obligation =>
                     obligation.Disposition == "executable"
                     && obligation.Implementation is "supported" or "unimplemented"))
        {
            // During catalog derivation, an unimplemented status names a known
            // engine gap before its negative transcript is admitted. Once any
            // scenario is linked, the runner holds that evidence bidirectionally.
            // MARVEL-307 turns absence itself into the final module gate.
            if (obligation.Implementation == "unimplemented"
                && obligation.Scenarios.Count == 0)
            {
                continue;
            }

            if (obligation.Scenarios.Count == 0)
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no scenarios");
            }

            if (string.IsNullOrWhiteSpace(obligation.Mutation))
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no mutation evidence");
            }

            if (obligation.Implementation == "unimplemented"
                && string.IsNullOrWhiteSpace(obligation.Exception))
            {
                throw new TranscriptException(
                    $"unimplemented obligation '{obligation.Id}' names no expected exception");
            }

            foreach (string reference in obligation.Scenarios)
            {
                _ = references.Add(reference);
            }
        }

        return references;
    }

    internal static void ValidateScenarioCompleteness(
        IReadOnlySet<string> expected, IReadOnlySet<string> executed)
    {
        var missing = expected.Except(executed, StringComparer.Ordinal).ToList();
        var unexpected = executed.Except(expected, StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || unexpected.Count > 0)
        {
            string details = string.Join("; ", new[]
            {
                missing.Count == 0
                    ? null
                    : $"catalog scenarios not executed: {string.Join(", ", missing)}",
                unexpected.Count == 0
                    ? null
                    : $"executed scenarios absent from catalog: {string.Join(", ", unexpected)}",
            }.Where(value => value is not null));
            throw new TranscriptException(details);
        }
    }

    private static string Reference(TranscriptScenario scenario) =>
        $"{scenario.Location.Path}::{scenario.Name}";

    private static CatalogEvidence ReadCatalog(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        var sources = new Dictionary<string, CatalogSource>(StringComparer.Ordinal);
        var obligations = new Dictionary<string, CatalogObligation>(StringComparer.Ordinal);
        foreach (JsonElement source in document.RootElement
                     .GetProperty("sources").EnumerateArray())
        {
            string sourceId = source.GetProperty("id").GetString()!;
            sources.Add(sourceId, new CatalogSource(
                source.GetProperty("disposition").GetString()!));
            foreach (JsonElement obligation in source
                         .GetProperty("obligations").EnumerateArray())
            {
                string id = obligation.GetProperty("id").GetString()!;
                obligations.Add(id, new CatalogObligation(
                    id,
                    sourceId,
                    obligation.GetProperty("disposition").GetString()!,
                    obligation.GetProperty("implementation").GetString(),
                    [.. obligation.GetProperty("scenarios").EnumerateArray()
                        .Select(item => item.GetString()!)],
                    obligation.GetProperty("mutation").GetString(),
                    obligation.GetProperty("exception").GetString()));
            }
        }

        return new CatalogEvidence(sources, obligations);
    }
}

internal sealed record CatalogObligation(
    string Id,
    string Source,
    string Disposition,
    string? Implementation,
    IReadOnlyList<string> Scenarios,
    string? Mutation,
    string? Exception);

internal sealed record CatalogSource(string Disposition);

internal sealed record CatalogEvidence(
    IReadOnlyDictionary<string, CatalogSource> Sources,
    IReadOnlyDictionary<string, CatalogObligation> Obligations);
