using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

// Prepared board through the durable factory seam, using the actual authored abilities.
internal sealed class ForcedDamageReceiptReplayFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Taskmaster { get; private set; }
    internal int Barrier { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var authored = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var book = new JsonObject { ["cards"] = new JsonArray() };
        foreach (string face in new[] { "04093", "05017" })
            book["cards"]!.AsArray().Add(authored["cards"]!.AsArray()
                .Single(row => row!["card"]!.GetValue<string>() == face)!.DeepClone());
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        Taskmaster = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea)).ObjectId;
        var barrier = world.CreateCard("05017",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Barrier = barrier.ObjectId;
        barrier.PlaceTokens("c_reflection", 1);
        world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
