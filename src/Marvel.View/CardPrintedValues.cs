using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Copies authoritative printed facts into the render-safe face contract.</summary>
internal static class CardPrintedValues
{
    internal static IReadOnlyDictionary<string, CardPrintedValue> From(
        IReadOnlyDictionary<string, PrintedStatValue> values) =>
        values.ToDictionary(pair => pair.Key, pair => new CardPrintedValue(
            pair.Value.Value, pair.Value.SpecialStar,
            pair.Value.PerPlayer, pair.Value.ConsequentialDamage), StringComparer.Ordinal);
}
