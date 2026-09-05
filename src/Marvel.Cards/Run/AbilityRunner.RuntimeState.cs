using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private enum ResolutionOutcome
    {
        None,
        Partial,
        Full,
    }

    /// <summary>What one ability is resolving against.</summary>
    /// <param name="World">The board.</param>
    /// <param name="Source">The card whose text this is.</param>
    /// <param name="Occurrence">What it is timed to.</param>
    /// <param name="InitialPlayer">The seat resolving its first structural frame.</param>
    /// <param name="Events">Where to record what it did.</param>
    /// <param name="Abilities">
    /// The runner itself, for the rules that run more cards. A main scheme this
    /// ability completes advances, and <c>rr:villain-defeat</c> resolves the
    /// new stage's own "When Revealed" — so an ability can reach back into the
    /// interpreter that is running it.
    /// </param>
    private sealed record Cast(
        World World, Card Source, Occurrence Occurrence, int InitialPlayer,
        List<GameEvent> Events,
        ICardAbilities Abilities)
    {
        /// <summary>The seat whose perspective the current structural frame uses.</summary>
        public int Player { get; private set; } = InitialPlayer;

        /// <summary>The resolver to restore after leaving an each-player frame.</summary>
        public int AbilityPlayer { get; init; } = InitialPlayer;

        /// <summary>A trace-local player area after a projected card move.</summary>
        public int? ProjectedPlayAreaPlayer { get; init; }

        public void RestorePlayer(int player) => Player = player;

        /// <summary>The trigger string this ability's events carry.</summary>
        /// <remarks>
        /// A constant ability resolves against no occurrence and so has none.
        /// Nothing reachable from <c>Grants</c> asks — every use of this is in
        /// a verb, and <c>Grants</c> refuses every verb by name — so the guard
        /// belongs there rather than being restated here, where only one of the
        /// two could stay right after an edit.
        /// </remarks>
        public string Trigger => string.IsNullOrEmpty(EventTrigger)
            ? Occurrence.Conditions[0]
            : EventTrigger;

        /// <summary>Event-stream provenance carried across a scheduled power.</summary>
        public string? EventTrigger { get; init; }

        /// <summary>
        /// What the actions in this ability actually did — the <c>result.*</c>
        /// namespace.
        /// </summary>
        /// <remarks>
        /// Scoped to one resolution of one ability, because that is the scope
        /// the cards use: "if no damage was healed <b>this way</b>" is about
        /// this sentence and not about the game.
        /// </remarks>
        public Dictionary<string, long> Results { get; } = new(StringComparer.Ordinal);

        /// <summary>Non-numeric keywords gained during this resolution scope.</summary>
        /// <remarks>
        /// A reveal shares this set across each of the card's When Revealed
        /// abilities. Other entry points keep the per-cast default.
        /// </remarks>
        public HashSet<string> GainedKeywords { get; init; } =
            new(StringComparer.Ordinal);

        /// <summary>The resource letters generated to pay for this event.</summary>
        public string Payment { get; private set; } = string.Empty;

        public void PaidWith(string resources) => Payment = resources;

        /// <summary>Whether initiation payment may change outcome-relevant state.</summary>
        public bool PaymentMayMutate { get; private set; }

        public AbilityCost? PaymentCost { get; private set; }

        public void SetPaymentMayMutate(bool value, AbilityCost? cost = null)
        {
            PaymentMayMutate = value;
            PaymentCost = cost;
        }

        public bool CheckingInitiation { get; private set; }

        public void SetCheckingInitiation(bool value) => CheckingInitiation = value;

        public bool FilteringContinuationOption { get; private set; }

        public void SetFilteringContinuationOption(bool value) =>
            FilteringContinuationOption = value;

        public IReadOnlyList<AbilityEffect> PriorSteps { get; private set; } = [];

        public void SetPriorSteps(IReadOnlyList<AbilityEffect> steps) =>
            PriorSteps = steps;

        /// <summary>Whether an earlier sequence step may have changed the board.</summary>
        public bool PriorStepMayMutate { get; private set; }

        public void SetPriorStepMayMutate(bool value) => PriorStepMayMutate = value;

        /// <summary>Seats whose form an earlier sequence step may change.</summary>
        public ulong PriorFormsMayChange { get; private set; }

        public void SetPriorFormsMayChange(ulong value) =>
            PriorFormsMayChange = value;

        /// <summary>Whether an earlier step may bind a selected game element.</summary>
        public bool PriorBindingMayChange { get; private set; }

        public void SetPriorBindingMayChange(bool value) =>
            PriorBindingMayChange = value;

        /// <summary>Cards an earlier unanswered selector may bind.</summary>
        public IReadOnlyList<Card> PriorBindingCandidates { get; private set; } = [];

        public void SetPriorBindingCandidates(IReadOnlyList<Card> value) =>
            PriorBindingCandidates = value;

        /// <summary>Whether a reachable binding path supplies no selected card.</summary>
        public bool PriorBindingMayBeEmpty { get; private set; }

        public void SetPriorBindingMayBeEmpty(bool value) =>
            PriorBindingMayBeEmpty = value;

        /// <summary>Cards discarded earlier in this resolution, in order.</summary>
        public List<Card> Discarded { get; } = [];

        /// <summary>The game element bound by the current alteration frame.</summary>
        public Card? Altered { get; private set; }

        public void BindAlteration(Card card) => Altered = card;

        /// <summary>Whether this ability has stopped to ask a question.</summary>
        public bool Suspended { get; private set; }

        /// <summary>Stops the ability here — <c>rr:choose-option</c>.</summary>
        public void Suspend() => Suspended = true;

        /// <summary>The scheduled activations this sentence must wait for.</summary>
        public List<int> ActivationIds { get; } = [];

        public void WaitFor(IEnumerable<int> ids) => ActivationIds.AddRange(ids);

        /// <summary>Whether text after the current node still has to resolve.</summary>
        public bool HasContinuation { get; private set; }

        public void SetContinuation(bool value) => HasContinuation = value;

        /// <summary>Which step of the top-level sequence is running.</summary>
        public int Position { get; private set; }

        /// <summary>Records which step of the sequence this is.</summary>
        /// <param name="step">Its index.</param>
        public void At(int step) => Position = step;

        /// <summary>The exact authored ability and structural route being resolved.</summary>
        public int AbilityOrdinal { get; private set; } = -1;

        public string AbilityFace { get; private set; } = Source.FaceId;

        public List<string> AbilityPath { get; } = [];

        public void RestoreAbility(
            int ordinal, IReadOnlyList<string> path, string? face = null)
        {
            AbilityOrdinal = ordinal;
            if (face is not null)
            {
                AbilityFace = face;
            }
            AbilityPath.Clear();
            AbilityPath.AddRange(path);
        }

        private PendingAbility? resolutionAbility;

        /// <summary>The exact ability whose status this cast updates.</summary>
        public PendingAbility? ResolutionAbility => resolutionAbility;

        /// <summary>Begin tracking the exact ability whose tree this cast runs.</summary>
        public void TrackResolution(int ordinal)
        {
            if (Tier is not { } tier)
            {
                return;
            }
            resolutionAbility = new PendingAbility(Source.ObjectId, tier, Player, ordinal);
            Occurrence.Begin(resolutionAbility.Value);
        }

        /// <summary>Record that one effect in the tracked ability applied.</summary>
        public void ResolveEffect()
        {
            if (resolutionAbility is { } ability)
            {
                Occurrence.Resolve(ability);
            }
        }

        /// <summary>Finish a tracked ability unless it remains suspended.</summary>
        public void CompleteResolution()
        {
            if (!Suspended && resolutionAbility is { } ability)
            {
                Occurrence.Complete(ability);
            }
        }

        public void SetAbilityPath(IEnumerable<string> path)
        {
            var copy = path.ToList();
            AbilityPath.Clear();
            AbilityPath.AddRange(copy);
        }

        public void CompletePendingDependency(ResolutionOutcome outcome)
        {
            int pending = AbilityPath.FindLastIndex(frame =>
                frame.EndsWith(":Pending", StringComparison.Ordinal));
            if (pending >= 0)
            {
                AbilityPath[pending] = AbilityPath[pending][..^"Pending".Length]
                    + outcome;
            }
        }

        public bool HasPendingDependency => AbilityPath.Any(frame =>
            frame.EndsWith(":Pending", StringComparison.Ordinal));

        private AbilityCardReference? chosenBinding;
        private AbilityCardReference? playerSelectionBinding;
        private int sourceIncarnation = Source.Incarnation;

        /// <summary>The card the player picked, once they have.</summary>
        public Card? Chosen => CurrentCard(chosenBinding, "chosen");

        /// <summary>The outer card selection used by chosen-player references.</summary>
        public Card? PlayerSelection =>
            CurrentCard(playerSelectionBinding, "player selection");

        public int SourceBindingIncarnation => sourceIncarnation;

        public void RestoreSourceIncarnation(int incarnation) =>
            sourceIncarnation = incarnation;

        /// <summary>Records the card a <c>chooseCard</c> was answered with.</summary>
        /// <param name="card">What they picked.</param>
        public void Choose(Card? card)
        {
            chosenBinding = Bind(card);
        }

        /// <summary>Records a player answer in both chosen namespaces.</summary>
        public void ChooseSelection(Card? card)
        {
            var binding = Bind(card);
            chosenBinding = binding;
            playerSelectionBinding = binding;
        }

        public AbilityCardReference? CaptureChosen() => chosenBinding;

        public AbilityCardReference? CapturePlayerSelection() => playerSelectionBinding;

        public AbilityCardReference? CaptureCurrentSelection() =>
            playerSelectionBinding ?? chosenBinding;

        public void RestoreChosen(AbilityCardReference? binding) => chosenBinding = binding;

        public void RestorePlayerSelection(AbilityCardReference? binding) =>
            playerSelectionBinding = binding;

        public void RestorePersistedSelection(
            Card card, int area, int incarnation, bool overwriteChosen)
        {
            var binding = new AbilityCardReference(card, area, incarnation);
            playerSelectionBinding = binding;
            if (overwriteChosen || chosenBinding is null)
            {
                chosenBinding = binding;
            }
        }

        public bool SourceBindingIsCurrent(Card card) =>
            Source.ObjectId == card.ObjectId
            && sourceIncarnation == card.Incarnation;

        private static AbilityCardReference? Bind(Card? card) => card is null
            ? null
            : new AbilityCardReference(card, card.Area.Id, card.Incarnation);

        private Card? CurrentCard(AbilityCardReference? binding, string name) => binding?.Resolve(Source, name);

        public AbilityQueryContext QueryContext() => new(
            World, Source, Occurrence, Player, sourceIncarnation,
            chosenBinding, playerSelectionBinding, Altered, [.. PowerTargets]);

        /// <summary>
        /// Which of the card's abilities is running, or null.
        /// </summary>
        /// <remarks>
        /// Only a suspended choice reads it, and it reads it to find its way
        /// back: a card with a choice in two of its abilities cannot be resumed
        /// from the card and a position alone. See <c>Choice</c>.
        /// </remarks>
        public AbilityType? Tier { get; init; }

        /// <summary>Whether this Special is the final step in its parent sequence.</summary>
        public bool FinalStep { get; init; }

        public bool EachPlayerFrame { get; init; }

        public bool FinalPlayer { get; init; }

        /// <summary>The labelled player power whose occurrence is resolving.</summary>
        public string? Power { get; init; }

        /// <summary>Every game element selected for this labelled power.</summary>
        public IReadOnlyList<Card> PowerTargets { get; set; } = [];

        public void SetPowerTargets(IReadOnlyList<Card> targets) =>
            PowerTargets = targets;

        /// <summary>The card attributed as performer of the labelled power.</summary>
        public Card? PowerActor { get; init; }

        /// <summary>The performer attributed by an ability-envelope label.</summary>
        public Card? AbilityActor { get; set; }

        /// <summary>Whether this fresh cast passed envelope legality before resolution.</summary>
        public bool LabelsPreflighted { get; set; }

        private const string CrisisIgnoringThwartPrefix =
            "__preflight.crisisIgnoringThwart.";

        private HashSet<AbilityEffect> CrisisIgnoringThwarts { get; } = new(ReferenceEqualityComparer.Instance);

        private HashSet<int> PersistedCrisisIgnoringThwarts { get; } = [];

        /// <summary>Persists a pre-payment exception to this power's target limit.</summary>
        public void ValidateCrisisIgnoringThwart(AbilityEffect node) =>
            CrisisIgnoringThwarts.Add(node);

        /// <summary>Whether initiation established this scoped target exception.</summary>
        public bool CrisisIgnoringThwartWasValidated(AbilityEffect node, int ordinal) =>
            CrisisIgnoringThwarts.Contains(node)
            || PersistedCrisisIgnoringThwarts.Contains(ordinal);

        /// <summary>Writes validated power addresses into continuation metadata.</summary>
        public void PersistCrisisIgnoringThwarts(
            CompiledCardAbility ability, Dictionary<string, long> results)
        {
            var nodes = PowerNodes(ability.Effect, BasicPowers.ThwartVerb).ToList();
            for (int ordinal = 0; ordinal < nodes.Count; ordinal++)
            {
                if (CrisisIgnoringThwarts.Contains(nodes[ordinal])
                    || PersistedCrisisIgnoringThwarts.Contains(ordinal))
                {
                    results[$"{CrisisIgnoringThwartPrefix}{ordinal}"] = 1;
                }
            }
        }

        /// <summary>Consumes engine-owned target metadata without exposing a result.</summary>
        public bool RestoreCrisisIgnoringThwart(string name, long value)
        {
            if (!name.StartsWith(CrisisIgnoringThwartPrefix, StringComparison.Ordinal))
            {
                return false;
            }
            string encoded = name[CrisisIgnoringThwartPrefix.Length..];
            if (value != 1 || !int.TryParse(
                    encoded,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int ordinal)
                || ordinal < 0)
            {
                throw new RulesNotImplementedException(
                    $"'{Source.FaceId}' has invalid persisted thwart target metadata");
            }
            PersistedCrisisIgnoringThwarts.Add(ordinal);
            return true;
        }

        /// <summary>A numeric result carried into this labelled power.</summary>
        public long PowerAmount { get; init; } = -1;

        /// <summary>The outer threat assignment this interrupt can prevent.</summary>
        public ThreatPlacement? ImminentThreat { get; init; }

        /// <summary>Targets attacked by damage nodes, for one deferred retaliation each.</summary>
        public List<Card> Attacked { get; } = [];

        /// <summary>How much damage is about to be dealt — <c>rr:damage.step.1</c>.</summary>
        public long Incoming { get; init; }

        /// <summary>How much is left after this ability, defaulting to all of it.</summary>
        public long Remaining { get; private set; } = -1;

        /// <summary>Replaces the damage with this much.</summary>
        /// <param name="amount">What is left.</param>
        public void Replace(long amount) => Remaining = amount;
    }

}
