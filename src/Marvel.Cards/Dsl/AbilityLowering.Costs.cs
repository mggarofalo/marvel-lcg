using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilityModifierLowering;
using static Marvel.Cards.Dsl.AbilityProcedureLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using System.Collections.Immutable;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

internal static class AbilityCostLowering
{
    /// <summary>Lowers an arrow cost and every component of a combined cost.</summary>
    internal static AbilityCost LowerCost(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        var node = Operation(value, location);
        var child = location.Child(node.Kind);
        var argument = node.Argument;
        return node.Kind switch
        {
            "seq" => CostSequence(argument, child),
            "exhaust" => new AbilityCost.Exhaust(CostCard(argument, child)),
            "discard" => DiscardCost(argument, child),
            "removeCounters" => RemoveCounterCost(argument, child),
            "spend" => new AbilityCost.Spend(ResourceString(argument, child), PrintedOnly: false),
            "spendPrinted" => new AbilityCost.Spend(ResourceString(argument, child), PrintedOnly: true),
            "spendEnergyX" => EnergyCost(argument, child),
            "discardFromHand" => new AbilityCost.DiscardFromHand(new AbilityCostRange.Exact(PositiveCount(argument, child))),
            "discardUpToFromHand" => new AbilityCost.DiscardFromHand(new AbilityCostRange.UpTo(PositiveCount(argument, child))),
            "discardAnyFromHand" => AnyDiscardCost(argument, child),
            "exhaustChosen" => ExhaustChosenCost(argument, child),
            "heal" => HealCost(argument, child),
            "dealDamage" => DamageCost(argument, child, mustTakeAll: false),
            "takeDamage" => DamageCost(argument, child, mustTakeAll: true),
            _ => throw child.Error($"'{node.Kind}' is not a cost operation"),
        };
    }

    internal static AbilityCost.Sequence CostSequence(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.List list)
        {
            throw location.Error("expected a list of costs");
        }
        var builder = ImmutableArray.CreateBuilder<AbilityCost>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            builder.Add(LowerCost(list.Values[index], location.Item(index)));
        }
        return new(builder.MoveToImmutable());
    }

    internal static AbilityCostCard CostCard(AbilityValue value, AbilityLocation location) =>
        Text(value, location) switch
        {
            "this" => AbilityCostCard.Source,
            "you" => AbilityCostCard.Identity,
            var name => throw location.Error($"'{name}' is not a card supported by this cost"),
        };

    internal static AbilityCost.Discard DiscardCost(AbilityValue value, AbilityLocation location)
    {
        if (value is AbilityValue.Map)
        {
            var fields = Fields(value, location, "card");
            return new(CostCard(Required(fields, "card", location), location.Child("card")));
        }
        return new(CostCard(value, location));
    }

    internal static AbilityCost.RemoveCounters RemoveCounterCost(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "counter", "count");
        string counter = Text(Required(fields, "counter", location), location.Child("counter"));
        if (counter.Length == 0)
        {
            throw location.Child("counter").Error("expected a nonempty counter name");
        }
        return new(CostCard(Required(fields, "card", location), location.Child("card")), counter,
            PositiveInteger(Required(fields, "count", location), location.Child("count")));
    }

    internal static AbilityCost.SpendEnergy EnergyCost(AbilityValue value, AbilityLocation location)
    {
        FixedWord(value, location, "Y");
        return new();
    }

    internal static AbilityCost.DiscardFromHand AnyDiscardCost(AbilityValue value, AbilityLocation location)
    {
        FixedWord(value, location, "yourHand");
        return new(new AbilityCostRange.Any());
    }

    internal static AbilityCost.ExhaustChosen ExhaustChosenCost(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "count", "upTo", "anyNumber");
        var from = SelectCards(Required(fields, "from", location), location.Child("from"));
        if (from is not AbilityCardSelection.Query
            { Kind: AbilityCardQuery.HeroesAndAllies or AbilityCardQuery.CharactersYouControl or AbilityCardQuery.AlliesYouControl } query)
        {
            throw location.Child("from").Error("expected a supported exhaust-cost relation");
        }
        string[] ranges = ["count", "upTo", "anyNumber"];
        if (ranges.Count(fields.ContainsKey) > 1)
        {
            throw location.Error("a cost cannot declare several selection ranges");
        }
        AbilityCostRange range = fields.TryGetValue("count", out var count)
            ? new AbilityCostRange.Exact(PositiveCount(count, location.Child("count")))
            : fields.TryGetValue("upTo", out var upTo)
                ? new AbilityCostRange.UpTo(PositiveCount(upTo, location.Child("upTo")))
                : new AbilityCostRange.Exact(1);
        if (fields.TryGetValue("anyNumber", out var any))
        {
            // This marker's spelling is the engine's choice, not a rulebook term.
            FixedWord(any, location.Child("anyNumber"), "true");
            range = new AbilityCostRange.Any();
        }
        return new(query.Kind, range);
    }

    internal static AbilityCost.Heal HealCost(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "amount");
        return new(CostCard(Required(fields, "card", location), location.Child("card")),
            PositiveInteger(Required(fields, "amount", location), location.Child("amount")));
    }

    internal static AbilityCost.Damage DamageCost(AbilityValue value, AbilityLocation location, bool mustTakeAll)
    {
        var fields = Fields(value, location, "cards", "amount");
        return new(CostCard(Required(fields, "cards", location), location.Child("cards")),
            PositiveInteger(Required(fields, "amount", location), location.Child("amount")), mustTakeAll);
    }

    internal static long PositiveInteger(AbilityValue value, AbilityLocation location)
    {
        long number = Integer(value, location);
        return number > 0 ? number : throw location.Error("expected a positive integer");
    }

    internal static int PositiveCount(AbilityValue value, AbilityLocation location)
    {
        long number = PositiveInteger(value, location);
        return number <= int.MaxValue ? (int)number : throw location.Error("selection count exceeds the engine range");
    }

    internal static string ResourceString(AbilityValue value, AbilityLocation location)
    {
        string text = Text(value, location);
        return text.Length > 0 && text.All(symbol =>
            symbol is Resources.Mental or Resources.Energy or Resources.Physical or Resources.Wild)
            ? text : throw location.Error("expected one or more supported resource symbols");
    }
}
