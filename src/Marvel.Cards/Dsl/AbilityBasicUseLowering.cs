using Marvel.Rules.Play;
using Marvel.Cards.Run;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Admits the bounded matching ally receipt and refuses compound payment.</summary>
internal static class AbilityBasicUseLowering
{
    internal static void Validate(CardAbility ability, AbilityEffect effect, AbilityLocation location)
    {
        ValidateOverkill(ability, effect, location);
        var cost = ability.Cost is { } syntax
            ? AbilityCostLowering.LowerCost(AbilityBookLowering.Syntax(syntax), location.Child("cost")) : null;
        int matches = MatchingCount(effect);
        bool matching = matches != 0;
        bool capture = cost is AbilityCost.ExhaustChosen { CaptureBasicUsePower: true };
        if (matching != capture)
            throw location.Error("matching basic power requires its standalone selected ally cost and effect");
        if (matching && (matches != 1 || !SequentialMatch(effect)))
            throw location.Error("matching ally power supports one sequential application only");
        if (matching && (ability.Trigger.Event != BasicPowerTiming.HeroUseInitiated
            || ability.Trigger.Timing != AbilityType.Interrupt))
            throw location.Error("matching ally power requires the hero basic-use interrupt");
    }

    private static void ValidateOverkill(CardAbility ability, AbilityEffect effect, AbilityLocation location)
    {
        if (ContainsOverkill(effect) && (ability.Trigger.Event != BasicPowerTiming.HeroAttackInitiated
            || ability.Trigger.Timing != AbilityType.Interrupt))
            throw location.Error("basic-use overkill requires the hero basic-attack interrupt");
    }

    private static bool ContainsOverkill(AbilityEffect effect) =>
        effect is AbilityEffect.BoostBasicUse { Overkill: true }
        || AbilityEffectStructure.AllEffectChildren(effect).Any(ContainsOverkill);

    private static int MatchingCount(AbilityEffect effect) =>
        (effect is AbilityEffect.BoostBasicUse { MatchingAlly: true } ? 1 : 0)
        + AbilityEffectStructure.AllEffectChildren(effect).Sum(MatchingCount);

    private static bool SequentialMatch(AbilityEffect effect) => effect switch
    {
        AbilityEffect.BoostBasicUse { MatchingAlly: true } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(child =>
            MatchingCount(child) == 0 || SequentialMatch(child)),
        _ => false,
    };
}
