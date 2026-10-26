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

/// <summary>Nonpublic printed 08013 composition through the existing durable factory seam.</summary>
internal sealed class StealthStrikeReplayReviewFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Strike { get; private set; }
    internal int Enemy { get; private set; }
    internal int Observer { get; private set; }
    internal int Main { get; private set; }
    internal int Side { get; private set; }
    internal int Foreign { get; private set; }
    internal int[] Payment { get; private set; } = [];

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        // Printed 08013: "If that enemy is defeated by this attack, remove 2 threat from a scheme."
        // 08023 is a synthetic observer, not its printed ability or public content admission.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"08013","abilities":[{
                "trigger":{"event":"TurnAction","timing":"Action","subject":"you","form":"hero"},
                "labels":["attack"],
                "effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"seq":[
                  {"attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":4}}}},
                  {"if":{"test":{"wasDefeatedByDamage":"chosen"},"then":
                    {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":2}}}}}}
                ]}}}
              }]},
              {"card":"08023","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
                "effect":{"choose":{"options":[
                  {"giveStatus":{"card":"you","status":"confused"}},
                  {"giveStatus":{"card":"you","status":"stunned"}}
                ]}}
              }]}
            ]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        Strike = world.CreateCard("08013", world.Seats[0].Hand).ObjectId;
        Payment = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01003", world.Seats[0].Hand).ObjectId).ToArray();
        Enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))).ObjectId;
        Observer = world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        Main = main.ObjectId;
        main.PlaceTokens("k_threat", 5);
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        Side = side.ObjectId;
        side.PlaceTokens("k_threat", 5);
        Foreign = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck)).ObjectId;
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
