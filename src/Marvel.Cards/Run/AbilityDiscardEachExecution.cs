using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityCardStateExecution;

namespace Marvel.Cards.Run;

/// <summary>Discards every selected card in play.</summary>
internal static class AbilityDiscardEachExecution
{
    // rr:discard.3: "If multiple cards are discarded ... from play by a singular effect,
    // place those cards in the appropriate discard pile in any order." Engine choice:
    // the selection's order.
    internal static bool Run(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var target in Every(selection, context).ToArray())
        {
            if (DeckTypes.IsInPlay(target.Area.Type) && CanRemove(selection, target, context))
                Rules.Play.Discard.CardFromEffect(context.World, context.World.Facts, context.Source, target,
                    context.Trigger, context.Events);
        }
        return true;
    }
}
