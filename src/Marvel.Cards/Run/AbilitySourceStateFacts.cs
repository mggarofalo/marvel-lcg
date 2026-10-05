using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Last-known source quantities retained by one ability continuation.</summary>
internal static class AbilitySourceStateFacts
{
    private const string Prefix = "__continuation.source_state.";

    internal static ImmutableDictionary<string, long> Capture(
        IReadOnlyDictionary<string, long> results, CardInstanceState state)
    {
        var facts = results.Where(pair => !IsFact(pair.Key))
            .ToImmutableDictionary(StringComparer.Ordinal)
            .SetItem(Prefix + "damage", state.Damage)
            .SetItem(Prefix + "ready", state.Ready ? 1 : 0);
        foreach (var (kind, count) in state.Tokens)
            facts = facts.SetItem(Prefix + "token." + kind, count);
        return facts;
    }

    internal static bool IsFact(string key) => key.StartsWith(Prefix, StringComparison.Ordinal);

    internal static CardInstanceState? Restore(IReadOnlyDictionary<string, long> facts)
    {
        if (!facts.TryGetValue(Prefix + "damage", out long damage)) return null;
        if (!facts.TryGetValue(Prefix + "ready", out long ready) || ready is not (0 or 1))
            throw new RulesNotImplementedException("invalid last-known source readiness");
        return CardInstanceState.FromFacts(damage, ready == 1,
            facts.Where(pair => pair.Key.StartsWith(Prefix + "token.", StringComparison.Ordinal))
                .ToImmutableDictionary(pair => pair.Key[(Prefix.Length + 6)..], pair => pair.Value, StringComparer.Ordinal));
    }
}
