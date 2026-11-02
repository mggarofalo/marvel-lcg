namespace Marvel.Cards.Dsl;

// differentTitleFromRevealed and the revealed binding read the cards their own ability
// invocation revealed. The engine chooses to admit them only in an effect that reveals
// a card, and never in a cost or play condition, which are read before any reveal.
internal static class AbilityRevealMemoryLowering
{
    private const string Field = "differentTitleFromRevealed";
    private const string Binding = "revealed";

    internal static void Validate(CardAbility ability, AbilityLocation location, IEnumerable<AbilityEffect> nodes)
    {
        if (ability.Cost is { } cost && Mentions(AbilityBookLowering.Syntax(cost), out string costName))
            throw (location with { Path = "cost" }).Error($"{costName} is read only by an effect");
        if (ability.When is { } when && Mentions(AbilityBookLowering.Syntax(when), out string whenName))
            throw (location with { Path = "when" }).Error($"{whenName} is read only by an effect");
        if (Mentions(AbilityBookLowering.Syntax(ability.Effect), out string name) && !nodes.Any(Reveals))
            throw location.Error($"{name} requires an effect that reveals a card");
    }

    private static bool Reveals(AbilityEffect node) => node is AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal }
        or AbilityEffect.DiscardUntil { Result: AbilityDiscardUntilResult.Reveal };

    private static bool Mentions(AbilityValue value, out string name)
    {
        switch (value)
        {
            case AbilityValue.Word { Value: Binding }:
                name = Binding;
                return true;
            case AbilityValue.Map map:
                foreach (var entry in map.Entries)
                {
                    if (string.Equals(entry.Key, Field, StringComparison.Ordinal))
                    {
                        name = Field;
                        return true;
                    }
                    if (Mentions(entry.Value, out name)) return true;
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
