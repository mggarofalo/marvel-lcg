using Marvel.Core.Random;
using Xunit;

namespace Marvel.Core.Tests.Random;

/// <summary>
/// The facade the engine draws through — <c>docs/rng-contract.md</c>.
/// </summary>
/// <remarks>
/// One generator per game, and every draw in the game comes off it. What this
/// layer adds over <see cref="MersenneTwister"/> is one rule about consumption,
/// and consumption is the half that is easy to get wrong without noticing:
/// wrong values are visible immediately, a wrong stream position moves every
/// draw after it.
/// </remarks>
public sealed class EngineRandomTests
{
    private static readonly int[] Four = [0, 1, 2, 3];

    [Fact]
    public void SelectingEveryElementDrawsNothingAndKeepsTheOrder()
    {
        // There is one answer, so there is nothing to choose. Drawing anyway
        // would produce the right list and the wrong stream position -- the
        // failure that looks like nothing at all until a later card lands
        // somewhere else.
        var random = new EngineRandom(5489u);

        var all = random.Choice2(Four, 4);

        Assert.Equal(Four, all);
        Assert.Equal(0, random.Generator.WordsConsumed);
    }

    [Fact]
    public void SelectingOneFewerDrawsNormally()
    {
        // The boundary the short-circuit sits on. `k == count` costs nothing;
        // `k == count - 1` is an ordinary partial shuffle and costs a word per
        // step.
        //
        // Note `>=` in place of `==` is **not** catchable, here or anywhere:
        // `ThrowIfGreaterThan` has already rejected every `x` above `count`, so
        // the two spellings cannot differ. What is catchable is the
        // short-circuit not firing at all, which is the test above.
        var random = new EngineRandom(5489u);

        var some = random.Choice2(Four, 3);

        Assert.Equal(3, some.Count);
        Assert.Equal(3, random.Generator.WordsConsumed);
    }

    [Fact]
    public void SelectingNothingDrawsNothing()
    {
        var random = new EngineRandom(5489u);

        Assert.Empty(random.Choice2(Four, 0));
        Assert.Equal(0, random.Generator.WordsConsumed);
    }

    [Fact]
    public void TheShortCircuitDisagreesWithTheGeneratorOnPurpose()
    {
        // `Choice2(items, count)` costs nothing; `ChooseWithoutReplacement(
        // items, count)` costs one word per element. Two layers, one request,
        // different stream positions -- deliberate, and pinned here so that
        // "fixing" either to match the other fails a test rather than moving
        // every game.
        var facade = new EngineRandom(5489u);
        var generator = new MersenneTwister(5489u);

        facade.Choice2(Four, 4);
        generator.ChooseWithoutReplacement(Four, 4);

        Assert.Equal(0, facade.Generator.WordsConsumed);
        Assert.Equal(4, generator.WordsConsumed);
    }

    [Fact]
    public void AnXOutsideTheSequenceIsRefused()
    {
        var random = new EngineRandom(5489u);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.Choice2(Four, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Choice2(Four, -1));
    }

    [Fact]
    public void ReseedingRestartsTheStream()
    {
        // A seed names a game, so seeding twice with one value has to produce
        // one game. `SetSeed` reinitialises the state rather than mixing into
        // it.
        var random = new EngineRandom(5489u);
        uint first = random.Generator.NextUInt32();

        random.SetSeed(5489u);

        Assert.Equal(first, random.Generator.NextUInt32());
    }
}
