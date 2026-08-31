using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>How a continuous effect got into the game, and therefore how it leaves.</summary>
public enum EffectSource
{
    /// <summary>
    /// A constant ability. Active as soon as its card enters play and while it
    /// remains in play — <c>rr:ability</c>, "Constant Abilities".
    /// </summary>
    ConstantAbility,

    /// <summary>
    /// A lasting effect, which persists past the ability that created it for a
    /// stated duration — <c>rr:lasting-effects.1</c>.
    /// </summary>
    LastingEffect,

    /// <summary>
    /// A delayed effect, which resolves once when its timing point or condition
    /// occurs — <c>rr:delayed-effect.1</c>.
    /// </summary>
    DelayedEffect,
}

/// <summary>
/// One entry in the continuous effect list.
/// </summary>
/// <remarks>
/// <para>
/// <b>Data, not a closure.</b> A lasting effect outlives the card that made it
/// and has to survive a save, so an entry has to be something that can be
/// written down. Anything holding a delegate could not be. What an entry
/// <i>does</i> is decided by reading <see cref="Kind"/> and
/// <see cref="Amount"/>, which is a small price for a game that can be put down
/// and picked up.
/// </para>
/// </remarks>
/// <param name="Source">How it got here, and therefore how it leaves.</param>
/// <param name="Kind">What it does — a stat name for a modifier, an ability id otherwise.</param>
/// <param name="Amount">Its magnitude, where it has one.</param>
/// <param name="Card">
/// The card whose text created it. For a constant ability this is the card that
/// must stay in play; for a lasting effect it is provenance only, and may name a
/// card that has already gone to the discard.
/// </param>
/// <param name="Affects">The object id this applies to, or <c>null</c> for a board-wide effect.</param>
/// <param name="Scope">
/// A live affected-set rule, or empty when <paramref name="Affects"/> names
/// the affected object directly. Kept as data so the rule can be re-evaluated
/// after a save and when another card enters play.
/// </param>
/// <param name="Lasts">
/// How long, as the card states it. <see cref="Duration.WhileInPlay"/> for a
/// constant ability, which states no duration of its own.
/// </param>
public sealed record ContinuousEffect(
    EffectSource Source,
    string Kind,
    long Amount = 0,
    int? Card = null,
    int? Affects = null,
    Duration? Lasts = null,
    string Scope = "")
{
    /// <summary>A live set containing the characters one player controls.</summary>
    public const string CharactersControlledBy = "charactersControlledBy";

    /// <summary>
    /// Always <see cref="TimingPriority.Continuous"/>.
    /// </summary>
    /// <remarks>
    /// All three sources share one tier, and the rules say so separately for
    /// each: <c>rr:ability.step.1</c> lists them together,
    /// <c>rr:delayed-effect.1.1</c> gives delayed effects "the same timing
    /// priority as constant effects", and <c>rr:lasting-effects.2</c> says a
    /// lasting effect "is treated as if it was a constant ability and has the
    /// same timing priority".
    /// </remarks>
    public static TimingPriority Priority => TimingPriority.Continuous;

    /// <summary>Whether this effect modifies the named card right now.</summary>
    public bool AppliesTo(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (Scope.Length == 0)
        {
            return Affects == card.ObjectId;
        }

        if (!string.Equals(Scope, CharactersControlledBy, StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"continuous-effect scope '{Scope}' is not implemented");
        }

        if (Affects is not int identity
            || identity < 0
            || identity >= world.Cards.Count)
        {
            return false;
        }

        var player = world.Seats.FirstOrDefault(seat => seat.IdentityCard.ObjectId == identity);
        return player is not null
            && (card.ObjectId == identity
                || (world.Facts.Kind(card.FaceId) == CardKind.Ally
                    && card.Area.Type == DeckType.AlliesArea
                    && card.Area.PlayArea == PlayArea.Of(player.Index)));
    }
}

