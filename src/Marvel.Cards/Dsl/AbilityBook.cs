using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// Every authored card, and every ability on them.
/// </summary>
/// <remarks>
/// <b><see cref="Authored"/> is not the same as "has an ability".</b> Most
/// encounter cards have no "When Revealed" at all, and a card that has been
/// read and found to have none is a different thing from one nobody has looked
/// at. Without the distinction an unported card silently does nothing, which is
/// the failure this whole codebase throws rather than allow.
/// </remarks>
/// <param name="Abilities">Every ability, in the order the data lists them.</param>
/// <param name="Authored">Every card that has been read, whether or not it does anything.</param>
/// <param name="AttachTo">
/// What each card that prints "attach to" names, by printed face id.
/// <para>
/// <b>Not an ability, and that is the point.</b> <c>rr:attach-to</c> is a rule
/// about the phrase — "if a card uses the phrase 'attach to', it must be
/// attached to the specified game element <b>as it enters play</b>" — so the
/// engine does the attaching on every path into play and the card supplies only
/// the element. Modelling it as a "When Revealed" instead reads correctly for a
/// card revealed off the encounter deck and is wrong everywhere else:
/// <c>rr:when-revealed-abilities.2</c> says a card put into play without being
/// revealed does not trigger one, and a setup attachment is put into play
/// without being revealed.
/// </para>
/// </param>
/// <param name="ControlledByFirstPlayer">
/// Cards whose setup text gives control to the first player. This is placement
/// metadata, not a claim that the rest of the card's abilities are authored.
/// </param>
/// <param name="PlacementOnly">
/// Cards whose placement text and absence of a When Revealed ability are known,
/// while their other printed abilities remain unauthored.
/// </param>
/// <param name="CounterPools">
/// Card-defined starting counter pools, including whether each is Uses.
/// </param>
public sealed record AbilityBook(
    IReadOnlyList<CardAbility> Abilities,
    IReadOnlySet<string> Authored,
    IReadOnlyDictionary<string, AbilityValue>? AttachTo = null,
    IReadOnlySet<string>? ControlledByFirstPlayer = null,
    IReadOnlySet<string>? PlacementOnly = null,
    IReadOnlyDictionary<string, CardCounterPool>? CounterPools = null)
{
    /// <summary>An empty book. No card has been read.</summary>
    public static AbilityBook None { get; } =
        new([], new HashSet<string>(StringComparer.Ordinal));

    /// <summary>What one card's "attach to" names, or null when it prints none.</summary>
    /// <param name="card">A printed face id.</param>
    public AbilityValue? Attaches(string card) =>
        AttachTo is { } named && named.TryGetValue(card, out var element) ? element : null;

    /// <summary>Whether setup gives this card to the first player.</summary>
    public bool FirstPlayerControls(string card) =>
        ControlledByFirstPlayer?.Contains(card) is true;

    /// <summary>Whether enough is known to resolve this card's reveal as silence.</summary>
    public bool KnowsWhenRevealed(string card) =>
        Authored.Contains(card) || PlacementOnly?.Contains(card) is true;

    /// <summary>Whether only this card's placement and reveal silence are known.</summary>
    public bool IsPlacementOnly(string card) => PlacementOnly?.Contains(card) is true;

    /// <summary>The counter pool a card enters play with, or null.</summary>
    public CardCounterPool? CounterPool(string card) =>
        CounterPools is { } pools && pools.TryGetValue(card, out var pool)
            ? pool
            : null;

    /// <summary>The abilities on one printed face.</summary>
    /// <param name="card">A printed face id.</param>
    public IEnumerable<CardAbility> On(string card) =>
        Abilities.Where(ability => string.Equals(ability.Card, card, StringComparison.Ordinal));
}
