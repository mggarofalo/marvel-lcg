using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Marvel.Session;

/// <summary>Captures journal values without retaining mutable engine buffers.</summary>
internal static class JournalValues
{
    internal static IReadOnlyList<T> List<T>(IEnumerable<T> values) => values.ToImmutableArray();

    internal static IReadOnlyList<T>? OptionalList<T>(IEnumerable<T>? values) =>
        values is null ? null : List(values);

    internal static IReadOnlyList<IReadOnlyList<T>>? Groups<T>(IReadOnlyList<IReadOnlyList<T>>? values) =>
        values is null ? null : List(values.Select(List));

    // Insertion order is retained because journal JSON is compared in domain order.
    // Only this private copy backs the read-only wrapper; no caller can mutate it.
    internal static IReadOnlyDictionary<TKey, TValue>? Map<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? values) where TKey : notnull =>
        values is null ? null : new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(values));
}
