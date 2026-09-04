using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Checks authored syntax and lowers it into engine-owned language operations.</summary>
public static partial class AbilityLowering
{
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
            "count" => new AbilityNumber.Count(Cards(argument, child)),
            "if" => ConditionalNumber(argument, child),
            "printedResourceCountDiscarded" => new AbilityNumber.PrintedResourcesDiscarded(Resource(argument, child)),
            "discardedWithResource" => new AbilityNumber.DiscardedWithResource(Resource(argument, child)),
            "powerAmount" => FixedNumber(argument, child, "cardsDiscarded", AbilityResolutionNumber.PowerAmount),
            "printedBoostIconsDiscarded" => FixedNumber(argument, child, "game", AbilityResolutionNumber.PrintedBoostIconsDiscarded),
            "topEncounterDiscardBoostPlusOne" => FixedNumber(argument, child, "game", AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne),
            _ => throw child.Error($"'{kind}' is not a numeric operation"),
        };
    }

    private static ImmutableArray<AbilityNumber> Operands(
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

    private static long Integer(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw location.Error("expected an integer");

    private static string Text(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Word word
            ? word.Value
            : throw location.Error("expected a word");

    private static AbilityNumber.CardValue CardNumber(
        AbilityValue value, AbilityLocation location, AbilityCardNumberProperty property) =>
        new(Cards(value, location), property);

    private static AbilityNumber.Counters Counters(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "counter");
        return new(Cards(Required(fields, "card", location), location.Child("card")),
            Text(Required(fields, "counter", location), location.Child("counter")));
    }

    private static AbilityNumber.Modified Modified(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "field");
        string field = Text(Required(fields, "field", location), location.Child("field"));
        if (!StateFields.IsModifiable(field))
        {
            throw location.Child("field").Error($"'{field}' is not an engine-owned modifiable field");
        }
        return new(Cards(Required(fields, "card", location), location.Child("card")), field);
    }

    private static AbilityNumber.Conditional ConditionalNumber(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "test", "then", "else");
        return new(Condition(Required(fields, "test", location), location.Child("test")),
            Number(Required(fields, "then", location), location.Child("then")),
            fields.TryGetValue("else", out var otherwise)
                ? Number(otherwise, location.Child("else")) : new AbilityNumber.Constant(0));
    }

    private static AbilityNumber.ResolutionValue FixedNumber(
        AbilityValue value, AbilityLocation location, string expected, AbilityResolutionNumber kind)
    {
        FixedWord(value, location, expected);
        return new(kind);
    }

    private static void FixedWord(AbilityValue value, AbilityLocation location, string expected)
    {
        if (!string.Equals(Text(value, location), expected, StringComparison.Ordinal))
        {
            throw location.Error($"expected '{expected}'");
        }
    }

    private static char Resource(AbilityValue value, AbilityLocation location)
    {
        string text = Text(value, location);
        return text.Length == 1 && text[0] is Resources.Mental or Resources.Energy or Resources.Physical or Resources.Wild
            ? text[0] : throw location.Error("expected one supported resource symbol");
    }

    private static string ResultName(AbilityValue value, AbilityLocation location)
    {
        string name = Text(value, location);
        return name is "healed" or "discarded" or "found" or "energy" or "resourceTypes"
            or "activationDamage" or "activationThreat" or "activationMade"
            ? name : throw location.Error($"'{name}' is not an authored resolution result");
    }
}
