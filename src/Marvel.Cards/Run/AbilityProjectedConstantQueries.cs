using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Refuses projected reads whose constant definitions or modifiers need changed inputs.
internal sealed class AbilityProjectedConstantQueries(AreaProjectionState state)
{
    internal bool MayModify(
        AbilityAdmissionContext current, Card target, string field)
    {
        var sources = current.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Concat(state.Entered.Select(id => current.World.Cards[id]))
            .Where(source => !state.Departed.Contains(source.ObjectId))
            .DistinctBy(source => source.ObjectId);
        return sources
            .Any(source => AbilityProgramQueries.On(current.Program, source)
                .Where(ability =>
                    ability.Trigger.Timing == AbilityType.Constant)
                .Any(ability => ConditionalGrant(
                    ability.Effect, field, ability.When is not null,
                    source, target, current)));
    }

    private bool ConditionalGrant(
        AbilityEffect effect, string field, bool conditioned,
        Card source, Card target, AbilityAdmissionContext current)
    {
        if (effect is AbilityEffect.Conditional conditional)
            return ConditionalBranchesGrant(conditional, field, source, target, current);
        if (effect is AbilityEffect.DefineBaseValue definition)
            return DefinesDynamicBase(definition, field, source, target);
        if (effect is AbilityEffect.GrantField { Until: null } grant
            && string.Equals(grant.Field, field, StringComparison.Ordinal))
        {
            bool dynamicAmount = grant.Amount is not AbilityNumber.Constant;
            return (conditioned || dynamicAmount)
                && GrantCouldAffect(grant.Cards, source, target, current);
        }
        if (effect is AbilityEffect.Sequence sequence)
        {
            return sequence.Effects.Any(child =>
                ConditionalGrant(
                    child, field, conditioned, source, target, current));
        }
        if (effect is AbilityEffect.Simultaneous simultaneous)
        {
            return simultaneous.Effects.Any(child =>
                ConditionalGrant(
                    child, field, conditioned, source, target, current));
        }
        return false;
    }

    private static bool DefinesDynamicBase(
        AbilityEffect.DefineBaseValue definition, string field, Card source, Card target) =>
        source.ObjectId == target.ObjectId && definition.Field == field
        && definition.Value is not AbilityNumber.Constant;

    private bool ConditionalBranchesGrant(
        AbilityEffect.Conditional conditional, string field,
        Card source, Card target, AbilityAdmissionContext current) =>
        conditional.Then is { } then && ConditionalGrant(
            then, field, conditioned: true, source, target, current)
        || conditional.Else is { } otherwise && ConditionalGrant(
            otherwise, field, conditioned: true, source, target, current);

    private bool GrantCouldAffect(
        AbilityCardSelection selector, Card source, Card target, AbilityAdmissionContext current)
    {
        if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This })
        {
            return source.ObjectId == target.ObjectId;
        }
        if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo })
        {
            return state.Hosts.GetValueOrDefault(
                source.ObjectId, source.Area.Host) == target.ObjectId;
        }
        if (selector is AbilityCardSelection.Titled titled)
        {
            return string.Equals(
                current.World.Facts.Title(target.FaceId),
                titled.Title, StringComparison.Ordinal);
        }
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain })
        {
            return CardKinds.IsVillain(
                EffectiveCards.Kind(target, current.World.Facts));
        }
        // Other selectors may change membership with the projected facts.
        // Failing closed is required until that membership is projected.
        return true;
    }
}
