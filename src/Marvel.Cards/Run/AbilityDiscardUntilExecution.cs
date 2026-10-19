using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Resolves a bounded encounter search and its distinct printed entry instruction.</summary>
internal static class AbilityDiscardUntilExecution
{
    internal static AbilityDeckAndRevealResult Run(
        AbilityEffect.DiscardUntil effect, AbilityDeckAndRevealContext context)
    {
        var found = EncounterDeck.DiscardUntil(context.World, context.World.Facts,
            effect.Kind, context.Trigger, context.Events, effect.Trait);
        if (found is null) return AbilityDeckAndRevealResult.Handled;
        switch (effect.Result)
        {
            case AbilityDiscardUntilResult.Reveal:
                int player = new AbilityExpressionEvaluation(context.Expressions,
                    new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(effect.Player);
                var revealContext = context with
                { Expressions = context.Expressions with { Bindings = context.Expressions.Bindings with { Player = player } } };
                return AbilityDeckAndRevealExecution.RevealCard(found, revealContext);
            case AbilityDiscardUntilResult.PutIntoPlayFirstPlayer:
                AbilityCardStateExecution.PutIntoPlay(found, context.World.FirstPlayer,
                    new(context.Expressions, context.Trigger, context.Events,
                        context.CardPlayAbilities, context.Readiness, new AbilityCardStateResult()));
                break;
            case AbilityDiscardUntilResult.AttachToThis:
                // rr:attach-to.3.1: another ability's specified host replaces the
                // attachment's printed "attach to" selection. This is not a reveal.
                Reveal.Resolve(context.World, context.World.Facts, found, context.World.FirstPlayer,
                    context.Events, placement: new CardEntryPlacement(context.Source.ObjectId, Verb: "Attach"));
                break;
            case AbilityDiscardUntilResult.PutIntoPlay:
                // rr:when-revealed-abilities.2: putting a card into play does not
                // resolve its When Revealed ability.
                Reveal.Resolve(context.World, context.World.Facts, found, context.World.FirstPlayer,
                    context.Events, placement: new CardEntryPlacement(Verb: "Put_Into_Play"));
                break;
            default:
                throw new InvalidOperationException("Unknown compiled discard-until result");
        }
        return AbilityDeckAndRevealResult.Handled;
    }
}
