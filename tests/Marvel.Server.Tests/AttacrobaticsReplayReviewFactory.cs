using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

// Synthetic scene through the durable factory seam, not public Black Widow admission.
internal sealed class AttacrobaticsReplayReviewFactory(bool guard) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Preparation { get; private set; }
    internal int Suit { get; private set; }
    internal int Action { get; private set; }
    internal int Villain { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var authored = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var book = JsonNode.Parse("""
            {"cards":[
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "cost":{"exhaust":"this"},"effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}
              }]},
              {"card":"08001a","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you"},
                "effect":{"draw":{"player":"you","count":1}}
              },{
                "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"villain"},
                "effect":{"draw":{"player":"you","count":2}}
              }]}
            ]}
            """)!;
        foreach (string face in new[] { "08006", "08009" })
            book["cards"]!.AsArray().Add(authored["cards"]!.AsArray()
                .Single(row => row!["card"]!.GetValue<string>() == face)!.DeepClone());
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[0].IdentityCard = world.CreateCard("08001a,08001b", world.Seats[0].Hero);
        world.Seats[0].IdentityCard.Exhaust();
        Preparation = world.CreateCard("08006",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Suit = world.CreateCard("08009",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Action = world.CreateCard("01092",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect,
            "retaliate", 2, Affects: Villain));
        if (guard) world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
