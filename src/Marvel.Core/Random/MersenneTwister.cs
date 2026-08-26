namespace Marvel.Core.Random;

/// <summary>
/// The cross-engine random number generator: MT19937, 32-bit, as published.
/// </summary>
/// <remarks>
/// <para>
/// MT19937 as Matsumoto and Nishimura published it. This is not "a" random
/// generator: a game is reproducible from its seed only if every draw comes out
/// in the same order on every machine, so the algorithm is part of the
/// contract and not an implementation detail.
/// </para>
/// <para>
/// <b>Nothing checks that this is really MT19937</b>, and something should:
/// the standard published test vector. MARVEL-251.
/// </para>
/// <para>
/// <b>There are no floating point numbers anywhere in this contract.</b>
/// Bounded integers come from the raw 32-bit output by bitmask rejection,
/// which sidesteps every cross-language question about double rounding and
/// eliminates modulo bias at the same time.
/// </para>
/// </remarks>
public sealed class MersenneTwister
{
    private const int N = 624;
    private const int M = 397;
    private const uint MatrixA = 0x9908B0DFu;
    private const uint UpperMask = 0x80000000u;
    private const uint LowerMask = 0x7FFFFFFFu;
    private const uint SeedMultiplier = 1812433253u;

    private readonly uint[] _mt = new uint[N];
    private int _index;

    /// <summary>Creates a generator seeded with <paramref name="seed"/>.</summary>
    public MersenneTwister(uint seed) => Seed(seed);

    /// <summary>How many 32-bit words this generator has produced.</summary>
    /// <remarks>
    /// Not part of the algorithm. It exists because the acceptance vectors
    /// record <c>words_consumed</c> per call, and consumption is as much a
    /// part of the contract as the values are: a rejected draw is *gone*, and
    /// an implementation that peeks or reuses one produces a correct-looking
    /// value and a diverging stream from then on.
    /// </remarks>
    public long WordsConsumed { get; private set; }

    /// <summary>
    /// Re-seeds with MT19937's <c>init_genrand</c>, which is the only seeding
    /// routine this contract uses.
    /// </summary>
    /// <remarks>
    /// MT19937 also defines <c>init_by_array</c>, and numpy picks between them
    /// by argument type. The contract always uses this one, so a C#
    /// implementation never has to reproduce that choice.
    /// </remarks>
    public void Seed(uint seed)
    {
        _mt[0] = seed;
        for (uint i = 1; i < N; i++)
        {
            // `>> 30` is the published constant (w - 2) and is easy to mistype
            // as `>> 31`. A wrong shift here produces a plausible stream that
            // matches nothing.
            _mt[i] = unchecked(SeedMultiplier * (_mt[i - 1] ^ (_mt[i - 1] >> 30)) + i);
        }

        // 624 means "exhausted", so the first extraction twists before
        // returning anything. Setting it to 0 yields a different stream.
        _index = N;
        WordsConsumed = 0;
    }

    private void Twist()
    {
        for (int i = 0; i < N; i++)
        {
            // Addition rather than OR. On these two masked operands the
            // results are identical; addition is what the published algorithm
            // says, so a reader checking against it finds what they expect.
            uint x = (_mt[i] & UpperMask) + (_mt[(i + 1) % N] & LowerMask);
            uint xA = x >> 1;
            if ((x & 1) != 0)
            {
                xA ^= MatrixA;
            }

            _mt[i] = _mt[(i + M) % N] ^ xA;
        }

        _index = 0;
    }

    /// <summary>One raw 32-bit word.</summary>
    public uint NextUInt32()
    {
        if (_index >= N)
        {
            Twist();
        }

        uint y = _mt[_index];
        y ^= y >> 11;
        y ^= (y << 7) & 0x9D2C5680u;
        y ^= (y << 15) & 0xEFC60000u;
        y ^= y >> 18;

        _index++;
        WordsConsumed++;
        return y;
    }

