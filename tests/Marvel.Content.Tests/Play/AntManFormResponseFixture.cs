using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production responses on synthetic research boards; no Ant-Man product admission.
internal static class AntManFormResponseFixture
{
    internal static AbilityRunner Runner(string synthetic)
    {
        var rows = JsonNode.Parse(synthetic)!["cards"]!.AsArray();
        foreach (var row in JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray())
            if (row!["card"]!.GetValue<string>() is "12009" or "12016" or "12031")
                rows.Add(row.DeepClone());
        return new AbilityRunner(AbilityCatalog.Parse(new JsonObject { ["cards"] = rows.DeepClone() }.ToJsonString()));
    }

    internal static (Game Game, World World, Card Source, Card Event, Card Scheme) Board(
        string eventFace, string destination = "12001c", int controller = 0)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = Runner("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"changeForm":{"player":"you","to":{"face":"DESTINATION"}}}}]}]}
            """.Replace("DESTINATION", destination, StringComparison.Ordinal));
        var world = new World(cards, 2, 241);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
            if (index == 1 && destination == "12001a") seat.IdentityCard.TurnTo("12001b");
            for (int count = 0; count < 20; count++) world.CreateCard("01087", seat.Deck);
            world.CreateCard("01087", seat.Hand);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        for (int count = 0; count < 20; count++)
            world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var response = world.CreateCard(eventFace, world.Seats[controller].Hand);
        world.Abilities = runner;
        var game = Game.Begin(world, cards, runner);
        for (int index = 0; index < 2; index++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.Verb == Game.ResolveMulligans).Id));
        return (game, world, source, response, scheme);
    }

    internal static void Change(Game game, Card source) => game.Resolve(Decision.Take(
        Assert.Single(game.PromptFor(1)!.Affordances, offer => offer.AnchorId == source.ObjectId).Id));

    internal static long Value(World world, int seat, string field) =>
        CardValues.Evaluate(world, world.Seats[seat].IdentityCard, field, world.Facts, 2).CurrentValue;
}
