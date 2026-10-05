using static Marvel.Cards.Run.AbilityContinuationCodec;
using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;

namespace Marvel.Cards.Run;

// Continuation wire data is intentionally decoded against the compiled program.
// Paths are engine-chosen save data, not an alternate executable syntax.

/// <summary>Owns the legacy continuation wire spelling and its authored-tree validation.</summary>
internal static class AbilityContinuationRestoration
{
    internal static RestoredContinuationState RestoreState(
        IReadOnlyList<Card> cards, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? values, int actor, string sourceFace)
    {
        Card At(int id, string name) => id >= 0 && id < cards.Count ? cards[id]
            : throw new RulesNotImplementedException($"'{sourceFace}' has invalid persisted {name} metadata");
        var raw = values ?? ImmutableDictionary<string, long>.Empty;
        var chosen = ChosenBinding(cards, raw, sourceFace);
        var crisis = AbilityContinuationWireCodec.CrisisIgnoringThwartOrdinals(raw, sourceFace);
        var results = raw.Where(pair => pair.Key is not PersistedChosen
            and not PersistedChosenArea and not PersistedChosenIncarnation
            and not PersistedSourceIncarnation
            && !AbilitySourceStateFacts.IsFact(pair.Key)
            && !pair.Key.StartsWith(CrisisIgnoringThwartPrefix, StringComparison.Ordinal))
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return new((discarded ?? []).Select(id => At(id, "discarded-card")).ToImmutableArray(),
            results,
            raw.TryGetValue(PersistedSourceIncarnation, out long sourceIncarnation)
                ? checked((int)sourceIncarnation) : -1,
            chosen, actor >= 0 ? At(actor, "ability-actor") : null, crisis,
            AbilitySourceStateFacts.Restore(raw));
    }

    internal static AbilityContinuationCardBinding? ChosenBinding(
        IReadOnlyList<Card> cards, IReadOnlyDictionary<string, long>? values, string sourceFace)
    {
        if (values?.TryGetValue(PersistedChosen, out long selected) != true) return null;
        if (selected < 0 || selected >= cards.Count)
            throw new RulesNotImplementedException($"'{sourceFace}' has invalid persisted chosen-card metadata");
        if (!values.TryGetValue(PersistedChosenArea, out long area)
            || !values.TryGetValue(PersistedChosenIncarnation, out long incarnation))
            throw new RulesNotImplementedException(
                $"'{sourceFace}' has persisted chosen-card metadata without target provenance");
        return new(cards[(int)selected].ObjectId, checked((int)area), checked((int)incarnation));
    }
}