    /// <summary>
    /// A uniform integer in <c>[0, n)</c>, for <c>1 &lt;= n &lt;= 2^32</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bitmask with rejection: <c>mask</c> is the smallest <c>2^k - 1</c> at
    /// least <c>n - 1</c>, and words are drawn until one falls below
    /// <paramref name="n"/>.
    /// </para>
    /// <para>
    /// <b><paramref name="n"/> is 64-bit on purpose.</b> The upper bound is
    /// 2^32, one more than <see cref="uint.MaxValue"/>. A <c>uint</c>
    /// parameter wraps that to 0; the mask coincidentally still comes out
    /// <c>0xFFFFFFFF</c>, but <c>value &lt; n</c> becomes <c>value &lt; 0</c>,
    /// which is never true on an unsigned type — so the loop spins forever.
    /// The vectors cover <c>n = 2^32</c>, so getting this wrong fails the
    /// fixture by hanging rather than by returning a wrong answer.
    /// </para>
    /// <para>
    /// Nothing is special-cased. <c>n = 1</c> still consumes a word, because
    /// the stream position must match; powers of two simply never reject.
    /// </para>
    /// </remarks>
    public uint NextBelow(long n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, 1L << 32);

        uint mask = (uint)(n - 1);
        mask |= mask >> 1;
        mask |= mask >> 2;
        mask |= mask >> 4;
        mask |= mask >> 8;
        mask |= mask >> 16;

        while (true)
        {
            uint value = NextUInt32() & mask;
            if (value < n)
            {
                return value;
            }
        }
    }

    /// <summary>Fisher-Yates, <b>downward</b>, in place.</summary>
    /// <remarks>
    /// Upward and downward Fisher-Yates both produce uniform permutations, but
    /// from the same stream they produce *different* ones — so the direction is
    /// part of the contract, not an implementation detail. A list of length 0
    /// or 1 consumes nothing.
    /// </remarks>
    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (int i = items.Count - 1; i >= 1; i--)
        {
            uint j = NextBelow(i + 1);
            (items[i], items[(int)j]) = (items[(int)j], items[i]);
        }
    }

    /// <summary>One element, uniformly.</summary>
    public T Choice<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("cannot choose from an empty sequence", nameof(items));
        }

        return items[(int)NextBelow(items.Count)];
    }

    /// <summary>
    /// <paramref name="k"/> distinct elements, by partial Fisher-Yates
    /// <b>upward</b> over a copy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This loop runs upward while <see cref="Shuffle"/> runs downward. That is
    /// deliberate — a partial shuffle has to fill from the front — and the
    /// difference is observable in the stream.
    /// </para>
    /// <para>
    /// <paramref name="k"/> outside <c>[0, count]</c> is an error and is
    /// <b>rejected, not clamped</b>. The vectors cannot catch this, because
    /// they only record successful calls: an implementation that clamped would
    /// pass every one of them and then quietly hand the game the wrong number
    /// of targets.
    /// </para>
    /// </remarks>
    public List<T> ChooseWithoutReplacement<T>(IReadOnlyList<T> items, int k)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(k);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(k, items.Count);

        var pool = new List<T>(items);
        var result = new List<T>(k);
        for (int i = 0; i < k; i++)
        {
            int j = i + (int)NextBelow(pool.Count - i);
            (pool[i], pool[j]) = (pool[j], pool[i]);
            result.Add(pool[i]);
        }

        return result;
    }

    /// <summary>The whole generator state; nothing else affects future output.</summary>
    /// <remarks>
    /// The array is copied. Handing out the live one makes a "snapshot" that
    /// changes underneath whoever is holding it.
    /// </remarks>
    public MersenneTwisterState GetState() => new([.. _mt], _index);

    /// <summary>Restores a state captured by <see cref="GetState"/>.</summary>
    public void SetState(MersenneTwisterState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Words.Count != N)
        {
            throw new ArgumentException($"state must hold {N} words", nameof(state));
        }

        for (int i = 0; i < N; i++)
        {
            _mt[i] = state.Words[i];
        }

        _index = state.Index;
    }
}

/// <summary>A captured <see cref="MersenneTwister"/> state.</summary>
public sealed class MersenneTwisterState(IReadOnlyList<uint> words, int index)
{
    /// <summary>The 624 state words.</summary>
    public IReadOnlyList<uint> Words { get; } = words;

    /// <summary>The extraction index, <c>0..624</c>.</summary>
    public int Index { get; } = index;
}
