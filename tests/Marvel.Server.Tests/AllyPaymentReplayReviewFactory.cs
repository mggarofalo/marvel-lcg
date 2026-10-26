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

/// <summary>Synthetic entry/payment scene using the supported durable factory seam.</summary>
internal sealed class AllyPaymentReplayReviewFactory(string responseFace) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Ally { get; private set; }
    internal int Wild { get; private set; }
    internal int Physical { get; private set; }
    internal int ResponsePayment { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"06012","abilities":[{
                "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
                "cost":{"spendAny":1},
                "effect":{"if":{"test":{"paidWithResource":"Y"},
                  "then":{"placeCounters":{"card":"this","counter":"test","count":3}},
                  "else":{"placeCounters":{"card":"this","counter":"test","count":2}}}}
              }]},
              {"card":"06016","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"doubleResourceFor":"Aggression"}}]}
            ]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        Ally = world.CreateCard("06012", world.Seats[0].Hand).ObjectId;
        Wild = world.CreateCard("06016", world.Seats[0].Hand).ObjectId;
        Physical = world.CreateCard("01003", world.Seats[0].Hand).ObjectId;
        ResponsePayment = world.CreateCard(responseFace, world.Seats[0].Hand).ObjectId;
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(option => option.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(option => option.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
