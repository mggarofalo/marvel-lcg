using System.Text;
using System.Text.Json;
using Marvel.Core.Digest;
using Xunit;

namespace Marvel.Core.Tests.Digest;

/// <summary>
/// The serialisation half of the state-digest contract, against
/// <c>datasets/digest/vectors.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// The fixture records whole-game traces, so the half that <i>populates</i> the
/// records (checklist steps 1–7) cannot be tested until the engine fold exists.
/// What can be tested now is steps 8 and 9 — serialise exactly, compare as
/// strings — and those are the steps most likely to differ between languages.
/// </para>
/// <para>
/// The test is a round trip: parse each recorded digest into the C# model,
/// write it back out, and require the <b>identical string</b>. That pins key
/// order, code-point field sorting, absence of whitespace and ASCII escaping
/// against documents the Python engine actually emitted, without pretending to
/// have a game.
/// </para>
/// </remarks>
public sealed class DigestVectorTests
{
    private static readonly JsonElement Fixture = Load();

    private static JsonElement Load()
    {
        using var stream = File.OpenRead(RepositoryPaths.Dataset("digest", "vectors.json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    private static JsonElement.ArrayEnumerator Cases() =>
        Fixture.GetProperty("cases").EnumerateArray();

    [Fact]
    public void TheFixtureIsTheVersionThisImplements()
    {
        Assert.Equal(StateDigest.Version, Fixture.GetProperty("digest_version").GetInt32());
        Assert.NotEmpty(Cases());
    }

    [Fact]
    public void EveryRecordedDigestRoundTripsByteForByte()
    {
        int checkedSteps = 0;
        foreach (var vector in Cases())
        {
            if (!vector.TryGetProperty("step_digests", out var digests))
            {
                // Two of the three boards record only per-step hashes, which
                // this half cannot reconstruct without an engine.
                continue;
            }

            foreach (var step in digests.EnumerateArray())
            {
                string canonical = step.GetString()!;
                Assert.Equal(canonical, StateDigest.Parse(canonical).Canonical());
                checkedSteps++;
            }
        }

        // Guards the loop above against passing by skipping everything.
        Assert.True(checkedSteps > 0, "no recorded digest was checked");
    }

    [Fact]
    public void ThePerStepHashesMatch()
    {
        foreach (var vector in Cases())
        {
            if (!vector.TryGetProperty("step_digests", out var digests))
            {
                continue;
            }

            var expected = vector.GetProperty("step_sha256").EnumerateArray()
                .Select(x => x.GetString()!).ToList();
            var actual = digests.EnumerateArray()
                .Select(x => StateDigest.Sha256(x.GetString()!)).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void TheTraceHashIsOverTheNewlineJoinedSteps()
    {
        foreach (var vector in Cases())
        {
            if (!vector.TryGetProperty("step_digests", out var digests))
            {
                continue;
            }

            string joined = string.Join("\n",
                digests.EnumerateArray().Select(x => x.GetString()));
            Assert.Equal(vector.GetProperty("trace_sha256").GetString(),
                         StateDigest.Sha256(joined));
        }
    }

    [Fact]
    public void TheEmptyDocumentIsNotTheEmptyString()
    {
        // An absent digest and an empty one mean different things to the
        // comparison, and conflating them would make a scene with no cards
        // indistinguishable from a scene recorded before v2 existed.
        Assert.Equal(StateDigest.Empty, new StateDigest([]).Canonical());
        Assert.NotEqual("", StateDigest.Empty);
    }

    [Fact]
    public void FieldsSortByCodePointAndNotByCulture()
    {
        // `t_GENIUS` before `toughness`: '_' (0x5F) precedes 'o' (0x6F). A
        // culture-aware sort reorders these on some machines and not others,
        // which is the worst failure mode available to a byte comparison.
        var record = new CardRecord(0, "x", "HeroArea", 0, 0, -1, true,
            new Dictionary<string, long> { ["toughness"] = 1, ["t_GENIUS"] = 1 });
        string text = new StateDigest([record]).Canonical();
        Assert.Contains("\"fields\":{\"t_GENIUS\":1,\"toughness\":1}", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData("a\\b", "\"a\\\\b\"")]
    [InlineData("a/b", "\"a/b\"")]                 // Python does not escape it
    [InlineData("a\nb", "\"a\\nb\"")]
    [InlineData("\u00e9", "\"\\u00e9\"")]          // ensure_ascii=True
    [InlineData("<&>", "\"<&>\"")]                 // .NET's default encoder would escape these
    public void StringsEscapeTheWayPythonDoes(string value, string expected)
    {
        var builder = new StringBuilder();
        StateDigest.WriteJsonString(builder, value);
        Assert.Equal(expected, builder.ToString());
    }
}
