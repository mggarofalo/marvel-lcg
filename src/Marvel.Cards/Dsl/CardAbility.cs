using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// One printed ability, as data.
/// </summary>
/// <param name="Card">The printed face id it is on, e.g. <c>01099</c>.</param>
/// <param name="Name">
/// The card's own name for it, which is what a player chooses between —
/// <c>rr:labeled-ability</c>, the label before the dash. Spider-Man's is
/// "Spider-Sense". Falls back to the card's name where the ability has none.
/// </param>
/// <param name="Trigger">When it fires.</param>
/// <param name="Effect">What it does.</param>
/// <remarks>
/// The implemented envelope carries the trigger, an optional live condition,
/// a cost and a per-round limit. Target selection remains an effect-tree
/// question until a printed card requires a target in the envelope itself.
/// </remarks>
/// <param name="Cost">
/// What must be paid to use it, or null. <c>rr:cost</c> — "a cost is anything a
/// player must do or pay in order to initiate an ability" — and
/// <c>rr:initiating-abilities.step.5</c> makes paying it a step of its own,
/// aborted "without paying any costs" if it cannot be met. 560 of the 966 action
/// abilities in the pool print one, and by far the commonest is exhausting the
/// card the ability is on.
/// </param>
/// <param name="Limit">
/// How many times per round this ability may be used, or null for no limit.
/// <c>rr:limit</c> — "each copy of an ability with such a limit may be used X
/// times per the specified period, <b>per instance of that ability</b>", so the
/// count is per card in play rather than per printed id.
/// </param>
/// <param name="When">An additional printed condition that must currently be true.</param>
/// <param name="AnyPlayer">
/// Whether the printed ability explicitly permits any player to initiate it.
/// This is card text overriding the ordinary controller or attachment-holder
/// permission, not a conclusion inferred from the card's ownership.
/// </param>
/// <param name="Labels">
/// Every parenthetical attack, defense, or thwart label printed on the
/// ability. Multiple labels belong to one ability and are resolved together.
/// </param>
/// <param name="PrintedResources">
/// Resource icons physically printed in this resource ability's text box.
/// Empty when its generated resources are not printed icons there.
/// </param>
/// <param name="Maximum">
/// A use maximum shared across every copy of this card by title, or null.
/// </param>
public sealed record CardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityNode Effect,
    AbilityNode? Cost = null, long? Limit = null, AbilityNode? When = null,
    bool AnyPlayer = false, IReadOnlyList<string>? Labels = null,
    string PrintedResources = "", AbilityMaximum? Maximum = null);
