using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Checks authored syntax and lowers it into engine-owned language operations.</summary>
public static class AbilityLowering
{
    /// <summary>Lowers the complete authored ability book.</summary>
    public static AbilityProgram Book(AbilityBook book) =>
        AbilityBookLowering.LowerBook(book);

    /// <summary>Lowers a card-selection expression.</summary>
    public static AbilityCardSelection Cards(
        AbilityValue value, AbilityLocation location) =>
        AbilitySelectorLowering.SelectCards(value, location);

    /// <summary>Lowers an ability cost.</summary>
    public static AbilityCost Cost(AbilityValue value, AbilityLocation location) =>
        AbilityCostLowering.LowerCost(value, location);

    /// <summary>Lowers an ability effect.</summary>
    public static AbilityEffect Effect(AbilityValue value, AbilityLocation location) =>
        AbilityEffectLowering.LowerEffect(value, location);

    /// <summary>Lowers an ability condition.</summary>
    public static AbilityCondition Condition(
        AbilityValue value, AbilityLocation location) =>
        AbilityConditionLowering.LowerCondition(value, location);

    /// <summary>Lowers a player reference.</summary>
    public static AbilityPlayer Player(AbilityValue value, AbilityLocation location) =>
        AbilityConditionLowering.LowerPlayer(value, location);

    /// <summary>Lowers a numeric expression, including its queries and conditions.</summary>
    /// <remarks>
    /// This method does not evaluate a game or select a
    /// branch: every operand is checked in authored order.
    /// </remarks>
    public static AbilityNumber Number(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        if (value is AbilityValue.Number number)
        {
            return new AbilityNumber.Constant(number.Value);
        }

        if (value is not AbilityValue.Map { Entries.Count: 1 } map)
        {
            throw location.Error("expected a number or one numeric operation");
        }

        var (kind, argument) = map.Entries.First();
        var child = location.Child(kind);
        return kind switch
        {
            "perPlayer" => new AbilityNumber.PerPlayer(Integer(argument, child)),
            "result" => new AbilityNumber.Result(ResultName(argument, child)),
            "add" => new AbilityNumber.Sum(Operands(argument, child, nonempty: false)),
            "mul" => new AbilityNumber.Product(Operands(argument, child, nonempty: false)),
            "min" => new AbilityNumber.Minimum(Operands(argument, child, nonempty: true)),
            "tokensOn" => CardNumber(argument, child, AbilityCardNumberProperty.Threat),
            "damageOn" => CardNumber(argument, child, AbilityCardNumberProperty.Damage),
            "remainingHealth" => CardNumber(argument, child, AbilityCardNumberProperty.RemainingHealth),
            "startingHealth" => CardNumber(argument, child, AbilityCardNumberProperty.StartingHealth),
            "countersOn" => Counters(argument, child),
            "modified" => Modified(argument, child),
            "count" => new AbilityNumber.Count(SelectCards(argument, child)),
            "if" => ConditionalNumber(argument, child),
            "printedResourceCountDiscarded" => new AbilityNumber.PrintedResourcesDiscarded(Resource(argument, child)),
            "discardedWithResource" => new AbilityNumber.DiscardedWithResource(Resource(argument, child)),
            "powerAmount" => FixedNumber(argument, child, "cardsDiscarded", AbilityResolutionNumber.PowerAmount),
            "printedBoostIconsDiscarded" => FixedNumber(argument, child, "game", AbilityResolutionNumber.PrintedBoostIconsDiscarded),
            "topEncounterDiscardBoostPlusOne" => FixedNumber(argument, child, "game", AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne),
            _ => throw child.Error($"'{kind}' is not a numeric operation"),
        };
    }

    internal static ImmutableArray<AbilityNumber> Operands(
        AbilityValue value, AbilityLocation location, bool nonempty)
    {
        if (value is not AbilityValue.List list)
        {
            throw location.Error("expected a list of numeric operands");
        }
        if (nonempty && list.Values.Count == 0)
        {
            throw location.Error("expected at least one numeric operand");
        }

        var lowered = ImmutableArray.CreateBuilder<AbilityNumber>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            lowered.Add(Number(list.Values[index], location.Item(index)));
        }
        return lowered.MoveToImmutable();
    }

    internal static long Integer(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw location.Error("expected an integer");

    internal static string Text(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Word word
            ? word.Value
            : throw location.Error("expected a word");

    internal static AbilityNumber.CardValue CardNumber(
        AbilityValue value, AbilityLocation location, AbilityCardNumberProperty property) =>
        new(SelectCards(value, location), property);

    internal static AbilityNumber.Counters Counters(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "counter");
        return new(SelectCards(Required(fields, "card", location), location.Child("card")),
            Text(Required(fields, "counter", location), location.Child("counter")));
    }

    internal static AbilityNumber.Modified Modified(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "field");
        string field = Text(Required(fields, "field", location), location.Child("field"));
        if (!StateFieldCatalog.IsModifiable(field))
        {
            throw location.Child("field").Error($"'{field}' is not an engine-owned modifiable field");
        }
        return new(SelectCards(Required(fields, "card", location), location.Child("card")), field);
    }

    internal static AbilityNumber.Conditional ConditionalNumber(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "test", "then", "else");
        return new(LowerCondition(Required(fields, "test", location), location.Child("test")),
            Number(Required(fields, "then", location), location.Child("then")),
            fields.TryGetValue("else", out var otherwise)
                ? Number(otherwise, location.Child("else")) : new AbilityNumber.Constant(0));
    }

    internal static AbilityNumber.ResolutionValue FixedNumber(
        AbilityValue value, AbilityLocation location, string expected, AbilityResolutionNumber kind)
    {
        FixedWord(value, location, expected);
        return new(kind);
    }

    internal static void FixedWord(AbilityValue value, AbilityLocation location, string expected)
    {
        if (!string.Equals(Text(value, location), expected, StringComparison.Ordinal))
        {
            throw location.Error($"expected '{expected}'");
        }
    }

    internal static char Resource(AbilityValue value, AbilityLocation location)
    {
        string text = Text(value, location);
        return text.Length == 1 && text[0] is Resources.Mental or Resources.Energy or Resources.Physical or Resources.Wild
            ? text[0] : throw location.Error("expected one supported resource symbol");
    }

    internal static string ResultName(AbilityValue value, AbilityLocation location)
    {
        string name = Text(value, location);
        return name is "healed" or "discarded" or "found" or "energy" or "resourceTypes"
            or "activationDamage" or "activationThreat" or "activationMade"
            ? name : throw location.Error($"'{name}' is not an authored resolution result");
    }
}
