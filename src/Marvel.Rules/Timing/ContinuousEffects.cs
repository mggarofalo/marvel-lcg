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
    Duration? Lasts = null)
{
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

    /// <summary>Everything registered, in force or not.</summary>
    /// <remarks>
    /// For a save, and for a test that wants to see a stale entry rather than
    /// have it filtered away. <see cref="Active"/> is what the game reads.
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

    /// <summary>Everything actually in force on this board, right now.</summary>
    /// <remarks>
    /// Read afresh every time rather than cached onto the board, which is what
    /// <c>rr:modifiers</c> and <c>rr:lasting-effects.3</c> both describe. Cheap
    /// and called often is the intended shape.
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Active() =>
        [.. entries.Select(entry => entry.Effect).Where(InForce)];

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
    /// <returns>How many effects ended.</returns>
    public int Expire(string timingPoint)
    {
        ArgumentNullException.ThrowIfNull(timingPoint);
        return entries.RemoveAll(entry =>
            string.Equals(entry.Effect.Lasts?.Until, timingPoint, StringComparison.Ordinal));
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
            entry.Remaining = remaining - 1;
            if (entry.Remaining <= 0)
            {
                entries.Remove(entry);
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

        foreach (var entry in due)
        {
            if (entry.Remaining is null or <= 1)
            {
                entries.Remove(entry);
            }
            else
            {
                entry.Remaining -= 1;
            }
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

    private void Remove(Entry entry) => entries.Remove(entry);

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

            disposed = true;
            effects.Remove(entry);
        }
    }
}
