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

    /// <summary>Raises when a cost reduction applies to a card that costs X.</summary>
    /// <remarks>
    /// <c>rr:non-numerical-variable.1</c>: X is defined first, "after which the amount
    /// paid may be modified by effects without changing the value of X". X is priced
    /// as its own variable component, so an increase adds to the zero printed part
    /// correctly, while reducing the amount paid for X is not implemented.
    /// </remarks>
    public static void RefuseReduction(string faceId, ICardFacts facts, int modifiers, long self, object? scoped)
    {
        if (Applies(faceId, facts) && (modifiers > 0 || self > 0 || scoped is not null))
            throw new RulesNotImplementedException($"card '{faceId}' costs X and a cost reduction applies to it");
    }
}
