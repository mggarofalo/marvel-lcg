using System.Text.Json;
using Marvel.Core.Random;
using Xunit;

namespace Marvel.Core.Tests.Random;

/// <summary>
/// The acceptance test for MARVEL-8: every vector in
/// <c>datasets/rng/vectors.json</c>, or the implementation is not compatible.
/// </summary>
/// <remarks>
/// <para>
/// These vectors were emitted by the Python engine that generated the frozen
/// corpus. So when C# and the fixture disagree, <b>C# is wrong</b> — the
/// fixture is what 1,773 recorded games were played against.
/// </para>
/// <para>
/// Every case asserts <c>words_consumed</c> as well as the values, because
/// consumption is as much of the contract as the results are. An
/// implementation can produce every correct value and still desynchronise the
/// stream, and from that point on nothing replays.
/// </para>
/// </remarks>
public sealed class RngVectorTests
{
    private static readonly JsonElement Cases = Load();

    private static JsonElement Load()
    {
        using var stream = File.OpenRead(RepositoryPaths.Dataset("rng", "vectors.json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("cases").Clone();
    }

    private static JsonElement.ArrayEnumerator CasesOf(string name) =>
        Cases.GetProperty(name).EnumerateArray();

    private static List<int> Sequence(int length) => [.. Enumerable.Range(0, length)];

    [Fact]
    public void TheFixtureIsPresentAndPopulated()
    {
        // Guards against the whole suite passing vacuously if the dataset moves.
        Assert.NotEmpty(CasesOf("next_uint32"));
        Assert.NotEmpty(CasesOf("shuffle"));
    }

    [Fact]
    public void NextUInt32MatchesEveryVector()
    {
        foreach (var vector in CasesOf("next_uint32"))
        {
            var rng = new MersenneTwister(vector.GetProperty("seed").GetUInt32());

            // One vector carries `skip: 623` so that its four reads straddle
            // the twist at word 624 — the boundary where an off-by-one in the
            // exhaustion check hides, because every word before it is right.
            if (vector.TryGetProperty("skip", out var skip))
            {
                for (int i = 0; i < skip.GetInt32(); i++)
                {
                    rng.NextUInt32();
                }
            }

            int count = vector.GetProperty("count").GetInt32();
            var actual = new List<uint>(count);
            for (int i = 0; i < count; i++)
            {
                actual.Add(rng.NextUInt32());
            }

            Assert.Equal(Expected(vector, x => x.GetUInt32()), actual);
        }
    }

    [Fact]
    public void TheReferenceVectorIsNumpys()
    {
        // Called out in docs/rng-contract.md as the check that the core is
        // unchanged from what the engine used to produce.
        var rng = new MersenneTwister(42);
        uint[] expected = [1608637542, 3421126067, 4083286876, 787846414, 3143890026, 3348747335];
        Assert.Equal(expected, Enumerable.Range(0, 6).Select(_ => rng.NextUInt32()));
    }

    [Fact]
    public void NextBelowMatchesEveryVector()
    {
        foreach (var vector in CasesOf("next_below"))
        {
            var rng = new MersenneTwister(vector.GetProperty("seed").GetUInt32());
            long n = vector.GetProperty("n").GetInt64();
            int count = vector.GetProperty("count").GetInt32();
            var actual = new List<long>(count);
            for (int i = 0; i < count; i++)
            {
                actual.Add(rng.NextBelow(n));
            }

            Assert.Equal(Expected(vector, x => x.GetInt64()), actual);
            AssertConsumed(vector, rng);
        }
    }

    [Fact]
    public void ShuffleMatchesEveryVector()
    {
        foreach (var vector in CasesOf("shuffle"))
        {
            var rng = new MersenneTwister(vector.GetProperty("seed").GetUInt32());
            var items = Sequence(vector.GetProperty("length").GetInt32());
            rng.Shuffle(items);

            Assert.Equal(Expected(vector, x => (long)x.GetInt32()), items.Select(x => (long)x));
            AssertConsumed(vector, rng);
        }
    }

    [Fact]
    public void ChoiceMatchesEveryVector()
    {
        foreach (var vector in CasesOf("choice"))
        {
            var rng = new MersenneTwister(vector.GetProperty("seed").GetUInt32());
            var items = Sequence(vector.GetProperty("length").GetInt32());
            int count = vector.GetProperty("count").GetInt32();
            var actual = new List<long>(count);
            for (int i = 0; i < count; i++)
            {
                actual.Add(rng.Choice(items));
            }

            Assert.Equal(Expected(vector, x => (long)x.GetInt32()), actual);
        }
    }

    [Fact]
    public void ChooseWithoutReplacementMatchesEveryVector()
    {
        foreach (var vector in CasesOf("choose_without_replacement"))
        {
            var rng = new MersenneTwister(vector.GetProperty("seed").GetUInt32());
            var items = Sequence(vector.GetProperty("length").GetInt32());
            var actual = rng.ChooseWithoutReplacement(items, vector.GetProperty("k").GetInt32());

            Assert.Equal(Expected(vector, x => (long)x.GetInt32()), actual.Select(x => (long)x));
            AssertConsumed(vector, rng);
        }
    }

    [Fact]
    public void TheEngineShortCircuitConsumesNothing()
    {
        foreach (var vector in CasesOf("engine_choice2"))
        {
            var engine = new EngineRandom(vector.GetProperty("seed").GetUInt32());
            var items = Sequence(vector.GetProperty("length").GetInt32());
            var actual = engine.Choice2(items, vector.GetProperty("x").GetInt32());

            Assert.Equal(Expected(vector, x => (long)x.GetInt32()), actual.Select(x => (long)x));
            Assert.Equal(vector.GetProperty("words_consumed").GetInt64(),
                         engine.Generator.WordsConsumed);
        }
    }

    [Fact]
    public void TheMixedSequenceStaysInStep()
    {
        // The case that a per-function match cannot fake: one generator driven
        // through every operation in turn, so a stream-position error anywhere
        // shows up as a wrong value everywhere after it.
        var mixed = Cases.GetProperty("mixed");
        var rng = new MersenneTwister(mixed.GetProperty("seed").GetUInt32());

        foreach (var step in mixed.GetProperty("steps").EnumerateArray())
        {
            long before = rng.WordsConsumed;
            string op = step.GetProperty("op").GetString()!;

            switch (op)
            {
                case "next_uint32":
                    Assert.Equal(step.GetProperty("result").GetUInt32(), rng.NextUInt32());
                    break;

                case "next_below":
                    Assert.Equal(step.GetProperty("result").GetInt64(),
                                 rng.NextBelow(step.GetProperty("n").GetInt64()));
                    break;

                case "shuffle":
                {
                    var items = Sequence(step.GetProperty("length").GetInt32());
                    rng.Shuffle(items);
                    Assert.Equal(Results(step), items.Select(x => (long)x));
                    break;
                }

                case "choice":
                {
                    var items = Sequence(step.GetProperty("length").GetInt32());
                    Assert.Equal(step.GetProperty("result").GetInt64(), rng.Choice(items));
                    break;
                }

                case "choose_without_replacement":
                {
                    var items = Sequence(step.GetProperty("length").GetInt32());
                    var picked = rng.ChooseWithoutReplacement(items, step.GetProperty("k").GetInt32());
                    Assert.Equal(Results(step), picked.Select(x => (long)x));
                    break;
                }

                default:
                    throw new InvalidOperationException($"unknown mixed op '{op}'");
            }

            Assert.Equal(step.GetProperty("words_consumed").GetInt64(), rng.WordsConsumed - before);
        }
    }

    private static List<long> Results(JsonElement step) =>
        [.. step.GetProperty("result").EnumerateArray().Select(x => (long)x.GetInt32())];

    private static List<T> Expected<T>(JsonElement vector, Func<JsonElement, T> read) =>
        [.. vector.GetProperty("expect").EnumerateArray().Select(read)];

    private static void AssertConsumed(JsonElement vector, MersenneTwister rng) =>
        Assert.Equal(vector.GetProperty("words_consumed").GetInt64(), rng.WordsConsumed);
}
