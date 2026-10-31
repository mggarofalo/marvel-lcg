using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A printed card cost of X, which the playing player defines.</summary>
/// <remarks>
/// <c>rr:non-numerical-variable</c>: an undefined variable is 0, and ruling
/// 5636cf3904ac9370 reads an X cost in hand as 0 for other abilities. A play
/// defines X by player choice (<c>rr:non-numerical-variable.1</c>) and pays it
/// through the card's own variable cost on top of that zero.
/// </remarks>
public static class VariableCardCost
{
    /// <summary>The printed spelling of the cost.</summary>
    public const string Printed = "X";

    /// <summary>Whether the card's printed cost is X.</summary>
    public static bool Applies(string faceId, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Attributes(faceId).TryGetValue("Cost", out string? printed) && printed == Printed;
    }

    /// <summary>The part of a reduction left for the amount paid for X, after the rest of the cost reached zero.</summary>
    /// <remarks>
    /// <c>rr:non-numerical-variable.1</c>: X is defined first, "after which the amount
    /// paid may be modified by effects without changing the value of X". Only an event
    /// defines X through its own variable cost; another card kind costing X raises
    /// rather than being priced free.
    /// </remarks>
    public static long Remaining(Card card, ICardFacts facts, long reductions, long fixedCost)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!Applies(card.FaceId, facts)) return 0;
        if (EffectiveCards.Kind(card, facts) != CardKind.Event)
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' costs X, which only an event's own variable cost defines");
        return Math.Max(0, reductions - fixedCost);
    }
}
