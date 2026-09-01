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
using Marvel.Rules.Timing;

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

    public (int Card, string Resources)? LastResourceGeneration { get; set; }

    public Prompt? PendingPrompt { get; set; }

    public Game? Game { get; set; }

    public (int Seat, IReadOnlyList<int> Unshuffled, IReadOnlyList<int> After)?
        LastSetupDeckShuffle { get; set; }

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
        Bind("set-empty-player-hand", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s hand is empty", SetEmptyPlayerHand),
        Bind("set-card-readiness", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            SetCardReadiness),
        Bind("set-card-damage", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) damage",
            SetCardDamage),
        Bind("set-card-counters", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<type>[a-z-]+) counters?",
            SetCardCounters),
        Bind("set-acceleration-tokens", TranscriptStepKind.Given,
            @"the main scheme has (?<count>\d+) acceleration tokens?",
            SetAccelerationTokens),
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
        Bind("place-obligation", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an obligation in seat (?<seat>\d+)'s play area",
            PlaceObligation),
        Bind("place-player-discard", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) starts in seat (?<seat>\d+)'s discard pile",
            PlacePlayerDiscard),
        Bind("attach-identity-upgrade", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is an upgrade attached to seat (?<seat>\d+)'s identity",
            AttachIdentityUpgrade),
        Bind("attach-card", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is attached to card (?<host>\d+[a-z]?) copy (?<hostCopy>\d+)",
            AttachCard),
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
        Bind("discard-card-effect", TranscriptStepKind.When,
            @"an effect attempts to discard card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            DiscardCardEffect),
        Bind("deal-encounter-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) is dealt (?<count>\d+) encounter cards?",
            DealEncounterCards),
        Bind("discard-encounter-deck", TranscriptStepKind.When,
            @"the top (?<count>\d+) cards? of the encounter deck (?:is|are) discarded",
            DiscardEncounterDeck),
        Bind("reveal-encounter-card", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is revealed to seat (?<seat>\d+)",
            RevealEncounterCard),
        Bind("assign-threat-accepting", TranscriptStepKind.When,
            @"(?<count>\d+) threat is assigned to the main scheme for seat (?<seat>\d+) accepting ""(?<label>[^""]+)""",
            AssignThreatAccepting),
        Bind("begin-threat-assignment", TranscriptStepKind.When,
            @"(?<count>\d+) threat begins assignment to the main scheme for seat (?<seat>\d+)",
            BeginThreatAssignment),
        Bind("answer-encounter-card", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses option (?<option>\d+) for the pending encounter-card decision",
            AnswerEncounterCard),
        Bind("order-pending-players", TranscriptStepKind.When,
            @"seat (?<seat>\d+) orders these players for the pending encounter-card decision",
            OrderPendingPlayers),
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
        Bind("end-player-phase", TranscriptStepKind.When,
            "the player phase ends", EndPlayerPhase),
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
        Bind("remove-threat", TranscriptStepKind.When,
            @"(?<count>\d+) threat is removed from card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            RemoveThreat),
        Bind("basic-attack", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic attack against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            BasicAttack),
        Bind("begin-basic-attack", TranscriptStepKind.When,
            @"seat (?<seat>\d+) begins their basic attack against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            BeginBasicAttack),
        Bind("basic-attack-accepting-paid", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic attack against card (?<face>\d+[a-z]?) copy (?<copy>\d+) and accepts ""(?<label>[^""]+)"" targeting card (?<target>\d+[a-z]?) copy (?<targetCopy>\d+) paid with card (?<payment>\d+[a-z]?) copy (?<paymentCopy>\d+)",
            BasicAttackAcceptingWithPayment),
        Bind("basic-thwart", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic thwart against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            BasicThwart),
        Bind("basic-recovery", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses their basic recovery",
            BasicRecovery),
        Bind("ally-power", TranscriptStepKind.When,
            @"card (?<ally>\d+[a-z]?) copy (?<allyCopy>\d+) uses its basic (?<power>attack|thwart) against card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            AllyPower),
        Bind("ally-power-accepting", TranscriptStepKind.When,
            @"card (?<ally>\d+[a-z]?) copy (?<allyCopy>\d+) uses its basic (?<power>attack|thwart) against card (?<face>\d+[a-z]?) copy (?<copy>\d+) and accepts the (?<label>.+) opportunity",
            AllyPowerAccepting),
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
        Bind("villain-phase-opportunity", TranscriptStepKind.When,
            @"villain phase (?<round>\d+) resolves accepting ""(?<label>[^""]+)""",
            ResolveVillainPhaseAccepting),
        Bind("villain-phase-paid-opportunity", TranscriptStepKind.When,
            @"villain phase (?<round>\d+) resolves accepting ""(?<label>[^""]+)"" paid with card (?<face>\d+[a-z]?) copy (?<copy>\d+)",
            ResolveVillainPhaseAcceptingWithPayment),
        Bind("villain-phase-defender", TranscriptStepKind.When,
            @"villain phase (?<round>\d+) resolves with card (?<face>\d+[a-z]?) copy (?<copy>\d+) defending the first attack",
            ResolveVillainPhaseWithDefender),
        Bind("villain-attack-defender", TranscriptStepKind.When,
            @"the villain attacks seat (?<seat>\d+) with card (?<face>\d+[a-z]?) copy (?<copy>\d+) defending",
            ResolveVillainAttackWithDefender),
        Bind("villain-attack-decline", TranscriptStepKind.When,
            @"the villain attacks seat (?<seat>\d+) with every optional choice declined",
            ResolveVillainAttack),
        Bind("villain-scheme-decline", TranscriptStepKind.When,
            @"the villain schemes against seat (?<seat>\d+) with every optional choice declined",
            ResolveVillainScheme),
        Bind("villain-attack-defender-opportunity", TranscriptStepKind.When,
            @"the villain attacks seat (?<seat>\d+) accepting ""(?<label>[^""]+)"" with card (?<face>\d+[a-z]?) copy (?<copy>\d+) defending",
            ResolveVillainAttackWithDefenderAndOpportunity),
        Bind("villain-attack-opportunity", TranscriptStepKind.When,
            @"the villain attacks seat (?<seat>\d+) accepting ""(?<label>[^""]+)""",
            ResolveVillainAttackWithOpportunity),
        Bind("villain-attack-two-opportunities", TranscriptStepKind.When,
            @"the villain attacks seat (?<seat>\d+) accepting ""(?<first>[^""]+)"" then ""(?<second>[^""]+)""",
            ResolveVillainAttackWithTwoOpportunities),
        Bind("minion-enters-play", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) enters play as a minion engaged with seat (?<seat>\d+)",
            MinionEntersPlay),
        Bind("support-enters-play", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) enters play as a support controlled by seat (?<seat>\d+)",
            SupportEntersPlay),
        Bind("upgrade-enters-play", TranscriptStepKind.When,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) enters play as an upgrade controlled by seat (?<seat>\d+)",
            UpgradeEntersPlay),
        Bind("request-printed-characteristics", TranscriptStepKind.When,
            @"the printed characteristics of card (?<face>\d+[a-z]?) copy (?<copy>\d+) are requested",
            RequestPrintedCharacteristics),
        Bind("initiate-action-with-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action paying with these cards",
            InitiateActionWithPayment),
        Bind("initiate-action-without-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action without payment",
            InitiateActionWithoutPayment),
        Bind("initiate-action-with-discard", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action discarding these cards",
            InitiateActionWithDiscard),
        Bind("initiate-indexed-action-with-variable", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s (?<ordinal>first|second) printed action defining X as (?<value>\d+) paying with these cards",
            InitiateIndexedActionWithVariable),
        Bind("initiate-indexed-action-without-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) initiates card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s (?<ordinal>first|second) printed action without payment",
            InitiateIndexedActionWithoutPayment),
        Bind("play-card-with-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) plays card (?<face>\d+[a-z]?) copy (?<copy>\d+) paying with these cards",
            PlayCardWithPayment),
        Bind("request-card-actions", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks for available card actions",
            RequestCardActions),
        Bind("use-card-resource-ability", TranscriptStepKind.When,
            @"seat (?<seat>\d+) uses card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s resource ability",
            UseCardResourceAbility),
        Bind("request-turn-card-actions", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks for card actions available during their turn",
            RequestTurnCardActions),
        Bind("inspect-core-scene", TranscriptStepKind.When,
            "the dealt Core scene is inspected", InspectCoreScene),
        Bind("begin-mulligan", TranscriptStepKind.When,
            @"game setup reaches seat (?<seat>\d+)'s mulligan", BeginMulligan),
        Bind("resolve-mulligan", TranscriptStepKind.When,
            @"seat (?<seat>\d+) mulligans these cards", ResolveMulligan),
        Bind("keep-mulligan", TranscriptStepKind.When,
            @"seat (?<seat>\d+) keeps every opening-hand card at mulligan",
            KeepMulligan),
        Bind("end-player-turn", TranscriptStepKind.When,
            @"seat (?<seat>\d+) ends their turn",
            EndPlayerTurn),
        Bind("request-voluntary-form-change", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks whether a voluntary form change is available",
            RequestVoluntaryFormChange),
        Bind("request-card-play", TranscriptStepKind.When,
            @"seat (?<seat>\d+) asks whether card (?<face>\d+[a-z]?) copy (?<copy>\d+) is available to play",
            RequestCardPlay),
        Bind("take-voluntary-form-change", TranscriptStepKind.When,
            @"seat (?<seat>\d+) takes their voluntary form change",
            TakeVoluntaryFormChange),
        Bind("choose-setup-card", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses card (?<face>\d+[a-z]?) copy (?<copy>\d+) for the pending setup ability",
            ChooseSetupCard),
        Bind("choose-pending-card", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses card (?<face>\d+[a-z]?) copy (?<copy>\d+) for the pending action",
            ChoosePendingCard),
        Bind("choose-pending-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses these cards for the pending action",
            ChoosePendingCards),
        Bind("order-pending-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) orders these cards for the pending action",
            OrderPendingCards),
        Bind("choose-pending-card-and-discard", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses card (?<face>\d+[a-z]?) copy (?<copy>\d+) and discards these cards for the pending action",
            ChoosePendingCardAndDiscard),
        Bind("choose-pending-card-with-payment", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses card (?<face>\d+[a-z]?) copy (?<copy>\d+) paying with these cards for the pending action",
            ChoosePendingCardWithPayment),
        Bind("accept-pending-opportunity", TranscriptStepKind.When,
            @"seat (?<seat>\d+) accepts the ""(?<label>[^""]+)"" pending opportunity",
            AcceptPendingOpportunity),
        Bind("accept-card-pending-opportunity", TranscriptStepKind.When,
            @"seat (?<seat>\d+) accepts card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s pending opportunity",
            AcceptCardPendingOpportunity),
        Bind("decline-pending-opportunity", TranscriptStepKind.When,
            @"seat (?<seat>\d+) declines the pending opportunity",
            DeclinePendingOpportunity),
        Bind("pending-opportunity-offered", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is offered the ""(?<label>[^""]+)"" pending opportunity",
            PendingOpportunityOffered),
        Bind("pending-window-pass", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) may pass the pending window",
            PendingWindowPass),
        Bind("pending-option-availability", TranscriptStepKind.Then,
            @"option (?<option>\d+) is (?<availability>offered|not offered) by the pending decision",
            PendingOptionAvailability),
        Bind("hand-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in hand", HandCount),
        Bind("mulligan-offered", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is offered a mulligan", MulliganOffered),
        Bind("active-player", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is the active player", ActivePlayer),
        Bind("end-phase-discard-offered", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is offered the end-of-player-phase discard",
            EndPhaseDiscardOffered),
        Bind("pending-setup-card-offered", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is offered by the pending setup ability",
            PendingCardOffered),
        Bind("pending-setup-card-not-offered", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is not offered by the pending setup ability",
            PendingCardNotOffered),
        Bind("pending-order-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is asked to order (?<count>\d+) cards? for the pending action",
            PendingOrderCount),
        Bind("simultaneous-effect-choice-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is asked to choose between (?<count>\d+) simultaneous effects",
            SimultaneousEffectChoiceCount),
        Bind("pending-choice-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is asked to choose (?<count>\d+) cards? for the pending action",
            PendingChoiceCount),
        Bind("pending-player-order-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is asked to order (?<count>\d+) players? for the pending encounter-card decision",
            PendingPlayerOrderCount),
        Bind("setup-deck-shuffled", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s player deck was shuffled by the setup ability",
            SetupDeckShuffled),
        Bind("card-player-hand", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s hand",
            CardInPlayerHand),
        Bind("card-player-deck", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s player deck",
            CardInPlayerDeck),
        Bind("player-deck-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their player deck", PlayerDeckCount),
        Bind("player-deck-top-face", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s player deck has card (?<face>\d+[a-z]?) on top",
            PlayerDeckTopFace),
        Bind("player-discard-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their discard pile", PlayerDiscardCount),
        Bind("encounter-queue-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) facedown encounter cards?", EncounterCount),
        Bind("facedown-encounter-queue-card", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is facedown in seat (?<seat>\d+)'s encounter queue",
            FacedownEncounterQueueCard),
        Bind("encounter-deck-count", TranscriptStepKind.Then,
            @"the encounter deck has (?<count>\d+) cards?", EncounterDeckCount),
        Bind("encounter-deck-face-counts", TranscriptStepKind.Then,
            "the encounter deck contains these card counts", EncounterDeckFaceCounts),
        Bind("owned-player-card-counts", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s player cards contain these counts",
            OwnedPlayerCardCounts),
        Bind("player-count", TranscriptStepKind.Then,
            @"the game has (?<count>\d+) players?", PlayerCount),
        Bind("encounter-discard-count", TranscriptStepKind.Then,
            @"the encounter discard pile has (?<count>\d+) cards?", EncounterDiscardCount),
        Bind("acceleration-token-count", TranscriptStepKind.Then,
            @"the main scheme has (?<count>\d+) acceleration tokens?", AccelerationTokenCount),
        Bind("card-readiness", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            CardReadiness),
        Bind("generated-resources", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) generated (?<resources>[A-Z]+) resources",
            GeneratedResources),
        Bind("last-attack-defense", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) defended the last attack without a basic defense",
            LastAttackDefense),
        Bind("last-attack-undefended", TranscriptStepKind.Then,
            "the last attack was undefended", LastAttackUndefended),
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
            @"(?:a|an) (?<first>[A-Za-z_]+) event was emitted before (?:a|an) (?<second>[A-Za-z_]+) event",
            EventOrder),
        Bind("players-lose", TranscriptStepKind.Then,
            "the players lose the game", PlayersLose),
        Bind("villain-wins", TranscriptStepKind.Then,
            "the villain wins the game", VillainWins),
        Bind("players-win", TranscriptStepKind.Then,
            "the players win the game", PlayersWin),
        Bind("seat-eliminated", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is eliminated", SeatEliminated),
        Bind("attack-ended", TranscriptStepKind.Then,
            "the attack has ended", AttackEnded),
        Bind("player-play-area-removed", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s play area is removed", PlayerPlayAreaRemoved),
        Bind("first-player", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has the first player token", FirstPlayer),
        Bind("minion-engaged", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is engaged with seat (?<seat>\d+)",
            MinionEngaged),
        Bind("facedown-drone-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) facedown Drone minions?",
            FacedownDroneCount),
        Bind("ally-controlled", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) remains an ally controlled by seat (?<seat>\d+)",
            AllyControlled),
        Bind("support-controlled", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) remains a support controlled by seat (?<seat>\d+)",
            SupportControlled),
        Bind("card-owned", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is owned by seat (?<seat>\d+)",
            CardOwned),
        Bind("card-controlled", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is controlled by seat (?<seat>\d+)",
            CardControlled),
        Bind("card-damage", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) damage",
            CardDamage),
        Bind("card-remaining-hit-points", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) remaining hit points",
            CardRemainingHitPoints),
        Bind("card-counters", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<type>[a-z-]+) counters?",
            CardCounters),
        Bind("card-target-availability", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<availability>available|unavailable) as a target",
            CardTargetAvailability),
        Bind("basic-recovery-result", TranscriptStepKind.Then,
            @"basic recovery is (?<availability>available|unavailable)",
            BasicRecoveryResult),
        Bind("voluntary-form-change-result", TranscriptStepKind.Then,
            @"a voluntary form change is (?<availability>available|unavailable)",
            BasicRecoveryResult),
        Bind("card-play-result", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<availability>available|unavailable) to play",
            CardPlayResult),
        Bind("modified-card-cost", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has modified resource cost (?<count>\d+) for seat (?<seat>\d+)",
            ModifiedCardCost),
        Bind("modified-card-statistic", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has modified (?<field>ATK|DEF|HS|REC|THW) (?<count>\d+)",
            ModifiedCardStatistic),
        Bind("card-removed", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is removed from the game",
            CardRemoved),
        Bind("card-is-villain", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is the faceup villain",
            CardIsVillain),
        Bind("card-in-villain-deck", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in the villain deck",
            CardInVillainDeck),
        Bind("card-is-main-scheme", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is the faceup main scheme",
            CardIsMainScheme),
        Bind("card-in-encounter-deck", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in the encounter deck",
            CardInEncounterDeck),
        Bind("card-in-seat-nemesis", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s set-aside nemesis pile",
            CardInSeatNemesis),
        Bind("card-in-play", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in play", CardInPlay),
        Bind("card-out-of-play", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is out of play", CardOutOfPlay),
        Bind("card-player-play-area", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s play area",
            CardInPlayerPlayArea),
        Bind("card-not-player-play-area", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is not in seat (?<seat>\d+)'s play area",
            CardNotInPlayerPlayArea),
        Bind("card-villain-play-area", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in the villain's play area",
            CardInVillainPlayArea),
        Bind("card-not-villain-play-area", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is not in the villain's play area",
            CardNotInVillainPlayArea),
        Bind("player-order", TranscriptStepKind.Then,
            @"the player order is (?<order>[\d,]+)", PlayerOrder),
        Bind("per-player-count", TranscriptStepKind.Then,
            @"the per-player count is (?<count>\d+)", PerPlayerCount),
        Bind("card-event-order", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) had a (?<first>[A-Za-z_]+) event before an (?<second>[A-Za-z_]+) event",
            CardEventOrder),
        Bind("card-discard-after-event", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) was discarded after a (?<verb>[A-Za-z_]+) event",
            CardDiscardAfterEvent),
        Bind("seat-form", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is in (?<form>hero|alter-ego) form", SeatForm),
        Bind("identity-face-out-of-play", TranscriptStepKind.Then,
            @"seat (?<seat>\d+)'s identity face (?<face>\d+[a-z]?) is out of play",
            IdentityFaceOutOfPlay),
        Bind("form-transition", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) changed from (?<from>hero|alter-ego) to (?<to>hero|alter-ego) form",
            FormTransition),
        Bind("card-status", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has a (?<status>stunned|confused|tough) status card",
            CardStatus),
        Bind("upgrade-attached-identity", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) remains attached to seat (?<seat>\d+)'s identity",
            UpgradeAttachedIdentity),
        Bind("card-attached-card", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is attached to card (?<host>\d+[a-z]?) copy (?<hostCopy>\d+)",
            CardAttachedToCard),
        Bind("facedown-card-attached-card", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is facedown attached to card (?<host>\d+[a-z]?) copy (?<hostCopy>\d+)",
            FacedownCardAttachedToCard),
        Bind("status-count", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has (?<count>\d+) (?<status>stunned|confused|tough) status cards?",
            StatusCount),
        Bind("card-afflicted", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<status>stunned|confused)",
            CardAfflicted),
        Bind("card-trait", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) has the (?<trait>[A-Z][A-Z ]*) trait",
            CardTrait),
        Bind("card-no-trait", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) does not have the (?<trait>[A-Z][A-Z ]*) trait",
            CardDoesNotHaveTrait),
        Bind("printed-characteristics", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) exposes these printed characteristics",
            PrintedCharacteristics),
        Bind("pending-card-offered", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is offered by the pending action",
            PendingCardOffered),
        Bind("pending-card-not-offered", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is not offered by the pending action",
            PendingCardNotOffered),
        Bind("no-pending-opportunity", TranscriptStepKind.Then,
            "no opportunity is pending", NoPendingOpportunity),
        Bind("combined-triggering-conditions", TranscriptStepKind.Then,
            @"the pending occurrence combines (?<first>[A-Za-z_]+) and (?<second>[A-Za-z_]+)",
            CombinedTriggeringConditions),
        Bind("card-action-availability", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+)'s action is (?<availability>available|unavailable)",
            CardActionAvailability),
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
        string[] allowed = [.. required, "modular sets", "decks"];
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

        IReadOnlyList<string>? playerDecks = row.TryGetValue("decks", out string? decks)
            ? [.. decks.Split(
                ',', StringSplitOptions.TrimEntries
                     | StringSplitOptions.RemoveEmptyEntries)]
            : null;

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
                    : null,
                PlayerDecks: playerDecks),
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

    private static void SetEmptyPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetPlayerHand(Seat(match, step), []));

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

    private static void SetAccelerationTokens(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneAccelerationTokens(
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

    private static void PlaceObligation(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Obligation, Seat(match, step))));

    private static void PlacePlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.PlayerDiscard, Seat(match, step))));

    private static void AttachIdentityUpgrade(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Upgrade, Seat(match, step))));

    private static void AttachCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CanonicalCoreScene scene = context.SceneRequired(step);
        Card card = scene.Find(SceneCard(match, step));
        var host = scene.Find(new SceneCard(
            match.Groups["host"].Value,
            Number(match, "hostCopy", step)));
        SceneDestination destination = card.Owner == World.Scenario
            ? new SceneDestination(SceneZone.Attachment, Host: host.ObjectId)
            : new SceneDestination(SceneZone.Upgrade, card.Owner, host.ObjectId);
        scene.Apply(new MoveSceneCard(
            SceneCard(match, step),
            destination));
    }

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

    private static void DiscardCardEffect(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Discard.Card(context.World, card, "behavioral transcript effect", context.Events);
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

    private static void RevealEncounterCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Area queue = context.World.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat));
        World.MoveToTop(card, queue);
        context.World.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, Round: 1, Number: 4,
            Subject: card.ObjectId, Seat: seat));
        context.Events.Clear();
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void AssignThreatAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Schedule(
            context.World,
            scheme,
            villain,
            Number(match, "count", step),
            ThreatCause.CardAbility,
            "behavioral transcript",
            Seat(match, step));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    private static void BeginThreatAssignment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Schedule(
            context.World,
            scheme,
            villain,
            Number(match, "count", step),
            ThreatCause.EnemyScheme,
            "behavioral transcript",
            Seat(match, step));
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void AnswerEncounterCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException(
                $"{step.Location}: no encounter-card decision is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: the pending encounter-card decision belongs to "
                + $"seat {asked.Player + 1}, not seat {seat + 1}");
        }

        int option = Number(match, "option", step);
        if (option < 1 || option > asked.Affordances.Count)
        {
            throw new TranscriptException(
                $"{step.Location}: option {option} is outside the pending decision's "
                + $"{asked.Affordances.Count} choices");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(asked.Affordances[option - 1].Id),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void OrderPendingPlayers(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException(
                $"{step.Location}: no encounter-card decision is pending");
        if (asked.Player != seat || asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending player order does not belong to seat {seat + 1}");
        }

        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException(
                $"{step.Location}: pending encounter-card decision asks for no order");
        TranscriptTable table = Table(step, "seat");
        int[] ordered = [.. table.Rows.Select(row =>
            context.World.Seats[TableNumber(row, "seat", step) - 1].IdentityCard.ObjectId)];
        if (ordered.Length < targets.Min
            || ordered.Length > targets.Max
            || ordered.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested player order is not legal for '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, ordered, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
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

    private static void EndPlayerPhase(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DrawToHandSize(context.World, context.Cards, context.Events);
        PhaseEnd.ReadyCards(context.World, context.Events);
        PhaseEnd.EndPlayerPhase(context.World, context.Events);
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

    private static void RemoveThreat(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.World.Agenda.Add(new PhaseStep(
            Steps.DealAttackDamage, Round: 1, Number: 4, Plan: true));
        _ = context.World.Agenda.Begin(context.World, context.Cards);
        _ = Threat.Remove(
            context.World,
            context.Cards,
            context.World.Abilities,
            scheme,
            Number(match, "count", step),
            "behavioral transcript",
            "Remove_Threat",
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

    private static void BeginBasicAttack(
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
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void BasicAttackAcceptingWithPayment(
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
        Card payment = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["payment"].Value,
            Number(match, "paymentCopy", step)));
        Card target = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["target"].Value,
            Number(match, "targetCopy", step)));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId, target.ObjectId);
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

    private static void AllyPowerAccepting(
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
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
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

    private static void ResolveVillainPhaseAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    private static void ResolveVillainPhaseAcceptingWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card payment = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId);
    }

    private static void ResolveVillainPhaseWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));

        FinishWithDefender(context, step, defender, acceptedLabel: null);
    }

    private static void ResolveVillainAttackWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));

        FinishWithDefender(context, step, defender, acceptedLabel: null);
    }

    private static void ResolveVillainAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgenda(context, step);
    }

    private static void ResolveVillainScheme(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Scheme, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgenda(context, step);
    }

    private static void ResolveVillainAttackWithDefenderAndOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));

        FinishWithDefender(context, step, defender, match.Groups["label"].Value);
    }

    private static void ResolveVillainAttackWithOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    private static void ResolveVillainAttackWithTwoOpportunities(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgendaAccepting(
            context, step,
            [match.Groups["first"].Value, match.Groups["second"].Value]);
    }

    private static void FinishWithDefender(
        TranscriptContext context, TranscriptStep step, Card defender, string? acceptedLabel)
    {

        bool defended = false;
        bool accepted = acceptedLabel is null;
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
            Affordance? opportunity = accepted
                ? null
                : asked.Affordances.SingleOrDefault(option =>
                    option.Label == acceptedLabel);
            if (opportunity is not null)
            {
                decision = Decision.Take(opportunity.Id);
                accepted = true;
            }
            else if (!defended && asked.Asking == Question.Defender)
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
        if (!accepted)
        {
            throw new TranscriptException(
                $"{step.Location}: the attack offered no '{acceptedLabel}' opportunity");
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

    private static void FinishAgendaAccepting(
        TranscriptContext context, TranscriptStep step, string acceptedLabel,
        int? payment = null, int? target = null)
    {
        bool accepted = false;
        bool targetChosen = target is null;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 answers");
            }

            Affordance? opportunity = accepted
                ? null
                : asked.Affordances.SingleOrDefault(option => option.Label == acceptedLabel);
            Affordance? targetOption = !accepted || targetChosen
                ? null
                : asked.Affordances.SingleOrDefault(option =>
                    option.Id == target || option.AnchorId == target);
            Decision decision = opportunity is not null
                ? payment is null
                    ? Decision.Take(opportunity.Id)
                    : Decision.Take(
                        opportunity.Id,
                        target is null ? [] : [target.Value],
                        [payment.Value])
                : targetOption is not null
                    ? Decision.Take(targetOption.Id)
                    : Decision.Decline;
            accepted |= opportunity is not null;
            targetChosen |= targetOption is not null;
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

        if (!accepted)
        {
            throw new TranscriptException(
                $"{step.Location}: the action offered no '{acceptedLabel}' opportunity");
        }
        if (!targetChosen)
        {
            throw new TranscriptException(
                $"{step.Location}: '{acceptedLabel}' offered no requested target {target}");
        }
    }

    private static void FinishAgendaAccepting(
        TranscriptContext context, TranscriptStep step, IReadOnlyList<string> acceptedLabels)
    {
        int next = 0;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 answers");
            }

            Affordance? opportunity = next == acceptedLabels.Count
                ? null
                : asked.Affordances.SingleOrDefault(option =>
                    option.Label == acceptedLabels[next]);
            Decision decision = opportunity is null
                ? Decision.Decline
                : Decision.Take(opportunity.Id);
            next += opportunity is null ? 0 : 1;
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

        if (next != acceptedLabels.Count)
        {
            throw new TranscriptException(
                $"{step.Location}: the attack offered no '{acceptedLabels[next]}' opportunity");
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

    private static void UpgradeEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Upgrade, Seat(match, step))));
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

    private static void InitiateActionWithDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [],
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    private static void InitiateIndexedActionWithVariable(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)],
            ordinal: PrintedOrdinal(match, step),
            values: new Dictionary<string, long>(StringComparer.Ordinal)
            {
                ["X"] = Number(match, "value", step),
            });
    }

    private static void InitiateIndexedActionWithoutPayment(
        TranscriptContext context, TranscriptStep step, Match match) =>
        InitiateAction(
            context, step, match, [], ordinal: PrintedOrdinal(match, step));

    private static int PrintedOrdinal(Match match, TranscriptStep step) =>
        match.Groups["ordinal"].Value switch
        {
            "first" => 0,
            "second" => 1,
            _ => throw new TranscriptException(
                $"{step.Location}: unknown printed action ordinal '{match.Groups["ordinal"].Value}'"),
        };

    private static void PlayCardWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance play = asked.Affordances.Single(candidate =>
            candidate.Verb == CardPlay.Verb && candidate.AnchorId == card.ObjectId);
        int[] targets = play.Targets switch
        {
            null => [],
            { Legal.Count: 1 } request => [request.Legal[0]],
            _ => throw new TranscriptException(
                $"{step.Location}: playing card {card.ObjectId} requires an explicit target"),
        };
        TranscriptTable table = Table(step, "card", "copy");
        int[] payment = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(play.Id, targets, payment));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    private static void RequestCardActions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        context.LastCardOptions = ((AbilityRunner)context.World.Abilities)
            .Actions(context.World, seat)
            .Select(action => action.Card)
            .ToHashSet();
    }

    private static void UseCardResourceAbility(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        var runner = (AbilityRunner)context.World.Abilities;
        if (!runner.ResourceAbilities(context.World, seat)
            .Any(candidate => candidate.Effect == card.ObjectId))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} has no available resource ability");
        }

        context.Events.Clear();
        string resources = runner.UseResource(
            context.World, seat, card.ObjectId, context.Events);
        context.LastResourceGeneration = (card.ObjectId, resources);
    }

    private static void RequestTurnCardActions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || game.Active != seat || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastCardOptions = asked.Affordances
            .Where(option => option.Verb is not Game.ChangeForm and not Game.EndPhaseVerb
                && option.Verb != CardPlay.Verb)
            .Select(option => option.AnchorId)
            .ToHashSet();
    }

    private static void InspectCoreScene(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        _ = context.World;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
    }

    private static void BeginMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.Game = Game.Begin(context.World, context.Cards, context.World.Abilities);
        SetPendingPrompt(context, context.Game.Pending);
        if (context.Game.Phase != GamePhase.Mulligan
            || context.Game.Active != Seat(match, step))
        {
            throw new TranscriptException(
                $"{step.Location}: setup did not reach the requested player's mulligan");
        }
    }

    private static void ResolveMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not resolving a mulligan");
        }

        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no mulligan prompt is pending");
        Affordance option = asked.Affordances.Single(candidate =>
            candidate.Verb == Game.ResolveMulligans);
        TranscriptTable table = Table(step, "card", "copy");
        int[] selected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id, selected, []));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    private static void KeepMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not resolving a mulligan");
        }

        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Decline);
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    private static void EndPlayerTurn(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerTurn || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Decline);
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    private static void RequestVoluntaryFormChange(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastAvailability = asked.Affordances.Any(option =>
            option.Verb == Game.ChangeForm);
    }

    private static void RequestCardPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (context.Game is null)
        {
            context.LastAvailability = CardPlay.Price(
                context.World, context.Cards, context.World.Seats[seat], card) is not null;
            return;
        }

        Game game = context.Game;
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastAvailability = asked.Affordances.Any(option =>
            option.Verb == CardPlay.Verb && option.AnchorId == card.ObjectId);
    }

    private static void TakeVoluntaryFormChange(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seatIndex = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seatIndex)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seatIndex + 1} is not taking their turn");
        }

        Affordance option = asked.Affordances.Single(candidate =>
            candidate.Verb == Game.ChangeForm);
        Seat seat = context.World.Seats[seatIndex];
        string from = FormName(Forms.Of(context.World, seat, context.Cards));
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
        string to = FormName(Forms.Of(context.World, seat, context.Cards));
        context.LastFormChange = (seatIndex, from, to);
    }

    private static void ChooseSetupCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerSetup)
        {
            throw new TranscriptException(
                $"{step.Location}: no player Setup ability is resolving");
        }

        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no setup prompt is pending");
        Card selected = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance option = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == selected.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {selected.ObjectId} is not offered by '{asked.Label}'");
        int[] unshuffled =
        [
            .. context.World.Seats[seat].Deck.Cards
                .Where(card => card.ObjectId != selected.ObjectId)
                .Select(card => card.ObjectId),
        ];
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
        context.LastSetupDeckShuffle = (
            seat,
            unshuffled,
            [.. context.World.Seats[seat].Deck.Cards.Select(card => card.ObjectId)]);
    }

    private static void InitiateAction(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        IReadOnlyList<int> payments,
        IReadOnlyList<int>? chosen = null,
        int? ordinal = null,
        IReadOnlyDictionary<string, long>? values = null)
    {
        int seat = Seat(match, step);
        Card source = context.SceneRequired(step).Find(SceneCard(match, step));
        var runner = (AbilityRunner)context.World.Abilities;
        var actions = runner.Actions(context.World, seat)
            .Where(candidate => candidate.Card == source.ObjectId);
        var action = ordinal is null
            ? actions.Single()
            : actions.Single(candidate => candidate.Ordinal == ordinal.Value);
        CostOption? price = runner.Describe(context.World, action).CostOptions.SingleOrDefault();
        IReadOnlyList<ResourceAllocation>? allocations = price is null
            ? null
            : values is not null
                ? null
                : ResourcePayment.Allocate(price, payments)
                ?? throw new TranscriptException(
                    $"{step.Location}: the selected cards cannot be allocated to the action cost");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.Events.AddRange(runner.Act(
            context.World, action, payments, chosen ?? [], values, allocations));
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

    private static void OrderPendingCards(
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

        if (asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending order has {asked.Affordances.Count} affordances");
        }
        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException($"{step.Location}: pending action asks for no order");
        TranscriptTable table = Table(step, "card", "copy");
        int[] ordered = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        if (ordered.Length < targets.Min
            || ordered.Length > targets.Max
            || ordered.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested order is not legal for '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, ordered, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void ChoosePendingCards(
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

        if (asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending choice has {asked.Affordances.Count} affordances");
        }
        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException($"{step.Location}: pending action asks for no targets");
        TranscriptTable table = Table(step, "card", "copy");
        int[] selected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        if (selected.Length < targets.Min
            || selected.Length > targets.Max
            || selected.Distinct().Count() != selected.Length
            || selected.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested targets are not legal for '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, selected, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void ChoosePendingCardAndDiscard(
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
        TranscriptTable table = Table(step, "card", "copy");
        int[] discarded = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, discarded, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void ChoosePendingCardWithPayment(
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
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == target.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {target.ObjectId} is not offered by '{asked.Label}'");
        TranscriptTable table = Table(step, "card", "copy");
        int[] payments = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, [], payments),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void AcceptPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending opportunity asks seat {asked.Player + 1}, "
                + $"not seat {seat + 1}");
        }

        string label = match.Groups["label"].Value;
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.Label == label)
            ?? throw new TranscriptException(
                $"{step.Location}: '{label}' is not offered by '{asked.Label}'");
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

    private static void AcceptCardPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending opportunity asks seat {asked.Player + 1}, "
                + $"not seat {seat + 1}");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == card.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} is not offered by '{asked.Label}'");
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

    private static void DeclinePendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat || !asked.Cancellable)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} cannot decline '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Decline,
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    private static void PendingOpportunityOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        string label = match.Groups["label"].Value;
        if (asked.Player != seat || !asked.Affordances.Any(candidate =>
                candidate.Label == label))
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not offered '{label}'");
        }
    }

    private static void PendingWindowPass(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.PendingPrompt is not { Cancellable: true } asked
            || asked.Player != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} cannot pass the pending window");
        }
    }

    private static void NoPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.PendingPrompt is not null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: '{context.PendingPrompt.Label}' is still pending");
        }
    }

    private static void PendingOptionAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no decision is pending");
        int option = Number(match, "option", step) - 1;
        bool offered = asked.Affordances.Any(candidate => candidate.Id == option);
        bool expected = match.Groups["availability"].Value == "offered";
        if (offered != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: option {option + 1} was "
                + (offered ? "offered" : "not offered"));
        }
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

    private static void MulliganOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan
            || game.Active != seat
            || game.Pending is not { Cancellable: false } prompt
            || !prompt.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} was not offered its mulligan");
        }
    }

    private static void ActivePlayer(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerTurn
            || game.Active != seat
            || game.Pending is not { Player: var player }
            || player != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} is not the active player");
        }
    }

    private static void EndPhaseDiscardOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.EndPhase
            || game.Active != seat
            || game.Pending is not { Player: var player } prompt
            || player != seat
            || !prompt.Affordances.Any(option => option.Verb == Game.EndPhaseVerb))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not offered the end-of-player-phase discard");
        }
    }

    private static void PendingOrderCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || targets.Legal.Count != count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} cards");
        }
    }

    private static void SimultaneousEffectChoiceCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Asking: Question.Order } prompt
            || prompt.Player != seat
            || prompt.Affordances.Count != count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} "
                + "simultaneous effects");
        }
    }

    private static void PendingChoiceCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || targets.Legal.Count < count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to choose {count} cards");
        }
    }

    private static void PendingPlayerOrderCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || targets.Legal.Count != count
            || targets.Legal.Any(id => context.World.Seats.All(
                candidate => candidate.IdentityCard.ObjectId != id)))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} players");
        }
    }

    private static void CardInPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Hand))
        {
            throw new TranscriptException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s hand; "
                + $"was {card.Area.Type}");
        }
    }

    private static void CardInPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Deck))
        {
            throw new TranscriptException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s deck; "
                + $"was {card.Area.Type}");
        }
    }

    private static void SetupDeckShuffled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.LastSetupDeckShuffle is not { } shuffle
            || shuffle.Seat != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no setup shuffle was observed for seat {seat + 1}");
        }
        if (shuffle.Unshuffled.SequenceEqual(shuffle.After))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1}'s deck retained its unshuffled order");
        }
    }

    private static void PlayerDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Deck.Cards.Count,
            "cards in the player deck", step);

    private static void PlayerDeckTopFace(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        IReadOnlyList<Card> deck = context.World.Seats[seat].Deck.Cards;
        Card? top = deck.Count == 0 ? null : deck[^1];
        string expected = match.Groups["face"].Value;
        if (top is null || !string.Equals(top.FaceId, expected, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} on top of seat {seat + 1}'s deck; "
                + $"was {top?.FaceId ?? "<empty>"}");
        }
    }

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

    private static void PlayerCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step), context.World.Players,
            "players in the game", step);

    private static void EncounterDeckFaceCounts(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        TranscriptTable table = Table(step, "card", "count");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string face = row["card"];
            int expected = TableNumber(row, "count", step);
            int actual = context.World.AreaOf(DeckType.EncounterDeck).Cards.Count(
                card => card.FaceId == face);
            Equal(expected, actual, $"copies of {face} in the encounter deck", step);
        }
    }

    private static void OwnedPlayerCardCounts(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "card", "count");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string face = row["card"];
            int expected = TableNumber(row, "count", step);
            int actual = context.World.Cards.Count(card =>
                card.Owner == seat && card.Faces.Contains(face, StringComparer.Ordinal));
            Equal(expected, actual, $"owned copies of {face}", step);
        }
    }

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

    private static void GeneratedResources(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string expected = match.Groups["resources"].Value;
        if (context.LastResourceGeneration is not { } actual
            || actual.Card != card.ObjectId
            || !string.Equals(actual.Resources, expected, StringComparison.Ordinal))
        {
            string observed = context.LastResourceGeneration is { } generation
                ? $"card {generation.Card} generated {generation.Resources}"
                : "no resource ability was used";
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to generate {expected}; "
                + observed);
        }
    }

    private static void LastAttackDefense(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (context.World.FinishedAttack is not { } attack
            || attack.Defender != card.ObjectId
            || attack.BasicDefense)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} did not defend the last attack "
                + "with a defense-labeled ability");
        }
    }

    private static void LastAttackUndefended(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.FinishedAttack is not { Defender: < 0 })
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: the last attack was defended");
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

    private static void AttackEnded(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Attack is not null || context.World.Activation is not null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected the attack and its activation to have ended");
        }
    }

    private static void CombinedTriggeringConditions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        var occurrence = context.World.Agenda.Occurrence
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending occurrence");
        string first = match.Groups["first"].Value;
        string second = match.Groups["second"].Value;
        if (!occurrence.Conditions.Contains(first, StringComparer.Ordinal)
            || !occurrence.Conditions.Contains(second, StringComparer.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected one occurrence containing {first} and {second}; "
                + $"was {string.Join(", ", occurrence.Conditions)}");
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

    private static void FacedownDroneCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int actual = FacedownDrones.EngagedWith(
            context.World, Seat(match, step)).Count;
        int expected = Number(match, "count", step);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} facedown Drone minions; was {actual}");
        }
    }

    private static void AllyControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.AlliesArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} as an ally controlled by "
                + $"seat {seat + 1}; was {card.Area}");
        }
    }

    private static void SupportControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.SupportsArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} as a support controlled by "
                + $"seat {seat + 1}; was {card.Area}");
        }
    }

    private static void CardDamage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            checked((int)context.SceneRequired(step).Find(SceneCard(match, step)).Damage),
            "damage on the card",
            step);

    private static void ModifiedCardStatistic(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string field = match.Groups["field"].Value switch
        {
            "ATK" => "attack",
            "DEF" => "defense",
            "HS" => "hand_size",
            "REC" => "recover",
            "THW" => "thwart",
            _ => throw new InvalidOperationException("unsupported printed statistic"),
        };
        int expected = Number(match, "count", step);
        int actual = checked((int)StateFields.Modified(
            context.World, card, field, context.Cards, context.World.Players));
        if (expected != actual)
        {
            string effects = string.Join(", ", context.World.Effects.Active()
                .Where(effect => effect.Kind == field)
                .Select(effect => $"{effect.Kind}:{effect.Amount}:{effect.Card}:{effect.Affects}"));
            string upgrades = string.Join(", ", context.World.Cards
                .Where(candidate => context.Cards.Kind(candidate.FaceId) == CardKind.Upgrade
                    && DeckTypes.IsInPlay(candidate.Area.Type))
                .Select(candidate => $"{candidate.FaceId}:{candidate.FaceUp}:"
                    + $"{candidate.Area.Type}:p{candidate.Area.PlayArea.Player}:"
                    + $"h{candidate.Area.Host}:o{candidate.Owner}"));
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} modified "
                + $"{match.Groups["field"].Value}; was {actual}; effects: [{effects}]; "
                + $"upgrades: [{upgrades}]");
        }
    }

    private static void PlayerPlayAreaRemoved(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.GameAreaOf(PlayArea.Of(seat)) is not null
            || context.World.Cards.Any(card => card.Area.PlayArea == PlayArea.Of(seat)))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1}'s play area still exists");
        }
    }

    private static void CardRemainingHitPoints(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        long actual = Math.Max(
            0,
            Damage.Health(context.World, context.Cards, card) - card.Damage);
        Equal(Number(match, "count", step), checked((int)actual),
            "remaining hit points", step);
    }

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

    private static void CardActionAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        if (context.LastCardOptions is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no card-action query has been made");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = match.Groups["availability"].Value == "available";
        bool actual = context.LastCardOptions.Contains(card.ObjectId);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId}'s action to be "
                + match.Groups["availability"].Value);
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

    private static void CardPlayResult(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        BasicRecoveryResult(context, step, match);
    }

    private static void ModifiedCardCost(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        long actual = CardPlay.CostOf(
            context.World,
            context.Cards,
            context.World.Seats[Seat(match, step)],
            card).Amount;
        Equal(
            Number(match, "count", step),
            checked((int)actual),
            "modified resource cost",
            step);
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

    private static void CardInVillainDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.VillainDeck)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in the villain deck; "
                + $"was {card.Area}");
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

    private static void CardInEncounterDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.EncounterDeck)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in the encounter deck; "
                + $"was {card.Area}");
        }
    }

    private static void CardInSeatNemesis(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Nemesis))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s "
                + $"set-aside nemesis pile; was {card.Area}");
        }
    }

    private static void CardInPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in play; was {card.Area.Type}");
        }
    }

    private static void CardOutOfPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} out of play; was {card.Area.Type}");
        }
    }

    private static void CardInPlayerPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Of(Seat(match, step)), expected: true);

    private static void CardNotInPlayerPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Of(Seat(match, step)), expected: false);

    private static void CardInVillainPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Villains, expected: true);

    private static void CardNotInVillainPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Villains, expected: false);

    private static void CardPlayArea(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        PlayArea playArea,
        bool expected)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool actual = card.Area.PlayArea == playArea;
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} "
                + (expected ? $"in play area {playArea}" : $"outside play area {playArea}")
                + $"; was {card.Area.PlayArea}");
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

    private static void CardDiscardAfterEvent(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int card = context.SceneRequired(step).Find(SceneCard(match, step)).ObjectId;
        // Reveal keeps a treachery's event verb while moving it from the
        // resolving area to the encounter discard pile, so use the card's
        // final landing rather than the engine's operation spelling.
        int discard = context.Events.FindLastIndex(gameEvent => EventLands(gameEvent, card));
        int prior = context.Events.FindLastIndex(gameEvent =>
            gameEvent.Verb == match.Groups["verb"].Value);
        if (prior < 0 || discard <= prior)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card} discarded after "
                + $"{match.Groups["verb"].Value}");
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

    private static void IdentityFaceOutOfPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card identity = context.World.Seats[Seat(match, step)].IdentityCard;
        string face = match.Groups["face"].Value;
        if (!identity.Faces.Contains(face, StringComparer.Ordinal)
            || string.Equals(identity.FaceId, face, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: identity face {face} is not its out-of-play side");
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

    private static void CardOwned(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Equal(Seat(match, step), card.Owner, "card owner", step);
    }

    private static void CardControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!DeckTypes.IsInPlay(card.Area.Type)
            || !card.Area.PlayArea.IsPlayers)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} is not a controlled in-play card");
        }

        Equal(Seat(match, step), card.Area.PlayArea.Player, "card controller", step);
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

    private static void CardAttachedToCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CanonicalCoreScene scene = context.SceneRequired(step);
        Card card = scene.Find(SceneCard(match, step));
        Card host = scene.Find(new SceneCard(
            match.Groups["host"].Value,
            Number(match, "hostCopy", step)));
        if (card.Area.Type != DeckType.UpgradesArea
            || card.Area.Host != host.ObjectId)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} attached to "
                + $"card {host.ObjectId}; was {card.Area}");
        }
    }

    private static void FacedownCardAttachedToCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CardAttachedToCard(context, step, match);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected attached card {card.ObjectId} facedown");
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

    private static void CardTrait(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string trait = match.Groups["trait"].Value.Replace(' ', '_');
        if (!Traits.Has(context.World, card, trait, context.Cards))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to have trait {trait}");
        }
    }

    private static void CardDoesNotHaveTrait(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string trait = match.Groups["trait"].Value.Replace(' ', '_');
        if (Traits.Has(context.World, card, trait, context.Cards))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} not to have trait {trait}");
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

    private static void PendingCardNotOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending setup prompt");
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (asked.Affordances.Any(offer => offer.AnchorId == card.ObjectId))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} was offered by '{asked.Label}'");
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
