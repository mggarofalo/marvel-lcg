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
