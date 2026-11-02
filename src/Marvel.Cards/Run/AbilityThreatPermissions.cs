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
            AbilityEffect.Conditional conditional => (TestCondition(conditional, selectors, expressions, projection)
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

    /// <summary>Whether an in-play threat-removal prohibition tests the presence of <paramref name="entering"/>'s title.</summary>
    /// <remarks>
    /// A power-state projection shows an entry from outside play only for a minion it
    /// started as unavailable. It cannot show any other entering card as present, so
    /// that entry raises instead.
    /// </remarks>
    internal static bool TestsPresenceOf(World world, AbilityProgram program, Card entering) =>
        world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type))
            .SelectMany(card => AbilityProgramQueries.ActiveOn(world, program, card))
            .Where(ability => ability.Trigger.Timing == AbilityType.Constant && HasProhibition(ability.Effect))
            .SelectMany(ability => TestedTitles(ability.Effect))
            .Any(title => entering.Faces.Any(face =>
                string.Equals(world.Facts.Title(face), title, StringComparison.Ordinal)));

    private static IEnumerable<string> TestedTitles(AbilityEffect? effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.SelectMany(TestedTitles),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.SelectMany(TestedTitles),
        AbilityEffect.Conditional conditional => TestedTitles(conditional.Test)
            .Concat(TestedTitles(conditional.Then)).Concat(TestedTitles(conditional.Else)),
        _ => [],
    };

    private static IEnumerable<string> TestedTitles(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.SelectMany(TestedTitles),
        AbilityCondition.Any any => any.Operands.SelectMany(TestedTitles),
        AbilityCondition.Negated negated => TestedTitles(negated.Operand),
        AbilityCondition.Exists { Cards: AbilityCardSelection.Titled titled } => [titled.Title],
        AbilityCondition.Exists { Cards: AbilityCardSelection.InAreas { Title: { } title } } => [title],
        _ => [],
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

    private static bool TestCondition(AbilityEffect.Conditional conditional, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions, AbilityThreatPermissionProjection? projection) =>
        projection?.Test(conditional.Test, selectors) ?? expressions.Test(conditional.Test);
}
