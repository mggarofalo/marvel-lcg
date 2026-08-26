using Marvel.Core.Random;
using Xunit;

namespace Marvel.Core.Tests.Random;

/// <summary>
/// MT19937, against the algorithm as published — <c>docs/rng-contract.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The anchor is a standard, not a recording.</b> ISO/IEC 14882 defines
/// <c>std::mt19937</c> as this generator seeded with <c>5489</c>, and requires
/// that the <b>10,000th</b> consecutive draw be <c>4123659995</c>. Any
/// conforming implementation reproduces it, so agreeing with it is evidence
/// about the algorithm rather than about whoever wrote the fixture.
/// </para>
/// <para>
/// It is also a good test rather than merely an authoritative one: 10,000 draws
/// is sixteen refills of the 624-word state, so a generator that tempers
/// correctly and twists wrongly cannot reach it. The shorter vectors below
/// localise a failure the long one only detects.
/// </para>
/// <para>
/// <b>Everything past <see cref="MersenneTwister.NextUInt32"/> is ours.</b> No
/// standard says how to reduce a 32-bit word to a bounded integer, which
/// direction Fisher-Yates runs, or what an empty shuffle costs. Those are
/// choices <c>docs/rng-contract.md</c> makes and pins, and they are tested here
/// as the properties they are — a seed names a game only if every one of them
/// holds still.
/// </para>
/// </remarks>
public sealed class MersenneTwisterTests
{
    /// <summary>The seed <c>std::mt19937</c> is default-constructed with.</summary>
    private const uint Standard = 5489u;

    [Fact]
    public void TheTenThousandthDrawIsTheValueTheStandardRequires()
    {
        var mt = new MersenneTwister(Standard);

        uint value = 0;
        for (int draw = 0; draw < 10_000; draw++)
        {
            value = mt.NextUInt32();
        }

        Assert.Equal(4123659995u, value);
        Assert.Equal(10_000, mt.WordsConsumed);
    }

    [Fact]
    public void TheFirstDrawsAreThePublishedSequence()
    {
        // Where the long vector says "somewhere in ten thousand draws", these
        // say "the first one" -- so a seeding bug and a twist bug fail
        // different tests.
        var mt = new MersenneTwister(Standard);

        Assert.Equal(
            [
                3499211612u, 581869302u, 3890346734u, 3586334585u, 545404204u,
                4161255391u, 3922919429u, 949333985u, 2715962298u, 1323567403u,
            ],
            Draws(mt, 10));
    }

    [Theory]
    [InlineData(0u, 2357136044u, 2546248239u, 3071714933u)]
    [InlineData(1u, 1791095845u, 4282876139u, 3093770124u)]
    [InlineData(12345u, 3992670690u, 3823185381u, 1358822685u)]
    [InlineData(uint.MaxValue, 419326371u, 479346978u, 3918654476u)]
    public void SeedingIsTheStandardsRecurrence(uint seed, uint first, uint second, uint third)
    {
        // `mt[0] = seed`, then `mt[i] = 1812433253 * (mt[i-1] ^ (mt[i-1] >> 30)) + i`.
        // Seed 0 is a real seed and not a request for entropy; `uint.MaxValue`
        // is the other end, where a widening bug would show.
        var mt = new MersenneTwister(seed);

        Assert.Equal([first, second, third], Draws(mt, 3));
    }

    [Fact]
    public void TheStateRefillsWithoutDroppingOrRepeatingAWord()
    {
        // Draw 624 is the first of the second block, so an off-by-one in the
        // refill shows here and nowhere earlier. Both neighbours are asserted
        // because a generator that refills one draw early and one that refills
        // one draw late are different bugs.
        var mt = new MersenneTwister(Standard);
        var drawn = Draws(mt, 626);

        Assert.Equal(2227348307u, drawn[622]);
        Assert.Equal(4020325887u, drawn[623]);
        Assert.Equal(4178893912u, drawn[624]);
    }

    [Fact]
    public void RestoringAStateResumesTheSameStream()
    {
        // The state is the whole generator: 624 words and an index. If
        // restoring one left anything out, the resumed stream would diverge --
        // and it would diverge *later*, once the omitted part started to
        // matter, which is why this draws past a refill boundary.
        var mt = new MersenneTwister(Standard);
        Draws(mt, 700);

        var saved = mt.GetState();
        var expected = Draws(mt, 700);

        var restored = new MersenneTwister(1u);
        restored.SetState(saved);

        Assert.Equal(expected, Draws(restored, 700));
    }

    [Fact]
    public void ABoundOfTwoToThirtyTwoTerminatesAndKeepsTheWord()
    {
        // `n` is 64-bit so that 2^32 is expressible. The mask covers every bit,
        // nothing can reject, and the raw word comes back unchanged. A `uint`
        // parameter wraps this bound to 0 and the rejection loop never exits --
        // the failure is a hang rather than a wrong answer.
        var reference = new MersenneTwister(Standard);
        uint word = reference.NextUInt32();

        var mt = new MersenneTwister(Standard);
        Assert.Equal(word, mt.NextBelow(1L << 32));
        Assert.Equal(1, mt.WordsConsumed);
    }

