using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    private static AbilityEffect GrantEffect(AbilityValue value, AbilityLocation location, bool each, bool lasting)
    {
        string target = each ? "cards" : "card";
        var fields = lasting
            ? Fields(value, location, target, "trait", "keyword", "amount", "until")
            : Fields(value, location, target, "trait", "keyword", "amount");
        if (fields.ContainsKey("trait") == fields.ContainsKey("keyword"))
        {
            throw location.Error("expected exactly one of 'trait' and 'keyword'");
        }
        string? until = lasting ? TimingPoint(Required(fields, "until", location), location.Child("until")) : null;
        var cards = Selected(fields, target, location);
        if (fields.TryGetValue("trait", out var trait))
        {
            if (fields.ContainsKey("amount"))
            {
                throw location.Child("amount").Error("a trait grant has no numeric amount");
            }
            return new AbilityEffect.GrantTrait(cards, Text(trait, location.Child("trait")), each, until);
        }
        string field = Modifier(Required(fields, "keyword", location), location.Child("keyword"), keyword: true);
        // These defaults preserve the engine's distinct constant and lasting
        // modifier conventions; they are not inferred from a card's title.
        var amount = fields.TryGetValue("amount", out var number)
            ? Number(number, location.Child("amount")) : new AbilityNumber.Constant(lasting ? 0 : 1);
        return new AbilityEffect.GrantField(cards, field, amount, each, until);
    }

    private static AbilityEffect.GrantControlledCharacters GrantControlledEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "fields", "amount", "until");
        var authored = Required(fields, "fields", location);
        if (authored is not AbilityValue.List list)
        {
            throw location.Child("fields").Error("expected a list of modifiable fields");
        }
        ImmutableArray<string> modifiers = [.. list.Values.Select((item, index) =>
            Modifier(item, location.Child("fields").Item(index), keyword: false))];
        return new(Player(Required(fields, "player", location), location.Child("player")), modifiers,
            Numeric(fields, "amount", location), TimingPoint(Required(fields, "until", location), location.Child("until")));
    }

    private static string Modifier(AbilityValue value, AbilityLocation location, bool keyword)
    {
        string name = Text(value, location);
        return StateFields.IsModifiable(name) || keyword && Keywords.Granted.Contains(name)
            ? name : throw location.Error($"'{name}' is not a modifier implemented by the engine");
    }

    private static string TimingPoint(AbilityValue value, AbilityLocation location)
    {
        string name = Text(value, location);
        return name is TimingPoints.EndOfTurn or TimingPoints.EndOfPlayerPhase or TimingPoints.EndOfVillainPhase
            or TimingPoints.EndOfPhase or TimingPoints.EndOfRound or TimingPoints.EndOfAttack or TimingPoints.EndOfActivation
            ? name : throw location.Error($"'{name}' is not a supported timing point");
    }

    private static AbilityEffect.PreventDamageFrom DamageProhibition(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "sourceKind", "sourceTrait");
        FixedWord(Required(fields, "card", location), location.Child("card"), "this");
        return new(ConditionCardKind(Required(fields, "sourceKind", location), location.Child("sourceKind")),
            Text(Required(fields, "sourceTrait", location), location.Child("sourceTrait")));
    }

    private static AbilityEffect.PreventDamageWhile ConditionalDamageProhibition(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "condition");
        FixedWord(Required(fields, "card", location), location.Child("card"), "this");
        return new(Condition(Required(fields, "condition", location), location.Child("condition")));
    }

    private static AbilityEffect DelayedEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "condition", "effect", "within");
        string condition = Text(Required(fields, "condition", location), location.Child("condition"));
        if (!Steps.EveryCondition.Contains(condition))
        {
            throw location.Child("condition").Error($"'{condition}' is not an engine occurrence");
        }
        var effect = Operation(Required(fields, "effect", location), location.Child("effect"));
        if (effect.Kind == "giveStatus")
        {
            var nested = location.Child("effect/giveStatus");
            var status = Fields(effect.Argument, nested, "card", "status");
            FixedWord(Required(status, "card", nested), nested.Child("card"), "damaged");
            FixedWord(Required(status, "status", nested), nested.Child("status"), "stunned");
            if (condition != Steps.DamageDealt)
            {
                throw location.Child("condition").Error("a delayed stun requires WhenDamageDealt");
            }
            return new AbilityEffect.DelayedStun(fields.TryGetValue("within", out var within)
                ? TimingPoint(within, location.Child("within")) : null);
        }
        if (effect.Kind != "discard")
        {
            throw location.Child("effect").Error("only discard and damage-triggered stun are supported delayed effects");
        }
        if (fields.ContainsKey("within"))
        {
            throw location.Child("within").Error("a delayed discard does not implement a separate timing bound");
        }
        return new AbilityEffect.DelayedDiscard(DiscardEffect(effect.Argument, location.Child("effect/discard")).Selection, condition);
    }
}