/// <summary>
/// The list of everything continuously in force, walked whenever the game state
/// changes.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:modifiers</c> opens by describing this: "The game constantly checks and
/// (if necessary) updates the count of any variable quantity that is being
/// modified." <c>rr:lasting-effects.3</c> says the same of lasting effects —
/// they "update whenever the game state updates". So the loop is the rule, not
/// an implementation choice, and <see cref="Active"/> is meant to be cheap and
/// called often rather than cached into the board.
/// </para>
/// <para>
/// <b>Registration, and two ways out.</b> An entry is registered by whatever
/// created it and can be disposed by whoever holds the registration. That is the
/// only way a lasting or delayed effect can leave, because there is nothing to
/// derive it from: the event that created it is in the discard pile and the
/// board no longer records that it was ever played.
/// </para>
/// <para>
/// A constant ability is different, and deliberately so. <c>rr:ability</c> says
/// it "becomes active as soon as its card enters play and remains active while
/// the card is in play" — so whether it is in force is a <i>function of the
/// board</i>, and <see cref="Active"/> derives it rather than trusting somebody
/// to have disposed the registration. A forgotten deregistration would be a
/// ghost: an ally's +1 ATK still being counted from the discard pile, on a board
/// that looks entirely normal. The rules make that unnecessary to risk.
/// </para>
/// <para>
/// <c>rr:lasting-effects.4</c> is why <see cref="Active"/> takes the world every
/// time instead of resolving affected cards at registration: "If a card enters
/// play after the creation of a lasting effect, it is still affected by that
/// lasting effect." An entry names a condition, and the condition is re-read.
/// </para>
/// </remarks>
public sealed class ContinuousEffects(World world)
{
    private readonly List<Entry> entries = [];
    private readonly HashSet<Entry> suppressed = [];
    private readonly List<ContinuousEffect> suppressedConstants = [];
    private readonly HashSet<int> departing = [];
    private readonly HashSet<int> healthDefeatPending = [];

    // While constants are settling, a nested read sees the previous complete
    // pass. See `Constant` -- this is iteration state, not cached game state.
    private bool deriving;
    private IReadOnlyList<ContinuousEffect> assumedConstants = [];

    /// <summary>Everything registered, in force or not.</summary>
    /// <remarks>
    /// For a save, and for a test that wants to see a stale entry rather than
    /// have it filtered away. <see cref="Active"/> is what the game reads.
    /// <para>
    /// A constant ability is <b>not</b> here, because nothing registers one —
    /// see <see cref="Active"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Registered => [.. entries.Select(entry => entry.Effect)];

    /// <summary>Put an effect into force.</summary>
    /// <remarks>
    /// Registering the same entry twice registers it twice, and that is correct:
    /// <c>rr:ability.10</c> — "If multiple instances of the same constant
    /// ability are in play, each instance affects the game independently."
    /// </remarks>
    /// <param name="effect">What is now in force.</param>
    /// <returns>A handle that removes it again.</returns>
    public Registration Register(ContinuousEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        var entry = new Entry(effect);
        entries.Add(entry);
        return new Registration(this, entry);
    }

