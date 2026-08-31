using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>Civil War leaders occupying and obeying the villain position.</summary>
public sealed class LeaderSetupTests
{
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:leader")]
    [Theory]
    [InlineData("captain_america", "56137", "56138")]
    [InlineData("captain_marvel", "56092", "56093")]
    [InlineData("iron_man", "56059", "56060")]
    [InlineData("spider_woman", "56168", "56169")]
    public void CivilWarLeaderDealsAsTheActiveVillain(
        string scenario, string firstFace, string secondFace)
    {
        // `rr:leader`: “The leader card type follows the same rules as the
        // villain card type for all purposes.” `pack:mc56:leaders` places
        // leaders in Civil War scenarios. The kind remains Leader while the
        // ordinary villain zones and state fields apply.
        var world = Deal(scenario);
        var leader = world.TheCardIn(DeckType.VillainArea)!;
        var next = Assert.Single(world.AreaOf(DeckType.VillainDeck).Cards);

        Assert.Equal(firstFace, leader.FaceId);
        Assert.Equal(CardKind.Leader, Cards.Kind(leader.FaceId));
        Assert.Equal(secondFace, next.FaceId);
        Assert.Equal(CardKind.Leader, Cards.Kind(next.FaceId));
        Assert.Contains(
            leader.ObjectId,
            BasicPowers.Attackable(world, Cards, player: 0).Select(card => card.ObjectId));
    }

    [Fact]
    public void LeaderDigestUsesTheEstablishedVillainWireShape()
    {
        // `pack:mc56:leaders` makes the values villain values. The names of
        // the digest fields are the engine's wire-format choice, so this pins
        // Leader to the already-established villain spelling rather than a new
        // parallel spelling.
        var world = Deal("captain_america");
        var leader = world.TheCardIn(DeckType.VillainArea)!;
        var record = world.Digest().Cards.Single(card => card.Id == leader.ObjectId);

        Assert.Equal(
            StateFields.Keys(CardKind.EncounterVillain, hasHeldPools: true),
            StateFields.Keys(CardKind.Leader, hasHeldPools: true));
        Assert.Equal(2, record.Fields["attack"]);
        Assert.Equal(14, record.Fields["health"]);
        Assert.Equal(1, record.Fields["printed_stage"]);
        Assert.Equal(1, record.Fields["scheme"]);
        Assert.Equal(0, record.Fields["k_threat"]);
        Assert.Equal(1, record.Fields["t_AVENGER"]);
    }

    [Rule("rr:villain-defeat")]
    [Fact]
    public void DefeatedLeaderAdvancesToItsNextStage()
    {
        // `pack:mc56:leaders` says every game rule affecting villains affects
        // leaders. rr:villain-defeat replaces a defeated villain with its next
        // stage, so Captain America II becomes the active Leader.
        var world = Deal("captain_america");
        var first = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();

        Defeat.FinalizeCharacter(world, Cards, first, "test", events);

        var second = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal("56138", second.FaceId);
        Assert.Equal(CardKind.Leader, Cards.Kind(second.FaceId));
        Assert.Empty(world.AreaOf(DeckType.VillainDeck).Cards);
        Assert.Contains(events, each => each is CardsMoved);
    }

    private static World Deal(string scenario) => WorldSetup.Deal(
        Cards,
        Blueprints.From(
            Dealer.DealOrder(Setup, scenario, ["spider_man"]),
            Cards),
        ["Spider-Man"],
        Seed);
}
