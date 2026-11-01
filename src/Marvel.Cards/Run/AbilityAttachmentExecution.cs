using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityCardActionTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityAttachmentExecution
{
    internal static TargetLegality Target(AbilityEffect.AttachCards effect, AbilityAdmissionScope context)
    {
        var host = Find(effect.Host, context);
        if (host is null || !DeckTypes.IsInPlay(host.Area.Type)) return TargetLegality.Invalid;
        return CardsLegality(Every(effect.Cards, context).Where(card => Eligible(card, host, context.World)));
    }

    internal static AbilityDeckAndRevealResult Run(AbilityEffect.AttachCards effect, AbilityDeckAndRevealContext context)
    {
        var selection = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var host = selection.Find(effect.Host);
        var cards = selection.Every(effect.Cards).ToArray();
        foreach (var observation in selection.Result(cards).Information) context.World.RecordInformation(observation);
        if (host is null || !DeckTypes.IsInPlay(host.Area.Type)) return AbilityDeckAndRevealResult.Handled;
        foreach (var card in cards.Where(card => Eligible(card, host, context.World)))
        {
            // An attachment already in play moves onto the named host without leaving play.
            if (DeckTypes.IsInPlay(card.Area.Type))
            {
                AttachmentTransfer.Move(context.World, card, host, context.Expressions.Source.FaceId, context.Events);
                continue;
            }
            // rr:attach-to.3.1: an ability naming a specific host replaces the
            // attachment's printed attach-to instruction. It is not a reveal.
            Reveal.Resolve(context.World, context.World.Facts, card, context.World.FirstPlayer,
                context.Events, placement: new CardEntryPlacement(host.ObjectId, Verb: "Attach"));
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    private static bool Eligible(Card card, Card host, World world) => card != host
        && card.Area.Host != host.ObjectId && EffectiveCards.Kind(card, world.Facts) == CardKind.Attachment;
}
