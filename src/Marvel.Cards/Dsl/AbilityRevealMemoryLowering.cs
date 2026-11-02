namespace Marvel.Cards.Dsl;

// differentTitleFromRevealed and the revealed binding read the cards their own ability
// invocation revealed, and the attachedThis binding reads this card once the ability has
// attached it. The engine chooses to admit each only in an effect that does that work,
// and never in a cost or play condition, which are read before it.
internal static class AbilityRevealMemoryLowering
{
    private const string Field = "differentTitleFromRevealed";
    private const string Binding = "revealed";
    private const string Attached = "attachedThis";

    // Fields whose word is a name rather than a card, such as a counter type.
    private static bool IsNameField(string key) => key is "counter" or "mark" or "markThisRound"
        or "markedThisRound" or "trait" or "title" or "keyword" or "status" or "kind" or "type";

    internal static void Validate(CardAbility ability, AbilityLocation location, IEnumerable<AbilityEffect> nodes)
    {
        if (ability.Cost is { } cost && Mentions(AbilityBookLowering.Syntax(cost), out string costName))
            throw (location with { Path = "cost" }).Error($"{costName} is read only by an effect");
        if (ability.When is { } when && Mentions(AbilityBookLowering.Syntax(when), out string whenName))
            throw (location with { Path = "when" }).Error($"{whenName} is read only by an effect");
        if (!Mentions(AbilityBookLowering.Syntax(ability.Effect), out string name)) return;
        if (name == Attached ? !nodes.Any(AttachesThis) : !nodes.Any(Reveals))
            throw location.Error(name == Attached
                ? $"{name} requires an effect that attaches this card"
                : $"{name} requires an effect that reveals a card");
    }

    private static bool Reveals(AbilityEffect node) => node is AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal }
        or AbilityEffect.DiscardUntil { Result: AbilityDiscardUntilResult.Reveal };

    private static bool AttachesThis(AbilityEffect node) =>
        node is AbilityEffect.AttachCards { Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } };

    private static bool Mentions(AbilityValue value, out string name)
    {
        switch (value)
        {
            case AbilityValue.Word { Value: Binding or Attached } word:
                name = word.Value;
                return true;
            case AbilityValue.Map map:
                foreach (var entry in map.Entries)
                {
                    if (string.Equals(entry.Key, Field, StringComparison.Ordinal))
                    {
                        name = Field;
                        return true;
                    }
                    if (!(IsNameField(entry.Key) && entry.Value is AbilityValue.Word)
                        && Mentions(entry.Value, out name)) return true;
                }
                break;
            case AbilityValue.List list:
                foreach (var item in list.Values)
                    if (Mentions(item, out name)) return true;
                break;
        }
        name = "";
        return false;
    }
}
