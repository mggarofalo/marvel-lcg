using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilityModifierLowering;
using static Marvel.Cards.Dsl.AbilityProcedureLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using System.Collections.Immutable;
using System.Globalization;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

internal static class AbilityBookLowering
{
    /// <summary>Validates all executable syntax before a book can enter gameplay.</summary>
    internal static AbilityProgram LowerBook(AbilityBook book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var abilities = ImmutableArray.CreateBuilder<CompiledCardAbility>(book.Abilities.Count);
        var effects = ImmutableDictionary.CreateBuilder<AbilityEffectAddress, AbilityEffect>();
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var ability in book.Abilities)
        {
            abilities.Add(CompileAbility(ability, ordinals, effects));
        }

        var attachments = LowerAttachments(book.AttachTo);
        var program = new AbilityProgram(abilities.MoveToImmutable(), book.Authored.ToImmutableHashSet(StringComparer.Ordinal),
            attachments, book.ControlledByFirstPlayer?.ToImmutableHashSet(StringComparer.Ordinal)
                ?? ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            book.PlacementOnly?.ToImmutableHashSet(StringComparer.Ordinal) ?? ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            book.CounterPools?.ToImmutableDictionary(StringComparer.Ordinal)
                ?? ImmutableDictionary.Create<string, CardCounterPool>(StringComparer.Ordinal), effects.ToImmutable())
        {
            SchemeDestinations = LowerSchemeDestinations(book),
            HandResourceForms = AbilityHandResourceForms.Lower(book),
        };
        AbilityLimit.ValidateGroups(program.Abilities);
        AbilityProfileReferences.Validate(book, program.Profiles);
        AbilityPrintedTextLowering.Validate(program);
        AbilityAllyLimitExclusionLowering.Validate(program);
        return program;
    }

    private static ImmutableDictionary<string, AbilitySchemeDestination> LowerSchemeDestinations(AbilityBook book)
    {
        var destinations = book.SchemeDestinations?.ToImmutableDictionary(StringComparer.Ordinal)
            ?? ImmutableDictionary.Create<string, AbilitySchemeDestination>(StringComparer.Ordinal);
        foreach (var (card, destination) in destinations.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!book.Authored.Contains(card) || !Enum.IsDefined(destination))
                throw new AbilityException($"card '{card}' has an unauthored or unsupported schemeDestination");
        }
        return destinations;
    }

    private static CompiledCardAbility CompileAbility(
        CardAbility ability, Dictionary<string, int> ordinals,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect>.Builder effects)
    {
        int ordinal = ordinals.GetValueOrDefault(ability.Card);
        ordinals[ability.Card] = ordinal + 1;
        var location = new AbilityLocation(ability.Card, ordinal, "effect");
        var effect = LowerEffect(Syntax(ability.Effect), location);
        AbilityStagedAttackLowering.ValidatePlacement(effect, location);
        var address = new AbilityEffectAddress(ability.Card, ordinal, "effect");
        // This ability's nodes are indexed apart first, so its validators read
        // them without scanning every earlier ability's nodes.
        var own = ImmutableDictionary.CreateBuilder<AbilityEffectAddress, AbilityEffect>();
        Index(effect, address, own);
        effects.AddRange(own);
        List<AbilityEffect> nodes = [.. own.Values];
        AbilityStoragePermissionLowering.Validate(ability, effect, location, nodes);
        AbilitySelfPlayCostLowering.Validate(ability, effect, location, nodes);
        AbilityAllyCostLowering.Validate(ability, effect, location, nodes);
        AbilityBasicUseLowering.Validate(ability, effect, location);
        ValidateTargetedSentences(nodes, location);
        AbilityAttackKeywordLowering.Validate(ability, ordinal, own, location);
        AbilityExcessDamageLowering.Validate(ability, effect, location, nodes);
        var compiledCost = ability.Cost is { } syntaxCost
            ? LowerCost(Syntax(syntaxCost), location with { Path = "cost" }) : null;
        AbilityDiscardChoiceLowering.Validate(ability, compiledCost, effect, location);
        AbilitySchemeChoiceLowering.Validate(ability, compiledCost, effect, location);
        AbilitySharedTraitPlayLowering.Validate(ability, compiledCost, effect, location, nodes);
        AbilityRevealMemoryLowering.Validate(ability, location, nodes);
        return new CompiledCardAbility(ability.Card, ability.Name, ability.Trigger, effect,
        compiledCost,
        ability.When is { } when ? LowerCondition(Syntax(when), location with { Path = "when" }) : null,
        ability.Limit, ability.AnyPlayer, ability.Labels?.ToImmutableArray() ?? [],
        ability.PrintedResources, ability.Maximum, address)
    {
        LimitPeriod = AbilityLimit.Validate(ability),
        LimitGroup = ability.LimitGroup,
        ResourceRestriction = AbilityResourceRestriction.Validate(ability),
        PrintedCostResource = AbilityPrintedCostResource.Validate(ability),
    };
    }

    // seqEachTargeted describes one listed option of a choice; anywhere else it
    // would claim a legality rule nothing reads.
    private static void ValidateTargetedSentences(List<AbilityEffect> nodes, AbilityLocation location)
    {
        var options = nodes.OfType<AbilityEffect.Choose>().SelectMany(choose => choose.Options).ToHashSet();
        if (nodes.OfType<AbilityEffect.Sequence>().Any(sequence => sequence.EverySentenceTargeted
            && !options.Any(option => ReferenceEquals(option, sequence))))
            throw location.Error("seqEachTargeted is admitted only as a listed choice option");
    }

    private static ImmutableDictionary<string, AbilityCardSelection> LowerAttachments(
        IReadOnlyDictionary<string, AbilityValue>? attachTo)
    {
        var attachments = ImmutableDictionary.CreateBuilder<string, AbilityCardSelection>(StringComparer.Ordinal);
        if (attachTo is null) return attachments.ToImmutable();
        foreach (var (card, target) in attachTo.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            attachments.Add(card, SelectCards(target, new AbilityLocation(card, 0, "attachTo")));
        }
        return attachments.ToImmutable();
    }

    internal static AbilityValue.Map Syntax(AbilityNode node) => new(
        new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [node.Kind] = node.Argument });

    internal static void Index(AbilityEffect effect, AbilityEffectAddress address,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect>.Builder effects)
    {
        effects.Add(address, effect);
        void Child(AbilityEffect child, string path) => Index(child, address with { Path = address.Path + "/" + path }, effects);
        void Children(ImmutableArray<AbilityEffect> children, string prefix)
        {
            for (int index = 0; index < children.Length; index++)
            {
                Child(children[index], prefix + "/" + index.ToString(CultureInfo.InvariantCulture));
            }
        }
        if (IndexCollections(effect, Child, Children)) return;
        if (IndexBranches(effect, Child)) return;
        IndexPowers(effect, Child);
    }

    private static bool IndexCollections(
        AbilityEffect effect, Action<AbilityEffect, string> child,
        Action<ImmutableArray<AbilityEffect>, string> children)
    {
        switch (effect)
        {
            case AbilityEffect.Sequence sequence:
                children(sequence.Effects, "seq"); return true;
            case AbilityEffect.Simultaneous simultaneous:
                children(simultaneous.Effects, "and"); return true;
            case AbilityEffect.Choose choose:
                children(choose.Options, "choose/options"); return true;
            default: return false;
        }
    }

    private static bool IndexBranches(
        AbilityEffect effect, Action<AbilityEffect, string> child)
    {
        if (effect is AbilityEffect.Conditional conditional)
        {
            IndexConditional(conditional, child);
            return true;
        }
        switch (effect)
        {
            case AbilityEffect.Dependent dependent:
                string kind = dependent.OnFull ? "then" : "otherwise";
                child(dependent.Effect, kind + "/effect");
                child(dependent.Continuation, kind + "/" + kind); return true;
            case AbilityEffect.EachPlayer eachPlayer:
                child(eachPlayer.Effect, "eachPlayer/effect"); return true;
            case AbilityEffect.ForEach forEach:
                child(forEach.Effect, "forEach/effect"); return true;
            case AbilityEffect.EachTime eachTime:
                child(eachTime.Effect, "eachTime/effect");
                child(eachTime.Then, "eachTime/then"); return true;
            case AbilityEffect.ChooseCard chooseCard:
                child(chooseCard.Effect, "chooseCard/effect"); return true;
            case AbilityEffect.AfterActivation afterActivation:
                child(afterActivation.Effect, "afterActivation/effect"); return true;
            default: return false;
        }
    }

    private static void IndexConditional(
        AbilityEffect.Conditional conditional, Action<AbilityEffect, string> child)
    {
        if (conditional.Then is { } then) child(then, "if/then");
        if (conditional.Else is { } otherwise) child(otherwise, "if/else");
    }

    private static void IndexPowers(
        AbilityEffect effect, Action<AbilityEffect, string> child)
    {
        switch (effect)
        {
            case AbilityEffect.Power power:
                string powerKind = power.Kind switch
                {
                    AbilityPowerKind.Attack => "attack",
                    AbilityPowerKind.Defense => "defense",
                    AbilityPowerKind.Thwart => "thwart",
                    _ => throw new AbilityException("unknown compiled power kind"),
                };
                child(power.Effect, powerKind + "/effect");
                break;
            case AbilityEffect.ThwartGroup group:
                string groupKind = group.Selection switch
                {
                    AbilityThwartSelection.All => "thwartSchemes",
                    AbilityThwartSelection.Distributed => "distributeThreat",
                    AbilityThwartSelection.Different => "thwartDifferentSchemes",
                    AbilityThwartSelection.LegalPractice => "legalPractice",
                    _ => throw new AbilityException("unknown compiled thwart selection"),
                };
                child(group.Thwart, groupKind + "/power");
                break;
            case AbilityEffect.PayOrEffect alternative:
                child(alternative.Otherwise, (alternative.ExhaustOnly ? "payOrExhaust" : "payOrEffect") + "/otherwise");
                break;
        }
    }
}
