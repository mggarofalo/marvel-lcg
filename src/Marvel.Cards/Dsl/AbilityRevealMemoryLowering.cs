namespace Marvel.Cards.Dsl;

// differentTitleFromRevealed reads the cards its own ability invocation revealed.
// The engine chooses to admit the field only in an effect that reveals a card,
// and never in a cost or play condition, which are read before any reveal.
internal static class AbilityRevealMemoryLowering
{
    private const string Field = "differentTitleFromRevealed";

    internal static void Validate(CardAbility ability, AbilityLocation location, IEnumerable<AbilityEffect> nodes)
    {
        if (ability.Cost is { } cost && Mentions(AbilityBookLowering.Syntax(cost)))
            throw (location with { Path = "cost" }).Error($"{Field} is read only by an effect");
        if (ability.When is { } when && Mentions(AbilityBookLowering.Syntax(when)))
            throw (location with { Path = "when" }).Error($"{Field} is read only by an effect");
        if (Mentions(AbilityBookLowering.Syntax(ability.Effect)) && !nodes.Any(node =>
                node is AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal }))
            throw location.Error($"{Field} requires an effect that reveals a card");
    }

    private static bool Mentions(AbilityValue value) => value switch
    {
        AbilityValue.Map map => map.Entries.Any(entry =>
            string.Equals(entry.Key, Field, StringComparison.Ordinal) || Mentions(entry.Value)),
        AbilityValue.List list => list.Values.Any(Mentions),
        _ => false,
    };
}
