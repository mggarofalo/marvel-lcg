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

// Synthetic Covert Ops interpretation through the existing durable factory seam;
// the production catalog and Black Widow admission are outside this scene.
internal sealed class CovertOpsReplayReviewFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Source { get; private set; }
    internal int[] Payment { get; private set; } = [];

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01001a","abilities":[{
                "trigger":{"event":"WhenCharacterThwarts","timing":"Response","actor":"you"},
                "effect":{"draw":{"player":"you","count":1}}}]},
              {"card":"08003","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "labels":["thwart"],
                "effect":{"if":{"test":{"exists":{"query":"thwartableSchemes"}},
                  "then":{"chooseCard":{"from":{"query":"thwartableSchemes"},"effect":{
                    "thwart":{"target":"chosen","effect":{"seq":[
                      {"removeThreat":{"scheme":"chosen","amount":4}},
                      {"giveStatus":{"card":{"query":"villain"},"status":"confused"}}]}}}}},
                  "else":{"thwart":{"effect":{
                    "giveStatus":{"card":{"query":"villain"},"status":"confused"}}}}}}
              }]}
            ]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        Source = world.CreateCard("08003", world.Seats[0].Hand).ObjectId;
        Payment = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01093", world.Seats[0].Hand).ObjectId).ToArray();
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("The synthetic prelude did not reach a player action");
    }
}
