using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ExplicitDeckSessionTests
{
    [Fact]
    public void OrderedPerSeatDecksSurviveWireSerializationAndDurableReplay()
    {
        var catalog = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        string[] heroes = ["spider_man", "captain_marvel"];
        PlayerDeckRecipe[] recipes = heroes.Select(key => catalog.Hero(key))
            .Select(hero => new PlayerDeckRecipe([.. hero.PlayerDeck.Reverse(), .. hero.HeroDeck.Reverse()]))
            .ToArray();
        var request = EngineJson.Snapshot(EngineRequest.OpenGame("open", "chosen-decks",
            new GameSpecification("rhino", heroes, [], 91, PlayerDecks: recipes)));
        Assert.Equal(recipes[0].Cards, request.Game!.PlayerDecks![0].Cards);
        Assert.Equal(recipes[1].Cards, request.Game.PlayerDecks[1].Cards);

        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        EngineResponse opened = host.Exchange(request);
        Assert.Null(opened.Error);
        EngineResponse current = opened;
        foreach (int seat in new[] { 0, 1 })
        {
            Assert.Equal(seat, current.Prompt!.Player);
            var keep = Assert.Single(current.Prompt.Affordances);
            current = host.Exchange(EngineRequest.ResolveGame($"keep-{seat}", "chosen-decks", opened.Capability!,
                Decision.Take(keep.Id), current.Revision));
            Assert.Null(current.Error);
        }
        var saved = Assert.Single(store.Load()).Save;
        Assert.Equal(recipes[0].Cards, saved.Setup.PlayerDecks![0].Cards);
        Assert.Equal(recipes[1].Cards, saved.Setup.PlayerDecks[1].Cards);

        EngineResponse before = host.Exchange(EngineRequest.SyncGame("sync", "chosen-decks", opened.Capability!));
        var restarted = new EngineHost(factory, store: store);
        EngineResponse after = restarted.Exchange(EngineRequest.SyncGame("sync", "chosen-decks", opened.Capability!));
        Assert.Null(after.Error);
        Assert.Equal(EngineJson.Write(before), EngineJson.Write(after));

        var explicitGame = factory.Create(request.Game).Game.State;
        var starterGame = factory.Create(request.Game with { PlayerDecks = null }).Game.State;
        Assert.NotEqual(explicitGame.Digest().Canonical(), starterGame.Digest().Canonical());
    }
}
