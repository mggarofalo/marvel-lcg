using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Play;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// The vocabulary the Ronan the Accuser scenario added, on its own.
public sealed class RonanLoweringTests : CoreCardTestBase
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"1\"")]
    public void AdditionalStartingCountersMustBeAPositiveInteger(string additional)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            $$$"""{"cards":[{"card":"16110","startingCounters":{"type":"fury","count":1,"uses":true,"additional":{{{additional}}}}}]}"""));
        Assert.Contains("positive integer 'additional'", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AdditionalStartingCountersAreNotMultiplied()
    {
        var book = AbilityCatalog.Parse(
            """{"cards":[{"card":"16110","startingCounters":{"type":"fury","count":2,"uses":true,"perPlayer":true,"additional":3}}]}""");
        var pool = book.CounterPools!["16110"];

        Assert.Equal(3, pool.Additional);
        Assert.Equal(11, pool.StartingFor(4));
    }

    [Theory]
    [InlineData("identity", true)]
    [InlineData("allies", false)]
    [Rule("rr:attach-to.3.1")]
    public void OnlyAPlayersIdentityAreaNamesOneCard(string area, bool resolves)
    {
        // A synthetic Superior Tactics attaches the Power Stone to a card a player area names.
        var world = RonanBoards.Board();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """{"cards":[{"card":"16113","abilities":[{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},"effect":{"attachCards":{"cards":{"titled":"Power Stone"},"host":{"inPlayerArea":{"area":"""
            + $"\"{area}\"" + ""","player":"firstPlayer"}}}}}]}]}"""));
        var stone = RonanBoards.Stone(world, GrootBoards.Villain(world));
        RonanBoards.Reveal(world, RonanBoards.SuperiorTactics);

        if (resolves)
        {
            BadoonBoards.Drive(world);
            Assert.Equal(world.Seats[0].IdentityCard, RonanBoards.Host(world, stone));
        }
        else Assert.Throws<RulesNotImplementedException>(() => BadoonBoards.Drive(world));
    }
}
