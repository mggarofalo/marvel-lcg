using System.Collections.Immutable;
using System.Globalization;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    /// <summary>Validates all executable syntax before a book can enter gameplay.</summary>
    public static AbilityProgram Book(AbilityBook book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var abilities = ImmutableArray.CreateBuilder<CompiledCardAbility>(book.Abilities.Count);
        var effects = ImmutableDictionary.CreateBuilder<AbilityEffectAddress, AbilityEffect>();
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var ability in book.Abilities)
        {
            int ordinal = ordinals.GetValueOrDefault(ability.Card);
            ordinals[ability.Card] = ordinal + 1;
            var location = new AbilityLocation(ability.Card, ordinal, "effect");
            var effect = Effect(Syntax(ability.Effect), location);
            var address = new AbilityEffectAddress(ability.Card, ordinal, "effect");
            Index(effect, address, effects);
            abilities.Add(new CompiledCardAbility(ability.Card, ability.Name, ability.Trigger, effect,
                ability.Cost is { } cost ? Cost(Syntax(cost), location with { Path = "cost" }) : null,
                ability.When is { } when ? Condition(Syntax(when), location with { Path = "when" }) : null,
                ability.Limit, ability.AnyPlayer, ability.Labels?.ToImmutableArray() ?? [],
                ability.PrintedResources, ability.Maximum, address));
        }

        var attachments = ImmutableDictionary.CreateBuilder<string, AbilityCardSelection>(StringComparer.Ordinal);
        if (book.AttachTo is { } attachTo)
        {
            foreach (var (card, target) in attachTo.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                attachments.Add(card, Cards(target, new AbilityLocation(card, 0, "attachTo")));
            }
        }
        return new AbilityProgram(abilities.MoveToImmutable(), book.Authored.ToImmutableHashSet(StringComparer.Ordinal),
            attachments.ToImmutable(), book.ControlledByFirstPlayer?.ToImmutableHashSet(StringComparer.Ordinal)
                ?? ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            book.PlacementOnly?.ToImmutableHashSet(StringComparer.Ordinal) ?? ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            book.CounterPools?.ToImmutableDictionary(StringComparer.Ordinal)
                ?? ImmutableDictionary.Create<string, CardCounterPool>(StringComparer.Ordinal), effects.ToImmutable());
    }

    private static AbilityValue.Map Syntax(AbilityNode node) => new(
        new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [node.Kind] = node.Argument });

    private static void Index(AbilityEffect effect, AbilityEffectAddress address,
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
        switch (effect)
        {
            case AbilityEffect.Sequence sequence:
                Children(sequence.Effects, "seq");
                break;
            case AbilityEffect.Simultaneous simultaneous:
                Children(simultaneous.Effects, "and");
                break;
            case AbilityEffect.Conditional conditional:
                if (conditional.Then is { } then) Child(then, "if/then");
                if (conditional.Else is { } otherwise) Child(otherwise, "if/else");
                break;
            case AbilityEffect.Dependent dependent:
                string kind = dependent.OnFull ? "then" : "otherwise";
                Child(dependent.Effect, kind + "/effect");
                Child(dependent.Continuation, kind + "/" + kind);
                break;
            case AbilityEffect.EachPlayer eachPlayer:
                Child(eachPlayer.Effect, "eachPlayer/effect");
                break;
            case AbilityEffect.ForEach forEach:
                Child(forEach.Effect, "forEach/effect");
                break;
            case AbilityEffect.EachTime eachTime:
                Child(eachTime.Effect, "eachTime/effect");
                Child(eachTime.Then, "eachTime/then");
                break;
            case AbilityEffect.Choose choose:
                Children(choose.Options, "choose/options");
                break;
            case AbilityEffect.ChooseCard chooseCard:
                Child(chooseCard.Effect, "chooseCard/effect");
                break;
            case AbilityEffect.AfterActivation afterActivation:
                Child(afterActivation.Effect, "afterActivation/effect");
                break;
            case AbilityEffect.Power power:
                string powerKind = power.Kind switch
                {
                    AbilityPowerKind.Attack => "attack",
                    AbilityPowerKind.Defense => "defense",
                    AbilityPowerKind.Thwart => "thwart",
                    _ => throw new AbilityException("unknown compiled power kind"),
                };
                Child(power.Effect, powerKind + "/effect");
                break;
            case AbilityEffect.ThwartGroup group:
                string groupKind = group.Selection switch
                {
                    AbilityThwartSelection.All => "thwartSchemes",
                    AbilityThwartSelection.Different => "thwartDifferentSchemes",
                    AbilityThwartSelection.LegalPractice => "legalPractice",
                    _ => throw new AbilityException("unknown compiled thwart selection"),
                };
                Child(group.Thwart, groupKind + "/power");
                break;
            case AbilityEffect.PayOrEffect alternative:
                Child(alternative.Otherwise, (alternative.ExhaustOnly ? "payOrExhaust" : "payOrEffect") + "/otherwise");
                break;
        }
    }
}
