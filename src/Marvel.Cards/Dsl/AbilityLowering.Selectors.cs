using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    /// <summary>Lowers card bindings, queries, and their supported selector composition.</summary>
    public static AbilityCardSelection Cards(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        if (value is AbilityValue.Word word)
        {
            return new AbilityCardSelection.Bound(word.Value switch
            {
                "this" => AbilityCardBinding.This,
                "that" => AbilityCardBinding.That,
                "trigger.actor" => AbilityCardBinding.TriggerActor,
                "trigger.target" => AbilityCardBinding.TriggerTarget,
                "chosen" => AbilityCardBinding.Chosen,
                "yourHero" => AbilityCardBinding.YourHero,
                "yourAlterEgo" => AbilityCardBinding.YourAlterEgo,
                "defeater" => AbilityCardBinding.Defeater,
                "activatingEnemy" => AbilityCardBinding.ActivatingEnemy,
                "defeated" => AbilityCardBinding.Defeated,
                "you" => AbilityCardBinding.You,
                "attachedTo" => AbilityCardBinding.AttachedTo,
                "trigger.subject" => AbilityCardBinding.TriggerSubject,
                _ => throw location.Error($"'{word.Value}' is not a card binding"),
            });
        }
        var node = Operation(value, location);
        var child = location.Child(node.Kind);
        return node.Kind switch
        {
            "query" => new AbilityCardSelection.Query(Query(Text(node.Argument, child), child)),
            "titled" => new AbilityCardSelection.Titled(Text(node.Argument, child)),
            "enemiesWithTrait" => new AbilityCardSelection.EnemiesWithTrait(Text(node.Argument, child)),
            "withTrait" => WithTrait(node.Argument, child),
            "withoutAnotherCopyAttached" => new AbilityCardSelection.WithoutAnotherCopyAttached(Cards(node.Argument, child)),
            "discardable" => new AbilityCardSelection.Discardable(Cards(node.Argument, child)),
            "minBy" or "maxBy" => Ranked(node.Argument, child, node.Kind == "maxBy"),
            "cardsIn" => InAreas(node.Argument, child),
            _ => throw child.Error($"'{node.Kind}' is not a card selector"),
        };
    }

    private static AbilityCardQuery Query(string name, AbilityLocation location) => name switch
    {
        "villain" => AbilityCardQuery.Villain,
        "mainScheme" => AbilityCardQuery.MainScheme,
        "yourAsideMinion" => AbilityCardQuery.YourAsideMinion,
        "yourAsideSideScheme" => AbilityCardQuery.YourAsideSideScheme,
        "minionsEngagedWithYou" => AbilityCardQuery.MinionsEngagedWithYou,
        "identitiesWithinPerPlayerLimit" => AbilityCardQuery.IdentitiesWithinPerPlayerLimit,
        "attachedToThis" => AbilityCardQuery.AttachedToThis,
        "heroesAndAllies" => AbilityCardQuery.HeroesAndAllies,
        "sideSchemes" => AbilityCardQuery.SideSchemes,
        "minions" => AbilityCardQuery.Minions,
        "enemies" => AbilityCardQuery.Enemies,
        "attackableEnemies" => AbilityCardQuery.AttackableEnemies,
        "attackableMinions" => AbilityCardQuery.AttackableMinions,
        "schemes" => AbilityCardQuery.Schemes,
        "thwartableSchemes" => AbilityCardQuery.ThwartableSchemes,
        "powerTargets" => AbilityCardQuery.PowerTargets,
        "yourAsidePile" => AbilityCardQuery.YourAsidePile,
        "upgradesAndSupportsYouControl" => AbilityCardQuery.UpgradesAndSupportsYouControl,
        "identitySpecificInYourHand" => AbilityCardQuery.IdentitySpecificInYourHand,
        "supportsYouControl" => AbilityCardQuery.SupportsYouControl,
        "charactersYouControl" => AbilityCardQuery.CharactersYouControl,
        "upgradesYouControl" => AbilityCardQuery.UpgradesYouControl,
        "blackPantherUpgrades" => AbilityCardQuery.BlackPantherUpgrades,
        "enemiesEngagedWithChosenPlayer" => AbilityCardQuery.EnemiesEngagedWithChosenPlayer,
        "alliesYouControl" => AbilityCardQuery.AlliesYouControl,
        "allies" => AbilityCardQuery.Allies,
        "heroes" => AbilityCardQuery.Heroes,
        "identities" => AbilityCardQuery.Identities,
        "identitiesWithTechInDiscard" => AbilityCardQuery.IdentitiesWithTechInDiscard,
        "topmostTechInChosenDiscard" => AbilityCardQuery.TopmostTechInChosenDiscard,
        "characters" => AbilityCardQuery.Characters,
        "drones" => AbilityCardQuery.Drones,
        "dronesEngagedWithYou" => AbilityCardQuery.DronesEngagedWithYou,
        _ => throw location.Error($"'{name}' is not a card query"),
    };

    private static AbilityCardSelection.WithTrait WithTrait(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "trait");
        return new(Cards(Required(fields, "cards", location), location.Child("cards")),
            Text(Required(fields, "trait", location), location.Child("trait")));
    }

    private static AbilityCardSelection.Ranked Ranked(
        AbilityValue value, AbilityLocation location, bool maximum)
    {
        var fields = Fields(value, location, "of", "by");
        string by = Text(Required(fields, "by", location), location.Child("by"));
        var rank = by switch
        {
            "cost" => AbilityCardRank.Cost,
            "attack" => AbilityCardRank.Attack,
            "printedHealth" => AbilityCardRank.PrintedHealth,
            _ => throw location.Child("by").Error($"'{by}' is not a card ranking"),
        };
        return new(Cards(Required(fields, "of", location), location.Child("of")), rank, maximum);
    }

    private static AbilityCardSelection.InAreas InAreas(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "area", "areas", "kind", "trait", "title");
        if (fields.ContainsKey("area") == fields.ContainsKey("areas"))
        {
            throw location.Error("expected exactly one of 'area' and 'areas'");
        }
        ImmutableArray<AbilitySearchArea> areas;
        if (fields.TryGetValue("area", out var area))
        {
            areas = [SearchArea(area, location.Child("area"))];
        }
        else
        {
            var several = Required(fields, "areas", location);
            if (several is not AbilityValue.List list)
            {
                throw location.Child("areas").Error("expected a list of search areas");
            }
            var builder = ImmutableArray.CreateBuilder<AbilitySearchArea>(list.Values.Count);
            for (int index = 0; index < list.Values.Count; index++)
            {
                builder.Add(SearchArea(list.Values[index], location.Child("areas").Item(index)));
            }
            areas = builder.MoveToImmutable();
        }
        CardKind? kind = null;
        if (fields.TryGetValue("kind", out var kindValue))
        {
            string name = Text(kindValue, location.Child("kind"));
            if (!Enum.TryParse<CardKind>(name, out var parsed)
                || !string.Equals(Enum.GetName(parsed), name, StringComparison.Ordinal))
            {
                throw location.Child("kind").Error($"'{name}' is not a printed card kind");
            }
            kind = parsed;
        }
        return new(areas, kind, OptionalText(fields, "trait", location), OptionalText(fields, "title", location));
    }

    private static AbilitySearchArea SearchArea(AbilityValue value, AbilityLocation location) =>
        Text(value, location) switch
        {
            "encounterDeck" => AbilitySearchArea.EncounterDeck,
            "encounterDiscardPile" => AbilitySearchArea.EncounterDiscardPile,
            "scenarioSetAside" => AbilitySearchArea.ScenarioSetAside,
            "yourDeck" => AbilitySearchArea.YourDeck,
            var name => throw location.Error($"'{name}' is not a supported search area"),
        };

    private static AbilityNode Operation(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.Map { Entries.Count: 1 } map)
        {
            throw location.Error("expected exactly one named operation");
        }
        var (kind, argument) = map.Entries.First();
        return new(kind, argument);
    }

    private static IReadOnlyDictionary<string, AbilityValue> Fields(
        AbilityValue value, AbilityLocation location, params string[] names)
    {
        if (value is not AbilityValue.Map map)
        {
            throw location.Error("expected named arguments");
        }
        foreach (string name in map.Entries.Keys)
        {
            if (!names.Contains(name, StringComparer.Ordinal))
            {
                throw location.Child(name).Error($"unknown argument '{name}'");
            }
        }
        return map.Entries;
    }

    private static AbilityValue Required(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        fields.TryGetValue(name, out var value)
            ? value
            : throw location.Child(name).Error($"missing argument '{name}'");

    private static string? OptionalText(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        fields.TryGetValue(name, out var value) ? Text(value, location.Child(name)) : null;
}
