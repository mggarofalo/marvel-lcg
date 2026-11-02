using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ZolaSetupOpeningTests
{
    [Theory]
    [InlineData(1u, new[] { "spider_man" }, "04122: choose a card")]
    [InlineData(2u, new[] { "spider_man", "captain_marvel" }, "order the players for 04122")]
    [Rule("rr:first-player.2")]
    public void TheIslandOfDrZolaRevealsHydraPrisonBeforeAskingTheFirstPlayer(
        uint seed, string[] heroes, string firstQuestion)
    {
        // rr:first-player.2: "If an encounter card requires a card ability to be
        // resolved, a game function to be performed, or a choice to be made but does
        // not specify which player should act, the first player does so."
        // 04112a Setup: "Search the encounter deck for Hydra Prison and reveal it.
        // Each player searches the encounter deck for a copy of Ultimate Bio-Servant
        // and puts it into play engaged with them." The encounter deck holds one
        // Hydra Prison, so the first player's search has one answer, and the
        // opener's first question comes from Hydra Prison's When Revealed text.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "zola",
            new GameSpecification("zola", heroes, ["under_attack"], Seed: seed)));
        Assert.Null(opened.Error);
        var prompt = Assert.IsType<Prompt>(opened.Prompt);
        Assert.Equal(0, prompt.Player);
        Assert.Equal(firstQuestion, prompt.Label);

        var world = factory.Create(new GameSpecification("zola", heroes, ["under_attack"], Seed: seed)).Game.State;
        var prison = Assert.Single(world.Cards, card => card.FaceId == "04122");
        Assert.Equal(DeckType.SideSchemesArea, prison.Area.Type);
        // The Ultimate Bio-Servant searches follow once Hydra Prison has resolved.
        Assert.Equal(4, world.Cards.Count(card =>
            card.FaceId == "04114" && card.Area.Type == DeckType.EncounterDeck));
    }
}
