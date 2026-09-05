using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    private static AbilityEffect DealOrCreate(AbilityValue value, AbilityLocation location, bool drones)
    {
        var fields = Fields(value, location, "player", "count");
        var players = fields.TryGetValue("player", out var player)
            ? Players(player, location.Child("player"))
            : drones ? throw location.Child("player").Error("missing argument 'player'") : new AbilityPlayerSelection.AllPlayers();
        int count = fields.TryGetValue("count", out var amount) ? NonnegativeCount(amount, location.Child("count")) : 1;
        return drones ? new AbilityEffect.CreateDrones(players, count) : new AbilityEffect.DealEncounterCards(players, count);
    }

    private static AbilityEffect.DealEncounterCard DealCardEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "player");
        return new(Selected(fields, "card", location), Player(Required(fields, "player", location), location.Child("player")));
    }

    private static AbilityEffect RandomCardsEffect(AbilityValue value, AbilityLocation location, bool place)
    {
        var fields = place ? Fields(value, location, "player", "count", "on") : Fields(value, location, "player", "count");
        var players = Players(Required(fields, "player", location), location.Child("player"));
        var count = Numeric(fields, "count", location);
        return place ? new AbilityEffect.PlaceAtRandom(players, count, Selected(fields, "on", location))
            : new AbilityEffect.DiscardAtRandom(players, count);
    }

    private static AbilityEffect.DiscardTop DiscardTopEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "count", "player");
        var from = SearchArea(Required(fields, "from", location), location.Child("from"));
        if (from is not (AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck))
        {
            throw location.Child("from").Error("discardTop requires an encounter or player deck");
        }
        var players = fields.TryGetValue("player", out var player) ? Players(player, location.Child("player")) : null;
        if (players is not null && from != AbilitySearchArea.YourDeck)
        {
            throw location.Child("from").Error("a player-qualified discardTop requires 'yourDeck'");
        }
        return new(from, players, Numeric(fields, "count", location));
    }

    private static AbilityEffect.DiscardUntil DiscardUntilEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "kind", "trait", "then");
        FixedWord(Required(fields, "from", location), location.Child("from"), "encounterDeck");
        bool putIntoPlay = Text(Required(fields, "then", location), location.Child("then")) switch
        {
            "reveal" => false,
            "putIntoPlayFirstPlayer" => true,
            var name => throw location.Child("then").Error($"'{name}' is not a supported discard-until result"),
        };
        return new(ConditionCardKind(Required(fields, "kind", location), location.Child("kind")),
            OptionalText(fields, "trait", location), putIntoPlay);
    }

    private static AbilityEffect.ShuffleInto ShuffleIntoEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "deck");
        return new(Selected(fields, "cards", location), SearchArea(Required(fields, "deck", location), location.Child("deck")));
    }

    private static AbilityEffect.Search SearchEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "for", "in");
        if (Required(fields, "in", location) is not AbilityValue.List list)
        {
            throw location.Child("in").Error("expected an ordered list of search areas");
        }
        var builder = ImmutableArray.CreateBuilder<AbilitySearchArea>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            var at = location.Child("in").Item(index);
            var area = Operation(list.Values[index], at);
            if (Integer(area.Argument, at.Child(area.Kind)) != 1)
            {
                throw at.Child(area.Kind).Error("expected the search-area marker 1");
            }
            builder.Add(SearchArea(new AbilityValue.Word(area.Kind), at.Child(area.Kind)));
        }
        return new(Text(Required(fields, "for", location), location.Child("for")), builder.MoveToImmutable());
    }

    private static AbilityEffect.PutIntoPlay PutIntoPlayEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "where");
        bool printed = Text(Required(fields, "where", location), location.Child("where")) switch
        {
            "printedDestination" => true,
            "engagedWithYou" => false,
            var name => throw location.Child("where").Error($"'{name}' is not a supported play destination"),
        };
        return new(Selected(fields, "card", location), printed);
    }

    private static AbilityEffect ChoiceFromPile(AbilityValue value, AbilityLocation location, bool top)
    {
        string count = top ? "count" : "max";
        var fields = Fields(value, location, count);
        int maximum = NonnegativeCount(Required(fields, count, location), location.Child(count));
        return top ? new AbilityEffect.ChooseTopForHand(maximum) : new AbilityEffect.ChooseDiscardToShuffle(maximum);
    }

    private static AbilityEffect CountersEffect(AbilityValue value, AbilityLocation location, bool remove)
    {
        var fields = Fields(value, location, "card", "counter", "count");
        string counter = Text(Required(fields, "counter", location), location.Child("counter"));
        if (counter.Length == 0)
        {
            throw location.Child("counter").Error("expected a nonempty counter name");
        }
        var card = Selected(fields, "card", location);
        return remove ? new AbilityEffect.RemoveCounters(card, counter,
            PositiveInteger(Required(fields, "count", location), location.Child("count")))
            : new AbilityEffect.PlaceCounters(card, counter, Numeric(fields, "count", location));
    }

    private static AbilityEffect.ReduceNextCardCost ReduceCostEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "amount");
        return new(Player(Required(fields, "player", location), location.Child("player")), Numeric(fields, "amount", location));
    }

    private static AbilityEffect.Power PowerEffect(AbilityValue value, AbilityLocation location, AbilityPowerKind kind)
    {
        var fields = kind == AbilityPowerKind.Defense ? Fields(value, location, "effect")
            : kind == AbilityPowerKind.Thwart ? Fields(value, location, "target", "effect", "automaticTarget")
                : Fields(value, location, "target", "effect");
        bool automatic = Marker(fields, "automaticTarget", location);
        return new(kind, kind == AbilityPowerKind.Defense ? null : Selected(fields, "target", location),
            Effect(Required(fields, "effect", location), location.Child("effect")), automatic);
    }

    private static AbilityEffect.ThwartGroup GroupThwartEffect(AbilityValue value, AbilityLocation location, AbilityThwartSelection selection)
    {
        var fields = Fields(value, location, "schemes", "power");
        var power = Effect(Required(fields, "power", location), location.Child("power"));
        if (power is not AbilityEffect.Power { Kind: AbilityPowerKind.Thwart } thwart)
        {
            throw location.Child("power").Error("expected a thwart power");
        }
        return new(selection, Selected(fields, "schemes", location), thwart);
    }

    private static AbilityEffect.ActivateEnemies ActivationEffect(AbilityValue value, AbilityLocation location, bool attack)
    {
        var fields = Fields(value, location, "enemies", "against", "first", "dynamic");
        AbilityCardSelection? against = null;
        bool engagedHero = false;
        if (fields.TryGetValue("against", out var target))
        {
            engagedHero = target is AbilityValue.Word { Value: "engagedHero" };
            if (!engagedHero)
            {
                against = Cards(target, location.Child("against"));
            }
        }
        return new(attack, Selected(fields, "enemies", location), against, engagedHero,
            Boolean(fields, "first", location), Boolean(fields, "dynamic", location));
    }
}
