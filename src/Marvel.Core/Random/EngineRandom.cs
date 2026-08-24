namespace Marvel.Core.Random;

/// <summary>
/// The facade the game calls, mirroring <c>engine/lib/random.py</c>.
/// </summary>
/// <remarks>
/// Thin on purpose. It exists for one behaviour the raw generator must not
/// have — see <see cref="Choice2"/> — and so that game code has a single seam
/// to call rather than reaching for <see cref="MersenneTwister"/> directly.
/// </remarks>
public sealed class EngineRandom(uint seed)
{
    /// <summary>The underlying generator.</summary>
    public MersenneTwister Generator { get; } = new(seed);

    /// <summary>Re-seeds the generator.</summary>
    public void SetSeed(uint seed) => Generator.Seed(seed);

    /// <summary>One element, uniformly.</summary>
    public T Choice<T>(IReadOnlyList<T> items) => Generator.Choice(items);

    /// <summary>Shuffles in place, Fisher-Yates downward.</summary>
    public void Shuffle<T>(IList<T> items) => Generator.Shuffle(items);

    /// <summary>
    /// <paramref name="x"/> distinct elements — but selecting *every* element
    /// consumes no randomness.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <paramref name="x"/> equals the sequence length there is exactly
    /// one answer, so this returns the input order and draws nothing. That is
    /// inherited behaviour from the Python engine and it is reproduced
    /// deliberately: it is semantically right, and changing it would move the
    /// stream for every game that contains such a call.
    /// </para>
    /// <para>
    /// <c>datasets/rng/vectors.json</c> pins it — the <c>engine_choice2</c>
    /// cases record <c>words_consumed: 0</c> for exactly this shape. Without
    /// the short-circuit the values still come out right and the consumption
    /// does not, which desynchronises every draw after it.
    /// </para>
    /// </remarks>
    public List<T> Choice2<T>(IReadOnlyList<T> items, int x)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(x, items.Count);

        if (x == items.Count)
        {
            return [.. items];
        }

        return Generator.ChooseWithoutReplacement(items, x);
    }
}
