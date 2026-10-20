using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingNumericTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(5, 2)]
    [InlineData(10, 5)]
    public void CompletePairsOfDelayCountersDetermineTheAmount(int delay, int expected)
    {
        // Stall Tactics04085: "1 threat ... for every 2 delay counters".
        // Synthetic observable counter avoids threat thresholds hiding the arithmetic.
        var runner = Runner("01006", "Action", """
            {"placeCounters":{"card":"this","counter":"observed","count":
              {"add":[1,{"floorDiv":{"value":{"countersOn":{"card":"this","counter":"delay"}},"divisor":2}}]}}}
            """);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        source.PlaceTokens("c_delay", delay);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(expected + 1, source.Tokens["c_observed"]);
    }

    [Fact]
    public void NegativeDividendUsesFloorRatherThanTruncation()
    {
        // Engine arithmetic convention: floor(-3 / 2) is -2.
        var runner = Runner("01006", "Action", """
            {"placeCounters":{"card":"this","counter":"observed","count":
              {"add":[3,{"floorDiv":{"value":-3,"divisor":2}}]}}}
            """);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(1, source.Tokens["c_observed"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DivisionRejectsNonpositiveFixedDivisors(int divisor)
    {
        Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            new AbilityValue.Map(new Dictionary<string, AbilityValue>
            { ["floorDiv"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                { ["value"] = new AbilityValue.Number(5), ["divisor"] = new AbilityValue.Number(divisor) }) }),
            new AbilityLocation("04085", 0, "effect")));
    }
}
