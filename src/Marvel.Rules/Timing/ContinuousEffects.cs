using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

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
/// an implementation choice, and
/// <see cref="Active"/> is meant to be cheap and
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
/// board</i>, and <see cref="Active"/>
/// derives it rather than trusting somebody
/// to have disposed the registration. A forgotten deregistration would be a
/// ghost: an ally's +1 ATK still being counted from the discard pile, on a board
/// that looks entirely normal. The rules make that unnecessary to risk.
/// </para>
/// <para>
/// <c>rr:lasting-effects.4</c> is why
/// <see cref="Active"/> takes the world every
/// time instead of resolving affected cards at registration: "If a card enters
/// play after the creation of a lasting effect, it is still affected by that
/// lasting effect." An entry names a condition, and the condition is re-read.
/// </para>
/// </remarks>
public sealed class ContinuousEffects(World state)
{
    internal readonly World world = state;
    internal readonly List<ContinuousEffectEntry> entries = [];
    internal readonly HashSet<ContinuousEffectEntry> suppressed = [];
    internal readonly List<ContinuousEffect> suppressedConstants = [];
    internal readonly HashSet<int> departing = [];
    internal readonly HashSet<int> healthDefeatPending = [];

    // While constants are settling, a nested read sees the previous complete
    // pass. See `Constant` -- this is iteration state, not cached game state.
    internal bool deriving;
    internal IReadOnlyList<ContinuousEffect> assumedConstants = [];


    /// <summary>Everything registered, in force or not.</summary>
    /// <remarks>
    /// For a save, and for a test that wants to see a stale entry rather than
    /// have it filtered away.
    /// <see cref="Active"/> is what the game reads.
    /// <para>
    /// A constant ability is <b>not</b> here, because nothing registers one —
    /// see <see cref="Active"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Registered => [.. entries.Select(entry => entry.Effect)];

    /// <summary>Put an effect into force.</summary>
    public Registration Register(ContinuousEffect effect) => this.RegisterCore(effect);

    /// <summary>Ends direct lasting effects on a card that leaves play.</summary>
    public void CardLeftPlay(Card card) => this.CardLeftPlayCore(card);

    /// <summary>Grants a field to every character one player controls.</summary>
    public Registration GrantToCharactersControlledBy(
        Card source, int player, string field, long amount, string until) =>
        this.GrantToCharactersControlledByCore(source, player, field, amount, until);

    /// <summary>Everything continuously in force at the current board.</summary>
    public IReadOnlyList<ContinuousEffect> Active() => this.ActiveCore();

    /// <summary>Ends lasting effects at a completed timing point.</summary>
    public int Expire(string timingPoint, List<GameEvent>? events = null) =>
        this.ExpireCore(timingPoint, events);

    /// <summary>Consumes one use of a registered effect.</summary>
    public bool Use(ContinuousEffect effect) => this.UseCore(effect);

    /// <summary>Returns delayed effects whose condition just occurred.</summary>
    public IReadOnlyList<ContinuousEffect> Occur(string condition, int? activationId = null) =>
        this.OccurCore(condition, activationId);

    /// <summary>Captures effective character health before constants change.</summary>
    public IReadOnlyDictionary<int, long> CaptureCharacterHealth() =>
        this.CaptureCharacterHealthCore();

    /// <summary>Settles characters made lethal by a health modifier ending.</summary>
    public bool SettleLostHealth(
        IReadOnlyDictionary<int, long> before, string trigger,
        List<GameEvent> events) =>
        this.SettleLostHealthCore(before, trigger, events);

    /// <summary>Marks a suspended health-loss defeat procedure as settled.</summary>
    internal void CompleteHealthDefeat(Card card) =>
        this.CompleteHealthDefeatCore(card);


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
            return new ConstantEnding(new ContinuousEffectDeparture(this, [], []));
        }

        return new ConstantEnding(new ContinuousEffectCascadePreflight(this).PreflightDepartures(
            [source], includeHostedCards, moveRoots: false));
    }

    /// <summary>A preflighted set of state-based changes after constants end.</summary>
    public sealed class ConstantEnding
    {
        private readonly ContinuousEffectDeparture departure;

        internal ConstantEnding(ContinuousEffectDeparture departure) =>
            this.departure = departure;

        /// <summary>Marks the complete cascade as departing while it is applied.</summary>
        public IDisposable Begin() => departure.Begin();

        /// <summary>Applies state-based changes after the source has left play.</summary>
        public void Complete(string trigger, List<GameEvent> events) =>
            departure.Complete(trigger, events);
    }

    /// <summary>Proves one ordered transaction whose complete card set will depart.</summary>
    internal ContinuousEffectDeparture PreflightConstantsEnding(
        IReadOnlyList<Card> sources,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(attachmentPreflightExemptions);
        return new ContinuousEffectCascadePreflight(this).PreflightDepartures(
            [.. sources.DistinctBy(card => card.ObjectId)],
            includeHostedCards: false,
            moveRoots: false,
            attachmentPreflightExemptions);
    }

    internal void CompleteConstantsEnding(
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
            if (CardKinds.IsCharacter(EffectiveCards.Kind(card, world.Facts))
                && StateFields.Modified(
                    world, card, "is_infinite_health", world.Facts, world.Players) <= 0
                && card.Damage >= Play.DamagePlacement.Health(world, world.Facts, card))
            {
                _ = this.SettleHealthDefeat(card, trigger, events);
            }
            else
            {
                Discard.Card(world, card, trigger, events);
            }
        }
    }

    /// <summary>Whether a card is part of one preflighted departure snapshot.</summary>
    public bool IsDeparting(Card card) => departing.Contains(card.ObjectId);


    internal DepartureScope BeginDepartures(IReadOnlyList<int> cards)
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



    /// <summary>A registered effect, and the means to end it.</summary>
    /// <remarks>
    /// Disposing is how a lasting or delayed effect ends early — a cancel, or a
    /// delayed effect that has resolved and is spent
    /// (<c>rr:delayed-effect.1</c>). Disposing twice is harmless.
    /// </remarks>
    public sealed class Registration : IDisposable
    {
        private readonly ContinuousEffects effects;
        private readonly ContinuousEffectEntry entry;
        private bool disposed;

        internal Registration(ContinuousEffects effects, ContinuousEffectEntry entry)
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
