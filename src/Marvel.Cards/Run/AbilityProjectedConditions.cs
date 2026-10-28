using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;

namespace Marvel.Cards.Run;

// Evaluates conditions whose inputs are represented by an area projection.
internal static class AbilityProjectedConditions
{
    internal static bool? Test(
        AbilityCondition test, AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (test is AbilityCondition.InForm form)
        {
            if (form.Player is AbilityPlayer.ChosenPlayer or AbilityPlayer.FirstPlayer)
                return null;
            int player = context.Evaluator().Seat(form.Player);
            return AbilityAreaFormProjection.FormsOf(state, context, player).Contains(form.Form);
        }
        if (test is AbilityCondition.CardText { Property: AbilityCardTextProperty.Trait } trait)
            return ProjectedFind(trait.Card, state, context) is { } card
                && state.HasTrait(context, card, trait.Text);
        if (test is AbilityCondition.CardText
            { Property: AbilityCardTextProperty.Status } status)
        {
            return ProjectedStatusTest(status, state, context);
        }
        if (test is AbilityCondition.Negated negated)
        {
            return Test(negated.Operand, state, context) is { } inner
                ? !inner : null;
        }
        return test is AbilityCondition.All or AbilityCondition.Any
            ? ProjectedCompoundTest(test, state, context)
            : null;
    }

    private static bool? ProjectedStatusTest(
        AbilityCondition.CardText status, AreaProjectionState state,
        AbilityAdmissionContext context) =>
        ProjectedFind(status.Card, state, context) is { } target
            ? state.StatusOf(context, target, status.Text) > 0
            : null;

    private static bool? ProjectedCompoundTest(
        AbilityCondition test, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var operands = test is AbilityCondition.All all
            ? all.Operands : ((AbilityCondition.Any)test).Operands;
        var values = operands
            .Select(child => Test(child, state, context)).ToList();
        if (test is AbilityCondition.All && values.Any(value => value == false))
        {
            return false;
        }
        if (test is AbilityCondition.Any && values.Any(value => value == true))
        {
            return true;
        }
        return values.Any(value => value is null)
            ? null
            : test is AbilityCondition.All;
    }

}
