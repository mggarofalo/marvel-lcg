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

/// <summary>Synthetic boost scene using the supported durable factory seam.</summary>
internal sealed class BoostAbilityReplayReviewFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Preparation { get; private set; }
    internal int Suit { get; private set; }
    internal int Action { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "cost":{"exhaust":"this"},"effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}
              },{
                "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","subject":"game"},
                "effect":{"cancelBoostIcons":"trigger"}
              }]},
              {"card":"08024","abilities":[{
                "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Response","subject":"game","form":"hero"},
                "cost":{"discard":"this"},"effect":{"cancelBoostAbility":"trigger"}
              }]},
              {"card":"08009","abilities":[{
                "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
                "cost":{"exhaust":"this"},"effect":{"ready":"you"}
              }]},
              {"card":"02030","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
                "effect":{"dealDamage":{"cards":"you","amount":1}}
              }]}
            ]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        world.Seats[0].IdentityCard.Exhaust();
        Preparation = world.CreateCard("08024",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Suit = world.CreateCard("08009",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Action = world.CreateCard("01092",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        world.CreateCard("02030", world.AreaOf(DeckType.EncounterDeck));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
