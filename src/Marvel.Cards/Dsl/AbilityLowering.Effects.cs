using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    /// <summary>Lowers an effect and validates nested branches without executing them.</summary>
    public static AbilityEffect Effect(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        var node = Operation(value, location);
        var child = location.Child(node.Kind);
        var argument = node.Argument;
        return node.Kind switch
        {
            "seq" => new AbilityEffect.Sequence(Effects(argument, child)),
            "and" => new AbilityEffect.Simultaneous(Effects(argument, child)),
            "if" => ConditionalEffect(argument, child),
            "then" or "otherwise" => DependentEffect(argument, child, node.Kind),
            "eachPlayer" => new AbilityEffect.EachPlayer(OnlyEffect(argument, child)),
            "afterActivation" => new AbilityEffect.AfterActivation(OnlyEffect(argument, child)),
            "forEach" => RepeatedEffect(argument, child),
            "eachTime" => EachTimeEffect(argument, child),
            "choose" => ChooseEffect(argument, child),
            "chooseCard" => ChooseCardEffect(argument, child),
            "exhaust" => CardInstruction(argument, child, AbilityCardInstruction.Exhaust),
            "ready" => CardInstruction(argument, child, AbilityCardInstruction.Ready),
            "discard" => DiscardEffect(argument, child),
            "removeFromGame" => CardInstruction(argument, child, AbilityCardInstruction.RemoveFromGame),
            "returnToHand" => CardInstruction(argument, child, AbilityCardInstruction.ReturnToHand),
            "returnOwnedToHand" => CardInstruction(argument, child, AbilityCardInstruction.ReturnOwnedToHand),
            "addToHand" => CardInstruction(argument, child, AbilityCardInstruction.AddToHand),
            "reveal" => CardInstruction(argument, child, AbilityCardInstruction.Reveal),
            "attachTo" => CardInstruction(argument, child, AbilityCardInstruction.AttachTo),
            "preventThreatRemoval" => CardInstruction(argument, child, AbilityCardInstruction.PreventThreatRemoval),
            "preventReady" => CardInstruction(argument, child, AbilityCardInstruction.PreventReady),
            "giveAdditionalBoost" => FieldCardInstruction(argument, child, "enemy", AbilityCardInstruction.GiveAdditionalBoost),
            "soakDamage" => FieldCardInstruction(argument, child, "onto", AbilityCardInstruction.SoakDamage),
            "replaceThreatWithDamage" => FieldCardInstruction(argument, child, "card", AbilityCardInstruction.ReplaceThreatWithDamage),
            "resolveSpecials" => FieldCardInstruction(argument, child, "cards", AbilityCardInstruction.ResolveSpecials),
            "declareDefender" => FieldCardInstruction(argument, child, "card", AbilityCardInstruction.DeclareDefender),
            "heal" => HealEffect(argument, child),
            "dealDamage" => DamageEffect(argument, child),
            "dealAttackDamage" => AttackDamageEffect(argument, child),
            "moveDamage" or "moveAttackDamage" => MoveDamageEffect(argument, child, node.Kind == "moveAttackDamage"),
            "indirectDamage" => IndirectEffect(argument, child),
            "giveStatus" => StatusEffect(argument, child),
            "changeForm" => FormEffect(argument, child),
            "draw" => DrawEffect(argument, child),
            "drawToHandSize" or "drawToPrintedHandSize" => new AbilityEffect.DrawToHandSize(
                Player(argument, child), node.Kind == "drawToPrintedHandSize"),
            "placeThreat" => PlaceThreatEffect(argument, child),
            "removeThreat" => RemoveThreatEffect(argument, child),
            "preventThreat" => new AbilityEffect.PreventThreat(Number(argument, child)),
            "preventDamage" => PreventDamageEffect(argument, child),
            "gainSurge" => new AbilityEffect.GainSurge(Integer(argument, child)),
            "advanceMainScheme" => FixedEffect(argument, child, "next", AbilityFixedInstruction.AdvanceMainScheme),
            "cancelOccurrence" => FixedEffect(argument, child, "trigger", AbilityFixedInstruction.CancelOccurrence),
            "cancelWhenRevealed" => FixedEffect(argument, child, "trigger.subject", AbilityFixedInstruction.CancelWhenRevealed),
            "alsoAttackEachOtherHero" => FixedEffect(argument, child, "this", AbilityFixedInstruction.AlsoAttackEachOtherHero),
            "makeAttackIndirect" => FixedEffect(argument, child, 1, AbilityFixedInstruction.MakeAttackIndirect),
            "revealTop" => FixedEffect(argument, child, 1, AbilityFixedInstruction.RevealTop),
            "placeAccelerationToken" => FixedEffect(argument, child, 1, AbilityFixedInstruction.PlaceAccelerationToken),
            "generateTopDiscard" => FixedEffect(argument, child, "printed", AbilityFixedInstruction.GenerateTopDiscard),
            "makeTheCall" => FixedEffect(argument, child, "game", AbilityFixedInstruction.MakeTheCall),
            "requireAllyDefender" => FixedEffect(argument, child, "engagedPlayer", AbilityFixedInstruction.RequireAllyDefender),
            "generate" => new AbilityEffect.Generate(ResourceString(argument, child)),
            "doubleResourceFor" => new AbilityEffect.DoubleResourceFor(Text(argument, child)),
            "shuffle" => new AbilityEffect.Shuffle(SearchArea(argument, child)),
            "payOrEffect" or "payOrExhaust" => PayAlternative(argument, child, node.Kind == "payOrExhaust"),
            "grant" or "grantEach" or "grantUntil" => GrantEffect(argument, child, node.Kind == "grantEach", node.Kind == "grantUntil"),
            "grantCharactersControlledBy" => GrantControlledEffect(argument, child),
            "preventDamageFrom" => DamageProhibition(argument, child),
            "preventDamageWhile" => ConditionalDamageProhibition(argument, child),
            "delayUntil" => DelayedEffect(argument, child),
            "dealEncounterCards" => DealOrCreate(argument, child, drones: false),
            "createDrones" => DealOrCreate(argument, child, drones: true),
            "dealEncounterCard" => DealCardEffect(argument, child),
            "discardAtRandom" or "placeAtRandom" => RandomCardsEffect(argument, child, node.Kind == "placeAtRandom"),
            "discardTop" => DiscardTopEffect(argument, child),
            "discardUntil" => DiscardUntilEffect(argument, child),
            "shuffleInto" => ShuffleIntoEffect(argument, child),
            "search" => SearchEffect(argument, child),
            "putIntoPlay" => PutIntoPlayEffect(argument, child),
            "chooseTopForHand" or "chooseDiscardToShuffle" => ChoiceFromPile(argument, child, node.Kind == "chooseTopForHand"),
            "placeCounters" or "removeCounters" => CountersEffect(argument, child, node.Kind == "removeCounters"),
            "discardHandWithResource" => new AbilityEffect.DiscardHandWithResource(Resource(argument, child)),
            "recoverDiscardedByResource" => new AbilityEffect.RecoverDiscardedByResource(Resource(argument, child)),
            "reduceNextCardCost" => ReduceCostEffect(argument, child),
            "attack" => PowerEffect(argument, child, AbilityPowerKind.Attack),
            "defense" => PowerEffect(argument, child, AbilityPowerKind.Defense),
            "thwart" => PowerEffect(argument, child, AbilityPowerKind.Thwart),
            "thwartSchemes" => GroupThwartEffect(argument, child, AbilityThwartSelection.All),
            "thwartDifferentSchemes" => GroupThwartEffect(argument, child, AbilityThwartSelection.Different),
            "legalPractice" => GroupThwartEffect(argument, child, AbilityThwartSelection.LegalPractice),
            "enemyAttacks" or "enemySchemes" => ActivationEffect(argument, child, node.Kind == "enemyAttacks"),
            _ => throw child.Error($"'{node.Kind}' is not a supported effect operation"),
        };
    }

    private static ImmutableArray<AbilityEffect> Effects(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.List list)
        {
            throw location.Error("expected a list of effects");
        }
        var builder = ImmutableArray.CreateBuilder<AbilityEffect>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            builder.Add(Effect(list.Values[index], location.Item(index)));
        }
        return builder.MoveToImmutable();
    }

    private static AbilityEffect OnlyEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "effect");
        return Effect(Required(fields, "effect", location), location.Child("effect"));
    }

    private static AbilityEffect.Conditional ConditionalEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "test", "then", "else");
        return new(Condition(Required(fields, "test", location), location.Child("test")),
            OptionalEffect(fields, "then", location), OptionalEffect(fields, "else", location));
    }

    private static AbilityEffect? OptionalEffect(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        fields.TryGetValue(name, out var value) ? Effect(value, location.Child(name)) : null;

    private static AbilityEffect.Dependent DependentEffect(AbilityValue value, AbilityLocation location, string kind)
    {
        var fields = Fields(value, location, "effect", kind);
        return new(Effect(Required(fields, "effect", location), location.Child("effect")),
            Effect(Required(fields, kind, location), location.Child(kind)), kind == "then");
    }

    private static AbilityEffect.ForEach RepeatedEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "count", "effect");
        return new(Number(Required(fields, "count", location), location.Child("count")),
            Effect(Required(fields, "effect", location), location.Child("effect")));
    }

    private static AbilityEffect.EachTime EachTimeEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "effect", "when", "then");
        return new(Effect(Required(fields, "effect", location), location.Child("effect")),
            Condition(Required(fields, "when", location), location.Child("when")),
            Effect(Required(fields, "then", location), location.Child("then")));
    }

    private static AbilityEffect.Choose ChooseEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "options", "descriptions");
        var options = Effects(Required(fields, "options", location), location.Child("options"));
        if (options.Length < 2)
        {
            throw location.Child("options").Error("expected at least two choice options");
        }
        ImmutableArray<string> descriptions = [];
        if (fields.TryGetValue("descriptions", out var authored))
        {
            if (authored is not AbilityValue.List list || list.Values.Count != options.Length)
            {
                throw location.Child("descriptions").Error("expected one description per option");
            }
            descriptions = [.. list.Values.Select((item, index) => Text(item, location.Child("descriptions").Item(index)))];
        }
        return new(options, descriptions);
    }

    private static AbilityEffect.ChooseCard ChooseCardEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "effect");
        return new(Selected(fields, "from", location),
            Effect(Required(fields, "effect", location), location.Child("effect")));
    }

    private static AbilityEffect.CardAction CardInstruction(
        AbilityValue value, AbilityLocation location, AbilityCardInstruction instruction) => new(instruction, Cards(value, location));

    private static AbilityEffect.CardAction FieldCardInstruction(
        AbilityValue value, AbilityLocation location, string field, AbilityCardInstruction instruction)
    {
        var fields = Fields(value, location, field);
        return new(instruction, Selected(fields, field, location));
    }

    private static AbilityEffect.CardAction DiscardEffect(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Map map && map.Entries.ContainsKey("card")
            ? FieldCardInstruction(value, location, "card", AbilityCardInstruction.Discard)
            : CardInstruction(value, location, AbilityCardInstruction.Discard);

    private static AbilityCardSelection Selected(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        Cards(Required(fields, name, location), location.Child(name));

    private static AbilityNumber Numeric(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        Number(Required(fields, name, location), location.Child(name));

    private static AbilityEffect.Heal HealEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "amount");
        return new(Selected(fields, "card", location), Numeric(fields, "amount", location));
    }

    private static AbilityEffect.Damage DamageEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "amount", "attack");
        return new(Selected(fields, "cards", location), Numeric(fields, "amount", location), Marker(fields, "attack", location));
    }

    private static AbilityEffect.AttackDamage AttackDamageEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "amount", "overkill");
        return new(Selected(fields, "cards", location), Numeric(fields, "amount", location), Marker(fields, "overkill", location));
    }

    private static AbilityEffect.MoveDamage MoveDamageEffect(AbilityValue value, AbilityLocation location, bool attack)
    {
        var fields = Fields(value, location, "from", "to", "amount");
        return new(Selected(fields, "from", location), Selected(fields, "to", location), Numeric(fields, "amount", location), attack);
    }

    private static AbilityEffect.IndirectDamage IndirectEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "among", "amount");
        return new(Selected(fields, "among", location), Numeric(fields, "amount", location));
    }

    private static AbilityEffect.GiveStatus StatusEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "status");
        string status = Text(Required(fields, "status", location), location.Child("status"));
        if (status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
        {
            throw location.Child("status").Error($"'{status}' is not a supported status");
        }
        return new(Selected(fields, "card", location), status);
    }

    private static AbilityEffect.ChangeForm FormEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "to");
        string form = Text(Required(fields, "to", location), location.Child("to"));
        if (form is not ("hero" or "alter-ego"))
        {
            throw location.Child("to").Error($"'{form}' is not a supported form");
        }
        return new(Player(Required(fields, "player", location), location.Child("player")), form);
    }

    private static AbilityEffect.Draw DrawEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "count");
        return new(Players(Required(fields, "player", location), location.Child("player")),
            NonnegativeCount(Required(fields, "count", location), location.Child("count")));
    }

    private static AbilityPlayerSelection Players(AbilityValue value, AbilityLocation location) =>
        value is AbilityValue.Word { Value: "each" } ? new AbilityPlayerSelection.AllPlayers()
            : new AbilityPlayerSelection.OnePlayer(Player(value, location));

    private static int NonnegativeCount(AbilityValue value, AbilityLocation location)
    {
        long number = Integer(value, location);
        return number is >= 0 and <= int.MaxValue ? (int)number : throw location.Error("expected a nonnegative engine-sized count");
    }

    private static AbilityEffect.PlaceThreat PlaceThreatEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "scheme", "amount");
        return new(Selected(fields, "scheme", location), Numeric(fields, "amount", location));
    }

    private static AbilityEffect.RemoveThreat RemoveThreatEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "scheme", "amount", "ignoresCrisis", "overridesCannotFrom");
        return new(Selected(fields, "scheme", location), Numeric(fields, "amount", location),
            Boolean(fields, "ignoresCrisis", location),
            fields.TryGetValue("overridesCannotFrom", out var source) ? Cards(source, location.Child("overridesCannotFrom")) : null);
    }

    private static AbilityEffect.PreventDamage PreventDamageEffect(AbilityValue value, AbilityLocation location)
    {
        if (value is AbilityValue.Word)
        {
            FixedWord(value, location, "trigger.target");
            return new(new AbilityNumber.Constant(long.MaxValue));
        }
        var fields = Fields(value, location, "card", "amount");
        FixedWord(Required(fields, "card", location), location.Child("card"), "trigger.target");
        return new(fields.ContainsKey("amount") ? Numeric(fields, "amount", location) : new AbilityNumber.Constant(long.MaxValue));
    }

    private static bool Marker(IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location)
    {
        if (!fields.TryGetValue(name, out var value))
        {
            return false;
        }
        if (Integer(value, location.Child(name)) != 1)
        {
            throw location.Child(name).Error("expected the marker 1");
        }
        return true;
    }

    private static bool Boolean(IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        fields.TryGetValue(name, out var value) ? Text(value, location.Child(name)) switch
        {
            "true" => true,
            "false" => false,
            _ => throw location.Child(name).Error("expected 'true' or 'false'"),
        } : false;

    private static AbilityEffect.Fixed FixedEffect(
        AbilityValue value, AbilityLocation location, string expected, AbilityFixedInstruction instruction)
    {
        FixedWord(value, location, expected);
        return new(instruction);
    }

    private static AbilityEffect.Fixed FixedEffect(
        AbilityValue value, AbilityLocation location, long expected, AbilityFixedInstruction instruction)
    {
        if (Integer(value, location) != expected)
        {
            throw location.Error($"expected the marker {expected}");
        }
        return new(instruction);
    }

    private static AbilityEffect.PayOrEffect PayAlternative(AbilityValue value, AbilityLocation location, bool exhaustOnly)
    {
        var fields = Fields(value, location, "resources", "otherwise");
        var otherwise = Effect(Required(fields, "otherwise", location), location.Child("otherwise"));
        if (exhaustOnly && otherwise is not AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust })
        {
            throw location.Child("otherwise").Error("payOrExhaust requires an exhaust alternative");
        }
        return new(ResourceString(Required(fields, "resources", location), location.Child("resources")), otherwise, exhaustOnly);
    }
}
