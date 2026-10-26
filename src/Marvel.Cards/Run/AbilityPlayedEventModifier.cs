using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Owns modifiers that last for one physical event play.</summary>
internal static class AbilityPlayedEventModifier
{
    // This duration marker is an engine choice. Discard completes exactly one
    // paid event play, including canceled plays, before recovery responses.
    private const string Lifetime = "paidEventCompletion";

    internal static Card? Target(AbilityAdmissionContext context)
    {
        var occurrence = context.Query.Occurrence;
        if (!occurrence.Is(EventPlayTiming.Initiated)
            || occurrence.Subject < 0 || occurrence.Subject >= context.World.Cards.Count) return null;
        var card = context.World.Cards[occurrence.Subject];
        return card.Area.Type == DeckType.RevealingArea
            && EffectiveCards.Kind(card, context.World.Facts) == CardKind.Event
            && card.Owner == context.Query.Player ? card : null;
    }

    internal static bool CanModify(AbilityEffect.ModifyPlayedEvent modifier, AbilityAdmissionContext context)
    {
        var card = Target(context);
        if (card is null) return false;
        return modifier.Kind != "eventThreatRemoval"
            || context.Program.On(card.FaceId).Any(ability => RemovesThreat(ability.Effect));
    }

    // FAQ 05011: prevention-only events have no threat-removal instance to modify.
    private static bool RemovesThreat(AbilityEffect effect) =>
        effect is AbilityEffect.RemoveThreat
        || AbilityEffectStructure.AllEffectChildren(effect).Any(RemovesThreat);

    internal static AbilityImmediateResult Run(
        AbilityEffect.ModifyPlayedEvent modifier, AbilityImmediateContext context)
    {
        var card = Target(context.Admission)
            ?? throw new RulesNotImplementedException("event modifier requires an active paid event initiation");
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, modifier.Kind, Amount: modifier.Amount,
            Card: context.Admission.Source.ObjectId, Affects: card.ObjectId,
            Lasts: new Duration(OnCondition: Lifetime, Uses: 1)));
        return new(true, true);
    }

    internal static void Complete(World world, Card card)
    {
        foreach (var effect in world.Effects.Registered.Where(effect =>
            effect.Affects == card.ObjectId && effect.Lasts?.OnCondition == Lifetime).ToArray())
            world.Effects.Use(effect);
    }
}
