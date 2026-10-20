using static Marvel.Cards.Run.AbilityExpressionAreaDependencies;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Collects area dependencies for selected-card effects and their quantities.</summary>
internal static class AbilitySelectionAreaDependencies
{
    private static readonly ImmutableArray<AbilityCardInstruction> SingularCardInstructions =
    [
        AbilityCardInstruction.AttachTo, AbilityCardInstruction.Exhaust,
        AbilityCardInstruction.RemoveFromGame, AbilityCardInstruction.Reveal,
        AbilityCardInstruction.RevealFromVillainDeck,
        AbilityCardInstruction.ReturnToHand, AbilityCardInstruction.AddToHand,
        AbilityCardInstruction.ReturnOwnedToHand, AbilityCardInstruction.Discard,
        AbilityCardInstruction.SoakDamage,
    ];

    internal static void Collect(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
            case AbilityEffect.CardAction action:
                if (SingularCardInstructions.Contains(action.Instruction))
                    CollectCardsInDependencies(action.Selection, context, areas);
                break;
            case AbilityEffect.Heal heal:
                CollectCardsInDependencies(heal.Card, context, areas);
                CollectSingularAreaDependencies(heal.Amount, context, areas);
                break;
            case AbilityEffect.MoveThreat move:
                CollectCardsInDependencies(move.From, context, areas);
                CollectCardsInDependencies(move.To, context, areas);
                CollectSingularAreaDependencies(move.Amount, context, areas);
                break;
            case AbilityEffect.SetHitPoints assignment:
                CollectCardsInDependencies(assignment.Card, context, areas);
                break;
            case AbilityEffect.MoveDamage move:
                CollectCardsInDependencies(move.From, context, areas);
                CollectCardsInDependencies(move.To, context, areas);
                CollectSingularAreaDependencies(move.Amount, context, areas);
                break;
            case AbilityEffect.RemoveThreat threat:
                CollectCardsInDependencies(threat.Schemes, context, areas);
                CollectSingularAreaDependencies(threat.Amount, context, areas);
                break;
            case AbilityEffect.GrantTrait or AbilityEffect.GrantField:
                CollectGrantDependencies(effect, context, areas);
                break;
            case AbilityEffect.PreventDamageWhile prevention:
                CollectPreventionDependencies(prevention, context, areas);
                break;
            default:
                throw new InvalidOperationException("Unknown selection area dependency");
        }
    }

    private static void CollectPreventionDependencies(
        AbilityEffect.PreventDamageWhile prevention, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        if (prevention.Target is { } target) CollectCardsInDependencies(target, context, areas);
        CollectSingularAreaDependencies(prevention.Condition, context, areas);
    }

    private static void CollectGrantDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        if (effect is AbilityEffect.GrantTrait trait)
        {
            if (trait.Until is not null)
                CollectCardsInDependencies(trait.Cards, context, areas);
            return;
        }
        var field = (AbilityEffect.GrantField)effect;
        if (field.Until is not null)
            CollectCardsInDependencies(field.Cards, context, areas);
        CollectSingularAreaDependencies(field.Amount, context, areas);
    }

}