    [Fact]
    public void ABoundOfOneStillCostsAWord()
    {
        // There is one answer and it is 0, so returning early would be correct
        // arithmetic and a broken stream: every draw after it would move.
        var mt = new MersenneTwister(Standard);

        Assert.Equal(0u, mt.NextBelow(1));
        Assert.Equal(1, mt.WordsConsumed);
    }

    [Fact]
    public void APowerOfTwoBoundNeverRejects()
    {
        // The mask is exact, so each answer costs exactly one word. This is
        // what makes consumption predictable for the sizes a card actually
        // asks about.
        var mt = new MersenneTwister(Standard);

        for (int draw = 0; draw < 100; draw++)
        {
            Assert.InRange(mt.NextBelow(256), 0u, 255u);
        }

        Assert.Equal(100, mt.WordsConsumed);
    }

    [Fact]
    public void ABoundOutsideTheContractIsRefused()
    {
        var mt = new MersenneTwister(Standard);

        Assert.Throws<ArgumentOutOfRangeException>(() => mt.NextBelow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => mt.NextBelow(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => mt.NextBelow((1L << 32) + 1));
    }

    [Fact]
    public void ShuffleRunsDownwardAndChooseWithoutReplacementRunsUpward()
    {
        // Both produce uniform permutations; from one stream they produce
        // *different* ones. The direction is therefore part of the contract,
        // and this is the test that would fail if somebody "simplified" one to
        // match the other.
        int[] downward = [0, 1, 2, 3, 4, 5, 6, 7];
        new MersenneTwister(Standard).Shuffle(downward);

        var upward = new MersenneTwister(Standard)
            .ChooseWithoutReplacement<int>([0, 1, 2, 3, 4, 5, 6, 7], 8);

        Assert.NotEqual(downward, upward);
    }

    [Fact]
    public void AShuffleWithNothingToDecideCostsNothing()
    {
        // Zero or one element has one arrangement. Consuming a word for it
        // would move every later draw in the game.
        var mt = new MersenneTwister(Standard);

        mt.Shuffle(Nothing);
        mt.Shuffle(One);

        Assert.Equal(0, mt.WordsConsumed);
    }

    /// <summary>Four elements, shared by the tests that draw from a fixed pool.</summary>
    private static readonly int[] Four = [0, 1, 2, 3];

    /// <summary>The two sequences with exactly one arrangement.</summary>
    private static readonly int[] Nothing = [];
    private static readonly int[] One = [7];

    [Fact]
    public void ChoosingEveryElementStillCostsAWordPerStep()
    {
        // The last step picks from a pool of one and draws anyway, because
        // `NextBelow` special-cases nothing: four of four costs four words.
        //
        // **`EngineRandom.Choice2` does not agree, and that is deliberate.** It
        // short-circuits `k == count` and draws nothing at all, so the two
        // layers sit at different stream positions for what reads like the same
        // request. Both are pinned, here and in `EngineRandomTests`, so the
        // difference is visible rather than discovered.
        var mt = new MersenneTwister(Standard);

        var all = mt.ChooseWithoutReplacement(Four, 4);

        Assert.Equal(4, all.Count);
        Assert.Equal(4, mt.WordsConsumed);
    }

    [Fact]
    public void KOutsideTheSequenceIsRefusedRatherThanClamped()
    {
        // A clamp would hand the game the wrong number of targets and look
        // like a successful call while doing it.
        var mt = new MersenneTwister(Standard);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => mt.ChooseWithoutReplacement<int>([0, 1], 3));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => mt.ChooseWithoutReplacement<int>([0, 1], -1));
    }

    [Fact]
    public void OneStreamServesEveryCallInTheOrderTheyAreMade()
    {
        // **The mixed case.** Each function above agrees with the stream on its
        // own; that is not the same as the four of them sharing one position.
        // Interleaving them and replaying the same interleaving from a raw
        // generator is what catches a call that consumes the wrong number of
        // words -- a per-function match hides it completely.
        var mixed = new MersenneTwister(Standard);
        int[] deck = [0, 1, 2, 3, 4];

        mixed.NextUInt32();
        mixed.NextBelow(6);
        mixed.Shuffle(deck);
        mixed.Choice<int>([0, 1, 2]);
        mixed.ChooseWithoutReplacement(Four, 2);

        // 1 + 1 + 4 (shuffle of five) + 1 + 2, none of which can reject: every
        // bound here is 6, 5, 4, 3, 2 or 4, and only 6 and 3 can reject at all.
        Assert.InRange(mixed.WordsConsumed, 9, 32);

        var replay = new MersenneTwister(Standard);
        for (long word = 0; word < mixed.WordsConsumed; word++)
        {
            replay.NextUInt32();
        }

        // The two generators have consumed the same words, so they must now
        // agree about the next one.
        Assert.Equal(replay.NextUInt32(), mixed.NextUInt32());
    }

    private static uint[] Draws(MersenneTwister mt, int count)
    {
        var drawn = new uint[count];
        for (int draw = 0; draw < count; draw++)
        {
            drawn[draw] = mt.NextUInt32();
        }

        return drawn;
    }
}
