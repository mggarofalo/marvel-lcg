using System.Collections.Immutable;
using System.Globalization;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Retains actual attacked players across one suspended activation collection.</summary>
internal static class AbilityActivationReceipts
{
    private const string Prefix = "activationAttackPlayer:";

    // Internal memory spelling is an engine choice; no authored player ids are accepted.
    internal static string PlayerKey(int player) => Prefix + player.ToString(CultureInfo.InvariantCulture);

    internal static void Begin(IDictionary<string, long> results, bool dynamic)
    {
        if (dynamic && results.Keys.Any(key => key.StartsWith("dynamicActivation:", StringComparison.Ordinal)))
            return;
        foreach (string key in results.Keys.Where(key => key.StartsWith(Prefix, StringComparison.Ordinal)).ToArray())
            results.Remove(key);
    }

    internal static ImmutableDictionary<string, long> Record(
        ImmutableDictionary<string, long> results, EnemyActivation activation)
    {
        // rr:stun-stunned.6 replaces the attack; a canceled activation did not attack a player.
        if (!activation.Made || !activation.Attacking) return results;
        string key = PlayerKey(activation.Player);
        return results.SetItem(key, results.GetValueOrDefault(key) + 1);
    }
}