    /// <summary>
    /// Grants a modified field to every character one player controls for a
    /// stated duration.
    /// </summary>
    /// <remarks>
    /// The player is anchored by their identity and the affected set is read
    /// live. <c>rr:lasting-effects.4</c> therefore includes an ally that enters
    /// play after this effect was registered instead of freezing the set at
    /// resolution time.
    /// </remarks>
    public Registration GrantToCharactersControlledBy(
        Card source, int player, string field, long amount, string until)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(until);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);

        if (!StateFields.IsModifiable(field))
        {
            throw new RulesNotImplementedException(
                $"'{field}' is not a field the engine can modify");
        }

        return Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            field,
            Amount: amount,
            Card: source.ObjectId,
            Affects: world.Seats[player].IdentityCard.ObjectId,
            Lasts: Duration.UntilEndOf(until),
            Scope: ContinuousEffect.CharactersControlledBy));
    }

    /// <summary>Everything actually in force on this board, right now.</summary>
    /// <remarks>
    /// <para>
    /// Read afresh every time rather than cached onto the board, which is what
    /// <c>rr:modifiers</c> and <c>rr:lasting-effects.3</c> both describe. Cheap
    /// and called often is the intended shape.
    /// </para>
    /// <para>
    /// <b>Two sources, and only one of them is a list.</b> Lasting and delayed
    /// effects were registered by whatever created them. Constant abilities
    /// never were: <c>rr:ability</c> makes one active "as soon as its card
    /// enters play" and <c>rr:ability.9</c> makes a conditional one active
    /// "anytime the specific condition is met", so both are read off the board
    /// here, card by card, through
    /// <c>ICardAbilities.Constant</c>. Nothing has to remember to register
    /// one when a card arrives or to dispose it when the card goes, and there
    /// is therefore no path into play on which a constant ability is quietly
    /// missing.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Active()
    {
        var registered = entries
            .Where(entry => !suppressed.Contains(entry))
            .Select(entry => entry.Effect)
            .Where(InForce)
            .ToList();
        var constants = ExcludingSuppressedConstants(
            deriving ? assumedConstants : Constant());
        return [.. registered, .. constants];
    }

    private IReadOnlyList<ContinuousEffect> ExcludingSuppressedConstants(
        IReadOnlyList<ContinuousEffect> constants)
    {
        if (suppressedConstants.Count == 0)
        {
            return constants;
        }

        var visible = constants.ToList();
        foreach (var suppressedEffect in suppressedConstants)
        {
            int index = visible.FindIndex(effect => effect == suppressedEffect);
            if (index >= 0)
            {
                visible.RemoveAt(index);
            }
        }
        return visible;
    }

    /// <summary>What every constant ability in play is doing right now.</summary>
    /// <remarks>
    /// <para>
    /// <b>Constants settle together.</b> <c>rr:modifiers.2</c> treats all
    /// modifiers as simultaneous, and one constant may depend on an attribute
    /// another constant grants. Each pass therefore reads the previous complete
    /// pass until two answers agree. An answer that cycles or keeps changing is
    /// refused rather than taken from an arbitrary intermediate pass.
    /// </para>
    /// </remarks>
    private List<ContinuousEffect> Constant()
    {
        var seen = new List<IReadOnlyList<ContinuousEffect>>();
        assumedConstants = [];

        try
        {
            // The card vocabulary is finite, but a malformed dependency can change
            // a numeric modifier forever without repeating a prior list. Sixty-four
            // full passes is the engine's chosen guard against that non-game state;
            // ordinary dependency chains settle in one pass per link.
            for (int pass = 0; pass < 64; pass++)
            {
                var found = new List<ContinuousEffect>();
                deriving = true;
                try
                {
                    foreach (var card in world.Cards)
                    {
                        if (DeckTypes.IsInPlay(card.Area.Type)
                            && !departing.Contains(card.ObjectId))
                        {
                            found.AddRange(world.Abilities.Constant(world, card));
                        }
                    }
                }
                finally
                {
                    deriving = false;
                }

                if (found.SequenceEqual(assumedConstants))
                {
                    return found;
                }

                if (seen.Any(previous => previous.SequenceEqual(found)))
                {
                    throw new RulesNotImplementedException(
                        "the constant abilities do not settle on one simultaneous effect list");
                }

                seen.Add([.. assumedConstants]);
                assumedConstants = found;
            }

            throw new RulesNotImplementedException(
                "the constant abilities did not settle after 64 simultaneous passes");
        }
        finally
        {
            deriving = false;
            assumedConstants = [];
        }
    }

    /// <summary>
    /// End every lasting effect whose duration names this timing point.
    /// </summary>
    /// <remarks>
    /// <c>rr:lasting-effects.5</c>: "A lasting effect expires as soon as the
    /// timing point specified by its duration is reached." The villain phase's
    /// own step 6 is one of these — <c>rr:villain-phase.step.6.a</c>, where
    /// everything lasting "until the end of the round" ends.
    /// </remarks>
    /// <param name="timingPoint">The point that has been reached.</param>
    /// <param name="events">Where to record state changes caused by restored constants.</param>
    /// <returns>How many effects ended.</returns>
    public int Expire(string timingPoint, List<GameEvent>? events = null)
    {
        ArgumentNullException.ThrowIfNull(timingPoint);
        var expired = entries.Where(entry => string.Equals(
            entry.Effect.Lasts?.Until, timingPoint, StringComparison.Ordinal)).ToList();
        End(expired, timingPoint, events);
        return expired.Count;
    }

    /// <summary>
    /// Apply one use of an effect, and end it if that was its last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound that is not a timing point: "reduce the cost of the next card
    /// you play by 1" is spent by a card being played, whenever that happens.
    /// An effect with no <see cref="Duration.Uses"/> is unlimited and this only
    /// reports that it applied.
    /// </para>
    /// <para>
    /// When the same effect is registered twice, one of the two is spent and
    /// the other is not, which is what <c>rr:ability.10</c> asks for: each
    /// instance affects the game independently.
    /// </para>
    /// </remarks>
    /// <param name="effect">The effect being applied.</param>
    /// <returns>False when no registered copy of it had a use left.</returns>
    public bool Use(ContinuousEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        var entry = entries.FirstOrDefault(
            candidate => candidate.Effect == effect && candidate.Remaining != 0);
        if (entry is null)
        {
            return false;
        }

        if (entry.Remaining is int remaining)
        {
            if (remaining <= 1)
            {
                End([entry], "continuous effect used", events: null);
            }
            else
            {
                entry.Remaining = remaining - 1;
            }
        }

        return true;
    }

    /// <summary>
    /// Resolve every delayed effect waiting on a condition that has just
    /// occurred, and end those that were waiting for the last time.
    /// </summary>
    /// <remarks>
    /// <c>rr:delayed-effect.1</c> — they resolve "automatically and immediately
    /// after their specified timing point or future condition occurs or becomes
    /// true, and before responses to that point or condition may be used". So
    /// this is called at the occurrence, not from the response window.
    /// <c>rr:delayed-effect.2</c> is why the result is a plain list rather than
    /// anything that goes into a window: "it is not treated as a new triggered
    /// ability, even if the delayed effect was originally created by a triggered
    /// ability".
    /// </remarks>
    /// <param name="condition">The condition that has occurred.</param>
    public IReadOnlyList<ContinuousEffect> Occur(string condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var due = entries
            .Where(entry => string.Equals(
                entry.Effect.Lasts?.OnCondition, condition, StringComparison.Ordinal))
            .ToList();

        var ending = due.Where(entry => entry.Remaining is null or <= 1).ToList();
        End(ending, condition, events: null);
        foreach (var entry in due.Except(ending))
        {
            entry.Remaining -= 1;
        }

        return [.. due.Select(entry => entry.Effect)];
    }

    private bool InForce(ContinuousEffect effect)
    {
        if (effect.Source != EffectSource.ConstantAbility)
        {
            return true;
        }

        // Derived rather than deregistered. See the class remarks.
        return effect.Card is int card
            && card >= 0
            && card < world.Cards.Count
            && DeckTypes.IsInPlay(world.Cards[card].Area.Type);
    }

    private void Remove(Entry entry)
    {
        if (entries.Contains(entry))
        {
            End([entry], "continuous effect ended", events: null);
        }
    }

    private void End(
        List<Entry> ending,
        string trigger,
        List<GameEvent>? events)
    {
        if (ending.Count == 0)
        {
            return;
        }

        Card[] candidates = LostUsesCandidates();
        ConstantEnding constantsEnding;
        suppressed.UnionWith(ending);
        try
        {
            var restoredUses = RestoredUsesAfter(candidates);
            constantsEnding = PreflightDepartures(
                [.. restoredUses, .. LethalAfterHealthEnds(
                    ending.Select(entry => entry.Effect))],
                includeHostedCards: true, moveRoots: true);
        }
        finally
        {
            suppressed.ExceptWith(ending);
        }

        foreach (var entry in ending)
        {
            entries.Remove(entry);
        }

        var sink = events ?? [];
        using var departure = constantsEnding.Begin();
        constantsEnding.Complete(trigger, sink);
    }

    private Card[] LostUsesCandidates() => world.Cards.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
            && !FacedownDrones.Is(card)
            && Characteristics.IsLost(world, card, "uses")
            && Reveal.Uses(world.Facts.Attributes(card.FaceId)).Count > 0
            && card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value) == 0).ToArray();

    private Card[] RestoredUsesAfter(Card[] candidates)
    {
        var restored = candidates
            .Where(card => !Characteristics.IsLost(world, card, "uses"))
            .ToArray();
        var restoredIds = restored.Select(card => card.ObjectId).ToHashSet();
        return restored.Where(card => !HasHostedAncestor(card, restoredIds)).ToArray();
    }

    private Card[] LethalAfterHealthEnds(IEnumerable<ContinuousEffect> ending)
    {
        var health = ending.Where(effect =>
                string.Equals(effect.Kind, "health", StringComparison.Ordinal)
                && effect.Amount > 0)
            .ToArray();
        if (health.Length == 0)
        {
            return [];
        }

        return world.Cards.Where(card =>
                DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(FacedownDrones.Kind(card, world.Facts))
                && StateFields.Modified(
                    world, card, "is_infinite_health", world.Facts, world.Players) <= 0
                && health.Where(effect => effect.AppliesTo(world, card))
                    .Sum(effect => effect.Amount) is long lost and > 0
                && card.Damage < Play.Damage.Health(world, world.Facts, card) + lost
                && card.Damage >= Play.Damage.Health(world, world.Facts, card))
            .ToArray();
    }

    /// <summary>Maximum hit points before a state change can alter constants.</summary>
    public IReadOnlyDictionary<int, long> CaptureCharacterHealth() => world.Cards
        .Where(card => DeckTypes.IsInPlay(card.Area.Type)
            && CardKinds.IsCharacter(FacedownDrones.Kind(card, world.Facts)))
        .OrderBy(card => card.ObjectId)
        .ToDictionary(
            card => card.ObjectId,
            card => Play.Damage.Health(world, world.Facts, card));

    /// <summary>Defeat characters made lethal by a health modifier ending.</summary>
    /// <remarks>
    /// <c>rr:hit-points.2.3</c> and <c>rr:hit-points.3.1</c> apply when a
    /// conditional constant switches off just as they do when its source
    /// leaves play. The before-image makes causality explicit: an unrelated
    /// state change does not rediscover a character that was already at zero.
    /// </remarks>
    public bool SettleLostHealth(
        IReadOnlyDictionary<int, long> before, string trigger,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);

        bool suspended = false;
        foreach (var (id, formerHealth) in before.OrderBy(pair => pair.Key))
        {
            if (id < 0 || id >= world.Cards.Count)
            {
                continue;
            }
            var card = world.Cards[id];
            if (!DeckTypes.IsInPlay(card.Area.Type)
                || !CardKinds.IsCharacter(FacedownDrones.Kind(card, world.Facts)))
            {
                continue;
            }

            long currentHealth = Play.Damage.Health(world, world.Facts, card);
            if (currentHealth < formerHealth
                && card.Damage < formerHealth
                && card.Damage >= currentHealth)
            {
                suspended |= SettleHealthDefeat(card, trigger, events);
            }
        }
        return suspended;
    }

    private bool SettleHealthDefeat(Card card, string trigger, List<GameEvent> events)
    {
        if (StateFields.Modified(
                world, card, "is_infinite_health", world.Facts, world.Players) > 0
            || card.Damage < Play.Damage.Health(world, world.Facts, card))
        {
            return false;
        }

        if (healthDefeatPending.Contains(card.ObjectId))
        {
            return true;
        }

        // The Rules Reference names the condition but no event-stream verb;
        // the engine chooses this spelling to distinguish it from damage.
        const string verb = "Hit_Points_Reduced";
        var occurrence = world.Agenda.Occurrence;
        if (!world.Abilities.WouldBeDefeated(
                world, card, card, trigger, verb, by: -1,
                events: events, recordDefeatOn: occurrence))
        {
            healthDefeatPending.Add(card.ObjectId);
            return true;
        }

        if (card.Damage >= Play.Damage.Health(world, world.Facts, card))
        {
            Defeat.Character(
                world, world.Facts, card, trigger, events,
                how: verb, recordOn: occurrence);
        }
        return false;
    }

    /// <summary>Marks a suspended health-loss defeat procedure as settled.</summary>
    internal void CompleteHealthDefeat(Card card) =>
        healthDefeatPending.Remove(card.ObjectId);

    private bool HasHostedAncestor(Card card, HashSet<int> candidates)
    {
        int host = card.Area.Host;
        var seen = new HashSet<int> { card.ObjectId };
        bool candidateAncestor = false;
        while (host >= 0)
        {
            if (!seen.Add(host))
            {
                throw new RulesNotImplementedException(
                    $"attachment {host} forms a hosting cycle");
            }
            if (candidates.Contains(host))
            {
                candidateAncestor = true;
            }
            host = host < world.Cards.Count ? world.Cards[host].Area.Host : -1;
        }
        return candidateAncestor;
    }

    /// <summary>
    /// Prove the state-based changes caused by one card's constants ending.
    /// </summary>
    /// <remarks>
    /// The source is still in play while this runs. Its constants are hidden
    /// only for the simulated post-departure read, so a refusal leaves both the
    /// source and every affected card untouched.
    /// </remarks>
    public ConstantEnding PreflightConstantsEnding(
        Card source, bool includeHostedCards = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!DeckTypes.IsInPlay(source.Area.Type) || departing.Contains(source.ObjectId))
        {
            return new ConstantEnding(this, [], []);
        }

        return PreflightDepartures(
            [source], includeHostedCards, moveRoots: false);
    }

    /// <summary>Proves one ordered transaction whose complete card set will depart.</summary>
    internal ConstantEnding PreflightConstantsEnding(
        IReadOnlyList<Card> sources,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(attachmentPreflightExemptions);
        return PreflightDepartures(
            [.. sources.DistinctBy(card => card.ObjectId)],
            includeHostedCards: false,
            moveRoots: false,
            attachmentPreflightExemptions);
    }

    private ConstantEnding PreflightDepartures(
        Card[] sources, bool includeHostedCards, bool moveRoots,
        IReadOnlySet<int>? attachmentPreflightExemptions = null)
    {
        if (sources.Length == 0)
        {
            return new ConstantEnding(this, [], []);
        }

        int firstSuppressed = suppressedConstants.Count;
        bool simulationEnded = false;
        try
        {
            var planned = new List<Card>();
            var plannedIds = new HashSet<int>();
            foreach (var source in sources)
            {
                AddDeparture(source, includeHostedCards, planned, plannedIds);
            }
            var definiteIds = moveRoots
                ? new HashSet<int>()
                : [.. plannedIds];

            var restored = moveRoots ? sources.ToList() : [];
            var restoredIds = restored.Select(card => card.ObjectId).ToHashSet();
            foreach (var card in LostUsesCandidates())
            {
                if (definiteIds.Contains(card.ObjectId))
                {
                    continue;
                }
                if (restoredIds.Add(card.ObjectId))
                {
                    restored.Add(card);
                }
                if (!plannedIds.Contains(card.ObjectId))
                {
                    AddDeparture(card, includeHostedCards: true, planned, plannedIds);
                }
            }

            var pending = new Queue<Card>(planned);
            while (pending.Count > 0)
            {
                var layer = new List<Card>();
                while (pending.TryDequeue(out var leaving))
                {
                    layer.Add(leaving);
                }

                var ending = layer
                    .SelectMany(card => world.Abilities.Constant(world, card))
                    .ToArray();
                var candidates = LostUsesCandidates();
                suppressedConstants.AddRange(ending);

                foreach (var card in RestoredUsesAfter(candidates))
                {
                    if (!definiteIds.Contains(card.ObjectId)
                        && restoredIds.Add(card.ObjectId))
                    {
                        restored.Add(card);
                    }
                    if (!plannedIds.Contains(card.ObjectId))
                    {
                        int before = planned.Count;
                        AddDeparture(card, includeHostedCards: true, planned, plannedIds);
                        foreach (var added in planned.Skip(before))
                        {
                            pending.Enqueue(added);
                        }
                    }
                }
                foreach (var card in LethalAfterHealthEnds(ending))
                {
                    if (!definiteIds.Contains(card.ObjectId)
                        && restoredIds.Add(card.ObjectId))
                    {
                        restored.Add(card);
                    }
                    if (!plannedIds.Contains(card.ObjectId))
                    {
                        int before = planned.Count;
                        AddDeparture(card, includeHostedCards: true, planned, plannedIds);
                        foreach (var added in planned.Skip(before))
                        {
                            pending.Enqueue(added);
                        }
                    }
                }
            }

            var roots = restored.Distinct().ToArray();
            var rootTrees = roots.ToDictionary(
                root => root.ObjectId,
                root => planned
                    .Where(card => card.ObjectId == root.ObjectId
                        || HasHostedAncestor(card, [root.ObjectId]))
                    .Select(card => card.ObjectId)
                    .ToArray());
            var definiteSources = planned
                .Where(source => definiteIds.Contains(source.ObjectId))
                .ToArray();

            // The derived-effect simulation has found every tentative cascade
            // root. End it before projecting physical absence, so constants
            // are now derived from the projected board itself.
            suppressedConstants.RemoveRange(
                firstSuppressed, suppressedConstants.Count - firstSuppressed);
            simulationEnded = true;

            var selected = PreflightSelectedDepartures(
                roots, definiteIds, definiteSources, rootTrees,
                attachmentPreflightExemptions ?? new HashSet<int>());
            var departures = definiteIds
                .Concat(selected.SelectMany(card => rootTrees[card.ObjectId]))
                .Distinct()
                .ToArray();
            return new ConstantEnding(this, selected, departures);
        }
        finally
        {
            if (!simulationEnded)
            {
                suppressedConstants.RemoveRange(
                    firstSuppressed, suppressedConstants.Count - firstSuppressed);
            }
        }
    }

    private Card[] PreflightSelectedDepartures(
        Card[] roots,
        IReadOnlySet<int> definiteIds,
        IReadOnlyList<Card> definiteSources,
        Dictionary<int, int[]> rootTrees,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        var selected = new HashSet<int>();
        while (true)
        {
            var projected = definiteIds
                .Concat(selected.SelectMany(id => rootTrees[id]))
                .Distinct()
                .ToArray();
            int[] newlyEligible;
            using (ProjectOut(projected))
            {
                newlyEligible = roots.Where(card =>
                        !selected.Contains(card.ObjectId)
                        && DeckTypes.IsInPlay(card.Area.Type)
                        && !Characteristics.IsLost(world, card, "uses"))
                    .Select(card => card.ObjectId)
                    .ToArray();
            }
            if (newlyEligible.Length > 0)
            {
                selected.UnionWith(newlyEligible);
                continue;
            }

            var selectedRoots = roots.Where(card => selected.Contains(card.ObjectId))
                .Where(card => !HasHostedAncestor(card, selected))
                .ToArray();

            PreflightDefiniteAttachments(
                definiteSources, definiteIds, attachmentPreflightExemptions);

            using (ProjectOut([.. definiteIds]))
            {
                foreach (var root in selectedRoots)
                {
                    Discard.PreflightProjectedAttachments(
                        world,
                        root,
                        rootTrees[root.ObjectId].Skip(1).Select(id => world.Cards[id]));
                }
            }

            return selectedRoots;
        }
    }

    /// <summary>Checks each hosted card at the board where its direct host departs.</summary>
    private void PreflightDefiniteAttachments(
        IReadOnlyList<Card> sources,
        IReadOnlySet<int> definiteIds,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        foreach (var source in sources)
        {
            var direct = world.Areas
                .Where(area => area.Host == source.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => definiteIds.Contains(card.ObjectId))
                .Where(card => !attachmentPreflightExemptions.Contains(card.ObjectId))
                .ToArray();
            if (direct.Length == 0)
            {
                continue;
            }

            // Project only the direct host and earlier ancestors. The hosted
            // cards remain in play, so their own constants can react to the
            // host's absence before Permanent is read. A captured Victory root
            // begins a separate earlier departure group while the defeated
            // host's constants are still active.
            var projected = new List<int>();
            var leaving = source;
            while (definiteIds.Contains(leaving.ObjectId))
            {
                projected.Add(leaving.ObjectId);
                if (attachmentPreflightExemptions.Contains(leaving.ObjectId)
                    || leaving.Area.Host < 0
                    || leaving.Area.Host >= world.Cards.Count)
                {
                    break;
                }
                leaving = world.Cards[leaving.Area.Host];
            }

            using (ProjectOut(projected))
            {
                Discard.PreflightProjectedAttachments(world, source, direct);
            }
        }
    }

    private ProjectionScope ProjectOut(IReadOnlyList<int> ids)
    {
        var cards = ids.Select(id => world.Cards[id]).Distinct().ToArray();
        var orders = cards.Select(card => card.Area)
            .Distinct()
            .ToDictionary(area => area, area => area.Cards.ToArray());
        var detached = new Area(-1, DeckType.RemovedArea, -1, PlayArea.Villains, -1);
        foreach (var card in cards)
        {
            card.Area.Remove(card);
            card.ProjectTo(detached);
        }
        return new ProjectionScope(orders);
    }

    private sealed class ProjectionScope(
        IReadOnlyDictionary<Area, Card[]> orders) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            foreach (var (area, order) in orders)
            {
                area.Replace(order);
                foreach (var card in order)
                {
                    card.ProjectTo(area);
                }
            }
            disposed = true;
        }
    }

    private void AddDeparture(
        Card root, bool includeHostedCards, List<Card> planned, HashSet<int> plannedIds)
    {
        var path = new HashSet<int>();
        var pending = new Stack<(Card Card, bool Exit)>();
        pending.Push((root, false));
        while (pending.TryPop(out var frame))
        {
            var card = frame.Card;
            if (frame.Exit)
            {
                path.Remove(card.ObjectId);
                continue;
            }

            if (path.Contains(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            if (!plannedIds.Add(card.ObjectId))
            {
                // Another root already planned this complete subtree. Sharing
                // a plan is not a hosting cycle; only revisiting the current
                // ancestor path is.
                continue;
            }

            planned.Add(card);
            if (!includeHostedCards)
            {
                continue;
            }

            path.Add(card.ObjectId);
            pending.Push((card, true));
            foreach (var child in world.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards)
                         .Reverse())
            {
                pending.Push((child, false));
            }
        }
    }

    private void CompleteConstantsEnding(
        IReadOnlyList<Card> restored,
        string trigger,
        List<GameEvent> events)
    {
        foreach (var card in restored.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)))
        {
            // `rr:hit-points.3.1`: when an ally or minion's +X hit-point
            // effect ends and its damage is now at least its hit points, that
            // character is defeated. Other restored cards here regained a
            // zero-use keyword and follow that keyword's discard rule.
            if (CardKinds.IsCharacter(FacedownDrones.Kind(card, world.Facts))
                && StateFields.Modified(
                    world, card, "is_infinite_health", world.Facts, world.Players) <= 0
                && card.Damage >= Play.Damage.Health(world, world.Facts, card))
            {
                _ = SettleHealthDefeat(card, trigger, events);
            }
            else
            {
                Discard.Card(world, card, trigger, events);
            }
        }
    }

    /// <summary>Whether a card is part of one preflighted departure snapshot.</summary>
    internal bool IsDeparting(Card card) => departing.Contains(card.ObjectId);

    /// <summary>A preflighted set of state-based changes after constants end.</summary>
    public sealed class ConstantEnding
    {
        private readonly ContinuousEffects effects;
        private readonly IReadOnlyList<Card> restored;
        private readonly IReadOnlyList<int> departures;
        private bool completed;

        internal ConstantEnding(
            ContinuousEffects effects,
            IReadOnlyList<Card> restored,
            IReadOnlyList<int> departures)
        {
            this.effects = effects;
            this.restored = restored;
            this.departures = departures;
        }

        /// <summary>
        /// Mark the whole preflighted cascade as one departure while it is applied.
        /// </summary>
        public IDisposable Begin() => effects.BeginDepartures(departures);

        /// <summary>Apply the preflighted changes after the source has left play.</summary>
        public void Complete(string trigger, List<GameEvent> events)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            ArgumentNullException.ThrowIfNull(events);
            if (completed)
            {
                return;
            }

            effects.CompleteConstantsEnding(restored, trigger, events);
            completed = true;
        }
    }

    private DepartureScope BeginDepartures(IReadOnlyList<int> cards)
    {
        var added = new List<int>();
        foreach (int card in cards)
        {
            if (departing.Add(card))
            {
                added.Add(card);
            }
        }
        return new DepartureScope(departing, added);
    }

    private sealed class DepartureScope(HashSet<int> departing, IReadOnlyList<int> cards)
        : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            foreach (int card in cards)
            {
                departing.Remove(card);
            }
            disposed = true;
        }
    }

    /// <summary>One registered effect and its remaining uses.</summary>
    /// <remarks>
    /// The effect itself is immutable, because it has to be writable to a save.
    /// How much of it is left over is not part of what the card says, so it
    /// lives here.
    /// </remarks>
    internal sealed class Entry(ContinuousEffect effect)
    {
        public ContinuousEffect Effect { get; } = effect;

        public int? Remaining { get; set; } = effect.Lasts?.Uses;
    }

    /// <summary>A registered effect, and the means to end it.</summary>
    /// <remarks>
    /// Disposing is how a lasting or delayed effect ends early — a cancel, or a
    /// delayed effect that has resolved and is spent
    /// (<c>rr:delayed-effect.1</c>). Disposing twice is harmless.
    /// </remarks>
    public sealed class Registration : IDisposable
    {
        private readonly ContinuousEffects effects;
        private readonly Entry entry;
        private bool disposed;

        internal Registration(ContinuousEffects effects, Entry entry)
        {
            this.effects = effects;
            this.entry = entry;
        }

        /// <summary>What was registered.</summary>
        public ContinuousEffect Effect => entry.Effect;

        /// <summary>How many applications are left, or null for unlimited.</summary>
        public int? Remaining => entry.Remaining;

        /// <summary>End it.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            effects.Remove(entry);
            disposed = true;
        }
    }
}
