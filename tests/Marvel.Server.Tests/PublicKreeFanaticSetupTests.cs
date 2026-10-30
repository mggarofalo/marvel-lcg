using System.Net;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicKreeFanaticSetupTests : TransportTestBase
{
    private static readonly string[] OtherHeroes = ["captain_marvel", "she_hulk", "iron_man"];
    private static readonly string[] Copies = ["90001", "90002", "90003", "90004", "90004", "90005", "90005"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NormalDiscoveryAndOpeningExposeTheModularSetAcrossBothTransports(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var response = await Exchange(host, socket, EngineRequest.ReadSetup("kree-catalog"));
        Assert.Null(response.Error);
        var choice = Assert.Single(response.Setup!.ModularSets, item => item.Key == "kree_fanatic");
        Assert.Equal("Kree Fanatic", choice.Name);
        Assert.Equal(14, response.Setup.Heroes.Count);
        Assert.Equal(39, response.Setup.Scenarios.Count);
        Assert.Equal(13, response.Setup.ModularSets.Count);
        Assert.DoesNotContain(response.Setup.Scenarios, item => item.Key.Contains("ronan", StringComparison.Ordinal));
        Assert.DoesNotContain(response.Setup.ModularSets, item => item.Key == "sinister_syndicate");
        response = await Exchange(host, socket, EngineRequest.OpenGame("kree-open", "kree-game",
            new GameSpecification("rhino_expert", ["spider_man"], [choice.Key], 567)));
        Assert.Null(response.Error);
        Assert.NotNull(response.World);
        Assert.NotNull(response.Prompt);
        Assert.NotNull(response.Capability);
    }

    [Fact]
    public void EverySupportedStandardExpertSlotAcceptsTheSetWithStarterAndCustomDecks()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "spider_man");
        string[] custom = [.. hero.Starter.SkipLast(1), "01086"];
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var scenarios = factory.DiscoverSetup().Scenarios.Where(item => item.AllowsModularSets);
        foreach (var scenario in scenarios)
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] heroes = ["spider_man", .. OtherHeroes.Take(count - 1)];
            var decks = count == 1 ? new Marvel.Rules.Setup.PlayerDeckRecipe[] { new(custom) } : null;
            var game = factory.Create(new GameSpecification(scenario.Key, heroes, ["kree_fanatic"], 567, decks)).Game;
            CompleteSetup(game, $"{scenario.Key}, {count} players");
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(count, game.State.Seats.Count);
            Assert.Equal(Copies, game.State.Cards.Where(card => card.FaceId.StartsWith("900", StringComparison.Ordinal))
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.All(game.State.Cards.Where(card => Copies.Contains(card.FaceId)), card => Assert.Equal(-1, card.Owner));
        }
    }

    [Fact]
    public void ExactEncounterCompositionUsesSeededShuffleAndKeepsDefaultSelection()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var specification = new GameSpecification("rhino", ["spider_man"], ["kree_fanatic"], 567);
        var game = factory.Create(specification).Game;
        string[] expected = [.. setup.Campaign("rhino").Encounters, .. setup.EncounterSet("standard"),
            .. setup.Hero("spider_man").Obligations, .. Copies];
        var deck = game.State.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId).ToArray();
        Assert.Equal(expected.Order(StringComparer.Ordinal), deck.Order(StringComparer.Ordinal));
        Assert.Equal(deck, factory.Create(specification).Game.State.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId));
        Assert.NotEqual(deck, factory.Create(specification with { Seed = 568 }).Game.State.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId).ToArray());
        var defaults = factory.Create(specification with { ModularSets = null }).Game;
        Assert.DoesNotContain(defaults.State.Cards, card => Copies.Contains(card.FaceId));
        Assert.Contains(defaults.State.Cards, card => card.FaceId == "01109");
        Assert.Throws<KeyNotFoundException>(() => factory.Create(specification with { ModularSets = ["sinister_syndicate"] }));
        Assert.Throws<KeyNotFoundException>(() => factory.Create(specification with { Scenario = "ronan" }));
    }

    private static void CompleteSetup(Game game, string context)
    {
        for (int index = 0; game.Phase == GamePhase.ScenarioSetup && index < 32; index++)
        {
            var prompt = game.Pending!;
            Assert.True(prompt.Asking is Question.Order or Question.Element);
            var offered = prompt.Affordances.First(option => option.IsLegal);
            var targets = offered.Targets is null ? [] : prompt.Asking == Question.Order
                ? offered.Targets.Legal : offered.Targets.Legal.Take(offered.Targets.Min).ToArray();
            game.Resolve(Decision.Take(offered.Id, targets, []));
        }
        Assert.True(game.Phase != GamePhase.ScenarioSetup,
            $"Setup cap: {context}; trigger={game.Pending?.Trigger}; question={game.Pending?.Asking}; "
            + $"offers={string.Join(";", game.Pending?.Affordances.Select(item => $"{item.Id}/{item.Verb}/{item.IsLegal}") ?? [])}");
    }

    private static async Task<EngineResponse> Exchange(EngineHost host, bool socket, EngineRequest request) => socket
        ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
        : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
}
