using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Risky Business villain-face replacements for imminent activations.</summary>
internal static class GreenGoblinActivationReplacement
{
    internal static bool Resolve(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        if (step.What is not (Steps.Attack or Steps.Scheme)) return false;

        Card villain = world.Cards[step.Subject];
        if (!DeckTypes.IsInPlay(villain.Area.Type)
            || EffectiveCards.Kind(villain, facts) != CardKind.EncounterVillain)
            return false;

        // The amounts and face pair are printed on 02001a–02003b. The pack's
        // new rules explicitly say these replacements perform no attack/scheme.
        (string environmentFace, string counter, long amount) = (step.What, villain.FaceId) switch
        {
            (Steps.Attack, "02001a") => ("02006a", "infamy", 1),
            (Steps.Attack, "02002a") => ("02006a", "infamy", 2),
            (Steps.Attack, "02003a") => ("02006a", "infamy", 3),
            (Steps.Scheme, "02001b" or "02002b") => ("02006b", "madness", -1),
            (Steps.Scheme, "02003b") => ("02006b", "madness", -2),
            _ => default,
        };
        if (environmentFace is null) return false;

        // A missing or wrong-side environment is an invalid Risky Business board.
        // Identify the printed card before changing state or appending an event.
        Card environment = world.Cards.SingleOrDefault(card =>
            card.FaceId == environmentFace
            && DeckTypes.IsInPlay(card.Area.Type)
            && EffectiveCards.Kind(card, facts) == CardKind.Environment)
            ?? throw new RulesNotImplementedException(
                $"{villain.FaceId} replacement requires in-play {environmentFace}");
        string field = "c_" + counter;
        long before = environment.Tokens.GetValueOrDefault(field);
        long after = Math.Max(0, checked(before + amount));
        environment.PlaceTokens(field, after - before);
        events.Add(new FieldSet(environment.ObjectId, field, before, after)
        {
            Trigger = step.What,
            Verb = amount > 0 ? "Place_Counter" : "Remove_Counter",
        });
        return true;
    }
}
