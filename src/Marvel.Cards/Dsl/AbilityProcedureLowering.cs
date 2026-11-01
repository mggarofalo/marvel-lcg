using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilityModifierLowering;
using static Marvel.Cards.Dsl.AbilityProcedureLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

internal static class AbilityProcedureLowering
{
    internal static AbilityEffect DealEncounterCards(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "count");
        var players = fields.TryGetValue("player", out var player)
            ? Players(player, location.Child("player"))
            : new AbilityPlayerSelection.AllPlayers();
        int count = fields.TryGetValue("count", out var amount) ? NonnegativeCount(amount, location.Child("count")) : 1;
        return new AbilityEffect.DealEncounterCards(players, count);
    }

    internal static AbilityEffect.DealEncounterCard DealCardEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "player");
        return new(Selected(fields, "card", location), LowerPlayer(Required(fields, "player", location), location.Child("player")));
    }

    internal static AbilityEffect RandomCardsEffect(AbilityValue value, AbilityLocation location, bool place)
    {
        var fields = place ? Fields(value, location, "player", "count", "on") : Fields(value, location, "player", "count");
        var players = Players(Required(fields, "player", location), location.Child("player"));
        var count = Numeric(fields, "count", location);
        return place ? new AbilityEffect.PlaceAtRandom(players, count, Selected(fields, "on", location))
            : new AbilityEffect.DiscardAtRandom(players, count);
    }

    internal static AbilityEffect.DiscardTop DiscardTopEffect(AbilityValue value, AbilityLocation location)
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

    internal static AbilityEffect.DiscardUntil DiscardUntilEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "kind", "trait", "then", "player", "set", "revealStatus");
        var from = SearchArea(Required(fields, "from", location), location.Child("from"));
        if (from == AbilitySearchArea.YourDeck && fields.ContainsKey("revealStatus"))
            throw location.Child("revealStatus").Error("revealStatus requires an encounter-deck reveal");
        if (from == AbilitySearchArea.YourDeck)
            return PlayerDiscardUntil(fields, location);
        if (from != AbilitySearchArea.EncounterDeck)
            throw location.Error("discardUntil requires an encounter or player deck");
        return EncounterDiscardUntil(fields, location);
    }

    private static AbilityEffect.DiscardUntil EncounterDiscardUntil(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location)
    {
        bool set = fields.ContainsKey("set");
        var result = EncounterDiscardResult(fields, location);
        string? setName = AbilityEncounterDiscardLowering.Set(fields, location, result);
        CardKind? kind = set ? null : ConditionCardKind(Required(fields, "kind", location), location.Child("kind"));
        // rr:engage: "When a minion enters play in a player's play area, it engages
        // that player." Only a minion engages, so any other kind is refused.
        if (result == AbilityDiscardUntilResult.PutIntoPlayEngaged && kind != CardKind.Minion)
            throw location.Child("kind").Error("putIntoPlayEngaged requires kind 'minion'");
        return new(kind,
            OptionalText(fields, "trait", location), result)
        {
            Set = setName, Player = fields.TryGetValue("player", out var player) ? AbilityLowering.Player(player, location.Child("player")) : AbilityPlayer.You,
            RevealStatus = AbilityEncounterDiscardLowering.RevealStatus(fields, location, result),
        };
    }

    private static AbilityDiscardUntilResult EncounterDiscardResult(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location) =>
        Text(Required(fields, "then", location), location.Child("then")) switch
        {
            "reveal" => AbilityDiscardUntilResult.Reveal,
            "putIntoPlayFirstPlayer" => AbilityDiscardUntilResult.PutIntoPlayFirstPlayer,
            "attachToThis" => AbilityDiscardUntilResult.AttachToThis,
            "putIntoPlay" => AbilityDiscardUntilResult.PutIntoPlay,
            "putIntoPlayEngaged" when fields.ContainsKey("player") => AbilityDiscardUntilResult.PutIntoPlayEngaged,
            "putIntoPlayEngaged" => throw location.Error("putIntoPlayEngaged requires the engaged 'player'"),
            var name => throw location.Child("then").Error($"'{name}' is not a supported discard-until result"),
        };

    private static AbilityEffect.DiscardUntil PlayerDiscardUntil(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location)
    {
        bool identitySet = fields.ContainsKey("set");
        if (identitySet && (fields.ContainsKey("kind") || fields.ContainsKey("trait")))
            throw location.Error("player discardUntil requires either an identity set or a kind and optional trait");
        FixedWord(Required(fields, "then", location), location.Child("then"), "addToHand");
        return new(identitySet ? null : ConditionCardKind(Required(fields, "kind", location), location.Child("kind")),
            OptionalText(fields, "trait", location), AbilityDiscardUntilResult.AddToHand)
        {
            From = AbilitySearchArea.YourDeck,
            Set = OptionalText(fields, "set", location),
            Player = fields.TryGetValue("player", out var owner)
                ? AbilityLowering.Player(owner, location.Child("player")) : AbilityPlayer.You,
        };
    }

    internal static AbilityEffect.ShuffleInto ShuffleIntoEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "deck");
        var at = location.Child("deck");
        var deck = Text(Required(fields, "deck", location), at) switch
        {
            "yourDeck" => AbilityShuffleDestination.YourDeck,
            "encounterDeck" => AbilityShuffleDestination.EncounterDeck,
            "ownerDeck" => AbilityShuffleDestination.OwnerDeck,
            var name => throw at.Error($"'{name}' is not a supported shuffle destination"),
        };
        return new(Selected(fields, "cards", location), deck);
    }

    internal static AbilityEffect.Search SearchEffect(AbilityValue value, AbilityLocation location)
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

    internal static AbilityEffect.PutIntoPlay PutIntoPlayEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "where");
        string destination = Text(Required(fields, "where", location), location.Child("where"));
        bool printed = destination switch
        {
            "printedDestination" => true,
            "engagedWithYou" or "engagedWithFirstPlayer" => false,
            "yourPlayArea" => false,
            var name => throw location.Child("where").Error($"'{name}' is not a supported play destination"),
        };
        return new(Selected(fields, "card", location), printed)
        { AllyUnderYourControl = destination == "yourPlayArea", FirstPlayer = destination == "engagedWithFirstPlayer" };
    }

    internal static AbilityEffect ChoiceFromPile(AbilityValue value, AbilityLocation location, bool top)
    {
        string count = top ? "count" : "max";
        var fields = top ? Fields(value, location, count) : Fields(value, location, count, "min", "differentTitles", "resource", "set");
        int maximum = NonnegativeCount(Required(fields, count, location), location.Child(count));
        if (top) return new AbilityEffect.ChooseTopForHand(maximum);
        int minimum = fields.TryGetValue("min", out var min) ? NonnegativeCount(min, location.Child("min")) : 1;
        if (minimum > maximum) throw location.Error("minimum cannot exceed maximum");
        return new AbilityEffect.ChooseDiscardToShuffle(maximum)
        {
            Minimum = minimum,
            DifferentTitles = !fields.ContainsKey("differentTitles") || Boolean(fields, "differentTitles", location),
            Resource = fields.TryGetValue("resource", out var resource) ? Resource(resource, location.Child("resource")) : null,
            Set = OptionalText(fields, "set", location),
        };
    }

    internal static AbilityEffect CountersEffect(AbilityValue value, AbilityLocation location, bool remove)
    {
        var fields = Fields(value, location, "card", "counter", "count");
        string counter = Text(Required(fields, "counter", location), location.Child("counter"));
        if (counter.Length == 0)
        {
            throw location.Child("counter").Error("expected a nonempty counter name");
        }
        var card = Selected(fields, "card", location);
        return remove ? new AbilityCounterEffect.RemoveCounters(card, counter,
            PositiveInteger(Required(fields, "count", location), location.Child("count")))
            : new AbilityCounterEffect.PlaceCounters(card, counter, Numeric(fields, "count", location));
    }

    internal static AbilityPricingEffect.ReduceNextCardCost ReduceCostEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "amount", "kind", "trait", "until");
        bool any = Required(fields, "player", location) is AbilityValue.Word { Value: "any" };
        var kind = fields.TryGetValue("kind", out var cardKind)
            ? ConditionCardKind(cardKind, location.Child("kind")) : (Marvel.Rules.State.CardKind?)null;
        string? trait = OptionalText(fields, "trait", location);
        var filter = kind is null && trait is null ? null : new Marvel.Rules.Play.CardCostFilter(kind, trait);
        return new(any ? AbilityPlayer.You : LowerPlayer(fields["player"], location.Child("player")),
            Numeric(fields, "amount", location))
        {
            AnyPlayer = any,
            Filter = any ? new(kind, trait) : filter,
            Until = ReductionExpiry(fields, any, location),
        };
    }

    // Engine choice: "this turn" spells EndOfTurn; omitted, a reduction lasts the phase.
    private static string ReductionExpiry(
        IReadOnlyDictionary<string, AbilityValue> fields, bool any, AbilityLocation location)
    {
        string? until = OptionalText(fields, "until", location);
        if (until is not (null or Marvel.Rules.Timing.TimingPoints.EndOfTurn))
            throw location.Child("until").Error("a card cost reduction can only last until EndOfTurn");
        if (any && until is not null)
            throw location.Child("until").Error("a reduction for any player lasts the phase");
        return until ?? Marvel.Rules.Timing.TimingPoints.EndOfPlayerPhase;
    }

    internal static AbilityEffect.ThwartGroup DistributedThwart(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "schemes", "amount");
        int total = checked((int)PositiveInteger(Required(fields, "amount", location), location.Child("amount")));
        var targets = new AbilityCardSelection.Query(AbilityCardQuery.PowerTargets);
        var removal = new AbilityEffect.RemoveThreat(targets, new AbilityNumber.Constant(1), false, null)
            { Assigned = true };
        var power = new AbilityEffect.Power(AbilityPowerKind.Thwart,
            new AbilityCardSelection.Bound(AbilityCardBinding.Chosen), removal, false);
        return new(AbilityThwartSelection.Distributed, Selected(fields, "schemes", location), power)
            { DistributionTotal = total };
    }

    internal static AbilityEffect.ThwartGroup GroupThwartEffect(AbilityValue value, AbilityLocation location, AbilityThwartSelection selection)
    {
        var fields = Fields(value, location, "schemes", "power");
        var power = LowerEffect(Required(fields, "power", location), location.Child("power"));
        if (power is not AbilityEffect.Power { Kind: AbilityPowerKind.Thwart } thwart)
        {
            throw location.Child("power").Error("expected a thwart power");
        }
        return new(selection, Selected(fields, "schemes", location), thwart);
    }

    internal static AbilityEffect.ActivateEnemies ActivationEffect(AbilityValue value, AbilityLocation location, bool attack)
    {
        var fields = Fields(value, location, "enemies", "against", "first", "dynamic", "modifier", "dealBoostCards", "chooseEnemy");
        bool chooseOne = SelectOneAttacker(fields, location, attack);
        if (!attack && fields.ContainsKey("dealBoostCards"))
            throw location.Error("dealBoostCards is supported only for attacks");
        AbilityCardSelection? against = null;
        bool engagedHero = false;
        if (fields.TryGetValue("against", out var target))
        {
            engagedHero = target is AbilityValue.Word { Value: "engagedHero" };
            if (!engagedHero)
            {
                against = SelectCards(target, location.Child("against"));
            }
        }
        return new(attack, Selected(fields, "enemies", location), against, engagedHero,
            Boolean(fields, "first", location), Boolean(fields, "dynamic", location))
        {
            ChooseOneEnemy = chooseOne,
            DealBoostCards = !fields.ContainsKey("dealBoostCards") || Boolean(fields, "dealBoostCards", location),
            Modifier = fields.TryGetValue("modifier", out var modifier)
                ? Number(modifier, location.Child("modifier")) : null,
        };
    }

    private static bool SelectOneAttacker(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location, bool attack)
    {
        if (!fields.TryGetValue("chooseEnemy", out var chooser)) return false;
        FixedWord(chooser, location.Child("chooseEnemy"), "firstPlayer");
        if (!attack || Boolean(fields, "dynamic", location))
            throw location.Error("chooseEnemy requires a nondynamic enemy attack");
        return true;
    }
}
