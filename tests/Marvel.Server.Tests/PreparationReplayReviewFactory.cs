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

/// <summary>Synthetic Preparation scene using the supported durable factory seam.</summary>
internal sealed class PreparationReplayReviewFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Preparation { get; private set; }
    internal int Suit { get; private set; }
    internal int Observer { get; private set; }
    internal int Minion { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"08010","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"cost":{"discard":"this"},
                "effect":{"attack":{"target":{"titled":"Hydra Soldier"},"effect":{"seq":[
                  {"dealAttackDamage":{"cards":"chosen","amount":2}},
                  {"if":{"test":{"isInPlay":"chosen"},
                    "then":{"giveStatus":{"card":"chosen","status":"stunned"}}}}
                ]}}}
              }]},
              {"card":"08023","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"choose":{"options":[
                  {"giveStatus":{"card":"you","status":"confused"}},
                  {"giveStatus":{"card":"you","status":"stunned"}}
                ]}}
              }]},
              {"card":"01001a","abilities":[{
                "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
                "effect":{"chooseCard":{"from":{"query":"villain"},
                  "effect":{"dealDamage":{"cards":"chosen","amount":1}}}}
              }]},
              {"card":"08009","abilities":[{
                "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
                "cost":{"exhaust":"this"},"effect":{"ready":"you"}
              }]},
              {"card":"01182","abilities":[{"trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
                "effect":{"dealEncounterCards":{"player":"engagedPlayer","count":1}}}]}
            ]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        world.Seats[0].IdentityCard.Exhaust();
        Preparation = world.CreateCard("08010",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Suit = world.CreateCard("08009",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Observer = world.CreateCard("08023",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        var minion = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Minion = minion.ObjectId;
        minion.TakeDamage(2);
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
