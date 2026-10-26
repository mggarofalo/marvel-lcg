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

/// <summary>Nonpublic Rapid Response scene through the existing durable factory seam.</summary>
internal sealed class RapidResponseReplayReviewFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Rapid { get; private set; }
    internal int Ally { get; private set; }
    internal int Suit { get; private set; }
    internal int Action { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = book["cards"]!.AsArray();
        // Synthetic Action and entry response expose defeat and suspended entry deterministically.
        rows.Single(row => row!["card"]!.GetValue<string>() == "01092")!["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},"effect":{"dealDamage":{"cards":{"titled":"Hawkeye"},"amount":3}}}]
            """);
        rows.Single(row => row!["card"]!.GetValue<string>() == "01066")!["abilities"]!.AsArray().Add(JsonNode.Parse("""
            {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
             "effect":{"choose":{"options":[
               {"giveStatus":{"card":"you","status":"confused"}},
               {"giveStatus":{"card":"you","status":"stunned"}}]}}}
            """));
        // Printed 08031 is a test-local interpretation; no product admission is implied.
        rows.Add(JsonNode.Parse("""
            {"card":"08031","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game","form":"hero"},
              "when":{"exists":{"defeatedAllyInYourDiscardPile":"you"}},"cost":{"discard":"this"},
              "effect":{"seq":[
                {"putIntoPlay":{"card":{"defeatedAllyInYourDiscardPile":"you"},"where":"yourPlayArea"}},
                {"dealDamage":{"cards":"chosen","amount":1}}
              ]}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("08001a,08001b", seat.Hero);
        seat.IdentityCard.Exhaust();
        Rapid = world.CreateCard("08031", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Suit = world.CreateCard("08009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Action = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        var ally = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Ally = ally.ObjectId;
        ally.Exhaust();
        ally.PlaceTokens("c_arrow", 1);
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
