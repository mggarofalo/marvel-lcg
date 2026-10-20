using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Printed RoRS generators on a synthetic cost board; does not claim expansion admission.</summary>
internal sealed record RestrictedResourceFixture(World World, AbilityRunner Runner, Card Generator, Card Action)
{
    internal static RestrictedResourceFixture Create(string restriction, string actionFace = "01063", bool hero = true,
        string cost = "spend", int players = 1)
    {
        string face = restriction == "arrowEvent" ? "04010" : "04033";
        string form = restriction == "aspectCard" ? ",\"form\":\"hero\"" : "";
        string actionCost = cost.Length == 0 ? "" : $"\"cost\":{{\"{cost}\":\"G\"}},";
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"{{{{face}}}}","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"{{{{form}}}}},
                "resourceRestriction":"{{{{restriction}}}}","printedResources":"G",
                "cost":{"exhaust":"this"},"effect":{"generate":"G"}}]},
              {"card":"01055","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"doubleResourceFor":"Aggression"}}]},
              {"card":"{{{{actionFace}}}}","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                {{{{actionCost}}}}"effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, players, 17);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", seat.Hero);
            for (int index = 0; index < 8; index++) world.CreateCard("01003", seat.Deck);
        }
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var generator = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var action = world.CreateCard(actionFace, facts.Kind(actionFace) == CardKind.Event ? world.Seats[0].Hand
            : world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        _ = Game.Begin(world, facts, runner);
        return new(world, runner, generator, action);
    }
}
