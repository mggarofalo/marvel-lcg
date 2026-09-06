using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Interprets the immutable constant portion of an ability program against the
// current board. Constant abilities have no occurrence to resolve, event sink,
// runtime registry, or continuation state.
internal sealed class AbilityConstantQueries
{
    private readonly AbilityProgram program;
    private readonly ImmutableHashSet<string> constantFaces;

    internal AbilityConstantQueries(AbilityProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        this.program = program;
        constantFaces = program.Abilities
            .Where(ability => ability.Trigger.Timing == AbilityType.Constant)
            .Select(ability => ability.Card)
            .ToImmutableHashSet(StringComparer.Ordinal);
    }

    internal IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // A player's printed card is blank while Ultron uses it facedown as a
        // Drone minion. Its face id remains underneath for the digest, so the
        // interpreter must use the runtime card kind rather than mistake that
        // id for active player-card text.
        if (!constantFaces.Contains(card.FaceId)) return [];

        var found = new List<ContinuousEffect>();
        foreach (var ability in AbilityProgramQueries.On(program, card))
        {
            if (ability.Trigger.Timing != AbilityType.Constant) continue;

            var bindings = new AbilityQueryContext(
                world, card, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                null, null, null, []);
            var selectors = new AbilitySelectorEvaluation(bindings, null, program);
            var expressions = new AbilityExpressionEvaluation(
                new AbilityExpressionContext(
                    bindings, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                    -1, false, null), selectors);
            Grants(ability.Effect, bindings, selectors, expressions, found);
        }

        return found;
    }

    private static void Grants(
        AbilityEffect effect, AbilityQueryContext bindings,
        AbilitySelectorEvaluation selectors, AbilityExpressionEvaluation expressions,
        List<ContinuousEffect> found)
    {
        switch (effect)
        {
            case AbilityEffect.Sequence sequence:
                foreach (var step in sequence.Effects)
                    Grants(step, bindings, selectors, expressions, found);
                break;
            case AbilityEffect.Simultaneous simultaneous:
                foreach (var step in simultaneous.Effects)
                    Grants(step, bindings, selectors, expressions, found);
                break;
            case AbilityEffect.Conditional conditional:
                if ((expressions.Test(conditional.Test) ? conditional.Then : conditional.Else) is { } taken)
                    Grants(taken, bindings, selectors, expressions, found);
                break;
            case AbilityEffect.GrantField { Until: null } grant:
                foreach (var target in ConstantTargets(grant.Cards, grant.EachCard, bindings, selectors))
                {
                    found.Add(new ContinuousEffect(
                        EffectSource.ConstantAbility, Kind: grant.Field,
                        Amount: expressions.Amount(grant.Amount), Card: bindings.Source.ObjectId,
                        Affects: target.ObjectId, Lasts: Duration.WhileInPlay));
                }
                break;
            case AbilityEffect.GrantTrait { Until: null } grant:
                foreach (var target in ConstantTargets(grant.Cards, grant.EachCard, bindings, selectors))
                {
                    found.Add(new ContinuousEffect(
                        EffectSource.ConstantAbility, Kind: Traits.Granted + grant.Trait,
                        Card: bindings.Source.ObjectId, Affects: target.ObjectId,
                        Lasts: Duration.WhileInPlay));
                }
                break;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventThreatRemoval }:
            case AbilityEffect.DoubleResourceFor:
            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RequireAllyDefender }:
            case AbilityEffect.PreventDamageFrom:
            case AbilityEffect.PreventDamageWhile:
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventReady }:
                // Context-specific prohibition and payment policies live in
                // AbilityProgramQueries, not in the continuous-effect list.
                break;
            default:
                throw new RulesNotImplementedException(
                    $"'{bindings.Source.FaceId}' cannot resolve {effect} as a constant ability");
        }
    }

    private static IReadOnlyList<Card> ConstantTargets(
        AbilityCardSelection selection, bool each, AbilityQueryContext bindings,
        AbilitySelectorEvaluation selectors)
    {
        if (each) return selectors.Every(selection);
        if (selectors.Find(selection) is { } target) return [target];
        if (selection is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo }) return [];
        throw new RulesNotImplementedException(
            $"'{bindings.Source.FaceId}' card {bindings.Source.ObjectId} in "
            + $"{bindings.Source.Area.Type} hosted by {bindings.Source.Area.Host} would grant "
            + "to a card that is not there");
    }
}
