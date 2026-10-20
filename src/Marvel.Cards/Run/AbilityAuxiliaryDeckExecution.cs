using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityAuxiliaryDeckExecution
{
    internal static AbilityDeckAndRevealResult Run(AbilityEffect effect, AbilityDeckAndRevealContext context)
    {
        if (effect is AbilityEffect.InitializeAuxiliaryDeck initialize)
        {
            var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
            var selected = evaluation.Result(evaluation.Every(initialize.Cards));
            foreach (var observation in selected.Information) context.World.RecordInformation(observation);
            AuxiliaryDeck.Initialize(context.World, initialize.Deck, selected.Value, context.Trigger, context.Events);
            return new(true, true, null, ImmutableDictionary<string, long>.Empty);
        }
        var reveal = (AbilityEffect.RevealAuxiliaryTop)effect;
        var card = AuxiliaryDeck.TakeTop(context.World, reveal.Deck, context.Trigger, context.Events);
        var request = AbilityEncounterReveal.Prepare(card, context);
        return new(true, request is not null, request, ImmutableDictionary<string, long>.Empty);
    }
}
