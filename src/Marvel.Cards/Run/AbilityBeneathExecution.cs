using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityBeneathExecution
{
    internal static AbilityDeckAndRevealResult Run(AbilityEffect.StoreBeneath store, AbilityDeckAndRevealContext context)
    {
        var selection = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var hostResult = selection.Result(selection.Find(store.Host));
        var cardsResult = selection.Result(selection.Every(store.Cards));
        foreach (var observation in hostResult.Information.Concat(cardsResult.Information))
            context.World.RecordInformation(observation);
        var host = hostResult.Value ?? throw new RulesNotImplementedException("stored cards have no bound host");
        var cards = cardsResult.Value.ToList();
        int count = Math.Min(store.RandomCount ?? cards.Count, cards.Count);
        for (int index = 0; index < count; index++)
        {
            Card card = store.RandomCount is null ? cards[0] : context.World.Random.Choice(cards);
            cards.Remove(card);
            BeneathCards.Place(context.World, card, host, store.FaceUp, context.Trigger, context.Events);
        }
        return new(true, count > 0, null, ImmutableDictionary<string, long>.Empty);
    }
}
