using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Typed live inputs for immediate nodes. The context carries only the current
// expression/admission read model, occurrence identity, event output and the
// reveal-scoped keyword state; it has no interpreter or continuation access.
internal sealed record AbilityImmediateContext(
    AbilityAdmissionContext Admission, string Trigger, List<GameEvent> Events,
    HashSet<string> GainedKeywords, IEncounterCardAbilities EncounterAbilities);
