using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Prompt-scoped private inspection without moving or revealing deck cards.</summary>
internal static class AbilityEncounterInspection
{
    internal static IReadOnlyList<Card> Cards(World world, AbilityEffect.InspectEncounterTop effect) =>
        EncounterRouting.Deck(world).Cards.Reverse().Take(effect.Count).ToArray();

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.InspectEncounterTop effect)
    {
        var cards = Cards(context.Expressions.World, effect);
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            context.Trigger, "Inspect encounter cards", false,
            [new Affordance(0, "Acknowledge", context.Expressions.Source.ObjectId, context.Player, "Finish looking")
            { DisplayLabel = "Finish looking", CommitLabel = "Done" }])
        {
            PublicKind = PublicDecisionKind.CardLook,
            DisplayQuestion = "Top encounter cards",
            Description = "Look at these cards in top-to-bottom order. They stay in the deck in the same order.",
            ContextCardIds = [.. cards.Select(card => card.ObjectId)],
            CauseCardIds = [context.Expressions.Source.ObjectId],
            ExposesConcealedCandidates = true,
        };
    }

    internal static void Answer(AbilityResolutionState cast, AbilityEffect.InspectEncounterTop effect)
    {
        // rr:look-looked-at.1: looked-at cards remain part of their deck. The
        // pending decision admits no intervening game action before acknowledgement.
        cast.Results[effect.Result] = Cards(cast.World, effect)
            .Count(card => cast.World.Facts.Kind(card.FaceId) == CardKind.Treachery);
        cast.ResolveEffect();
    }
}
