using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// Which occurrences an ability answers, out of all those with its condition.
/// </summary>
/// <remarks>
/// A closed set, because this is the part of a card most likely to grow an
/// escape hatch. "When <b>Rhino</b> attacks" and "when the villain attacks
/// <b>you</b>" are two different relations between a card and an occurrence, and
/// naming them is what stops the DSL acquiring a general predicate.
/// </remarks>
public static class AbilitySubjects
{
    /// <summary>The occurrence is about this card. A card's own "When Revealed".</summary>
    public const string This = "this";

    /// <summary>
    /// The occurrence is about the card this one is attached to. Charge's
    /// "When <b>Rhino</b> attacks", where Rhino is whatever it is attached to —
    /// <c>rr:star-icon.2</c> reads the star as being about "the attached enemy".
    /// </summary>
    public const string AttachedTo = "attachedTo";

    /// <summary>
    /// The occurrence happened to this card's controller. Spider-Sense's "an
    /// attack against <b>you</b>", which <c>rr:attack-enemy-activation.1.4</c>
    /// makes a claim about the attacked <i>player</i> whichever character was
    /// targeted.
    /// </summary>
    public const string You = "you";

    /// <summary>
    /// The occurrence is not about a card at all.
    /// </summary>
    /// <remarks>
    /// <c>rr:triggering-condition</c> makes a condition "a specific occurrence
    /// that takes place in the game", and some of those happen to nobody:
    /// Hunting Gene Traitors answers "after resolving step one of the villain
    /// phase", which names a moment and nothing in it. There is nothing to
    /// match, so a card that answers such a moment says <b>so</b> rather than
    /// being handed a subject it does not have — <c>you</c> would fit by
    /// accident here, because an encounter card's owner and an unattributed
    /// occurrence's player are both the scenario.
    /// </remarks>
    public const string Game = "game";

    /// <summary>Every subject this vocabulary has.</summary>
    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { This, AttachedTo, You, Game };
}
