using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Play;
using Marvel.Rules.Play;
using Marvel.Rules.State;
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

    [Fact]
    public void TheVillainPlayAreaIsReadAsEveryInPlayArea()
    {
        var reads = AbilitySelectionAreaReads.For(new AbilityCardSelection.InAreas(
            [AbilitySearchArea.VillainPlayArea], null, null, "Power Stone"));

        Assert.Contains(DeckType.UpgradesArea, reads);
        Assert.Contains(DeckType.VillainArea, reads);
        Assert.DoesNotContain(DeckType.EncounterDeck, reads);
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

    private static readonly AbilityCondition StoneOnRonan = new AbilityCondition.Exists(
        new AbilityCardSelection.InAreas([AbilitySearchArea.VillainPlayArea], null, null, "Power Stone"));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AProjectedVillainPlayAreaTestReadsTheStonesProjectedHost(bool projectedOntoRonan, bool expected)
    {
        var world = RonanBoards.Board();
        var stone = RonanBoards.Stone(world, world.Seats[0].IdentityCard);
        var hosts = new Dictionary<int, int>();
        if (projectedOntoRonan) hosts[stone.ObjectId] = GrootBoards.Villain(world).ObjectId;
        var projection = new AbilityThreatPermissionProjection(world, new HashSet<int>(), new HashSet<int>(), hosts);

        Assert.Equal(expected, projection.Test(StoneOnRonan, null!));
    }

    [Fact]
    public void AProjectedVillainPlayAreaTestReadsAStoneLeavingPlayAsAbsent()
    {
        var world = RonanBoards.Board();
        var stone = RonanBoards.Stone(world, GrootBoards.Villain(world));
        var projection = new AbilityThreatPermissionProjection(
            world, new HashSet<int> { stone.ObjectId }, new HashSet<int>(), new Dictionary<int, int>());

        Assert.False(projection.Test(StoneOnRonan, null!));
    }

    [Fact]
    public void AProjectedVillainPlayAreaTestRaisesForAStoneEnteringPlay()
    {
        var world = RonanBoards.Board();
        var stone = world.CreateCard(ShipCommandBoards.PowerStone, world.AreaOf(DeckType.EncounterDiscardPile));
        var projection = new AbilityThreatPermissionProjection(
            world, new HashSet<int>(), new HashSet<int> { stone.ObjectId }, new Dictionary<int, int>());

        Assert.Throws<RulesNotImplementedException>(() => projection.Test(StoneOnRonan, null!));
    }
}
