using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Evaluates constant permission to remove threat, preserving the removal cause.</summary>
internal static class AbilityThreatPermissions
{
    internal static bool CanRemoveThreat(
        World world, AbilityProgram program, Card scheme, int ignoredSource = -1, bool byThwarting = false, AbilityThreatPermissionProjection? projection = null)
    {
        foreach (var card in world.Cards.Where(card =>
            card.ObjectId != ignoredSource && (projection?.InPlay(card) ?? DeckTypes.IsInPlay(card.Area.Type))))
        {
            foreach (var ability in AbilityProgramQueries.ActiveOn(world, program, card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant && HasProhibition(ability.Effect)))
            {
                var bindings = new AbilityQueryContext(
                    world, card, new Occurrence(0, []),
                    AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                    null, null, null, []);
                var selectors = new AbilitySelectorEvaluation(bindings, null, program);
                var expressions = new AbilityExpressionEvaluation(
                    new AbilityExpressionContext(
                        bindings, ImmutableDictionary<string, long>.Empty, [],
                        string.Empty, -1, false, null), selectors);
                if (ProhibitsThreatRemoval(
                    ability.Effect, scheme, selectors, expressions, byThwarting,
                    projection?.ForSource(card))) return false;
            }
        }
        return true;
    }

    private static bool ProhibitsThreatRemoval(
        AbilityEffect effect, Card scheme, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions, bool byThwarting,
        AbilityThreatPermissionProjection? projection) => effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
                ProhibitsThreatRemoval(step, scheme, selectors, expressions, byThwarting, projection)),
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
                ProhibitsThreatRemoval(step, scheme, selectors, expressions, byThwarting, projection)),
            AbilityEffect.Conditional conditional => (TestCondition(conditional, expressions, projection)
                    ? conditional.Then : conditional.Else) is { } branch
                && ProhibitsThreatRemoval(branch, scheme, selectors, expressions, byThwarting, projection),
            AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.PreventThwarting } prohibition when byThwarting =>
                (projection is null ? selectors.Find(prohibition.Selection)
                    : projection.Find(prohibition.Selection))?.ObjectId == scheme.ObjectId,
            AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.PreventThreatRemoval } prohibition =>
                (projection is null ? selectors.Find(prohibition.Selection)
                    : projection.Find(prohibition.Selection))?.ObjectId == scheme.ObjectId,
            _ => false,
        };

    private static bool HasProhibition(AbilityEffect? effect) => effect switch
    {
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventThwarting
            or AbilityCardInstruction.PreventThreatRemoval } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.Any(HasProhibition),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(HasProhibition),
        AbilityEffect.Conditional conditional => HasProhibition(conditional.Then)
            || conditional.Else is { } alternate && HasProhibition(alternate),
        _ => false,
    };

    private static bool TestCondition(AbilityEffect.Conditional conditional,
        AbilityExpressionEvaluation expressions, AbilityThreatPermissionProjection? projection)
    {
        if (projection is not null)
            throw new Marvel.Rules.Play.RulesNotImplementedException(
                "projected conditional threat-removal permission requires projected predicate evaluation");
        return expressions.Test(conditional.Test);
    }
}
