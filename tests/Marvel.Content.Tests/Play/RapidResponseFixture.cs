using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

internal sealed class RapidResponseFixture
{
    internal World World { get; }
    internal AbilityRunner Runner { get; }
    internal Card Rapid { get; }
    internal Card Ally { get; }
    internal List<GameEvent> Events { get; } = [];

    internal RapidResponseFixture(bool entryChoice = false, bool oneHp = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse(BlackWidowRapidResponseCompositionTests.Row));
        if (entryChoice)
        {
            // Synthetic entry response isolates a suspension before the damage suffix.
            book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "01066")!
                ["abilities"]!.AsArray().Add(JsonNode.Parse("""
                {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
                 "effect":{"choose":{"options":[{"ready":"you"},{"ready":"this"}]}}}
                """));
        }
        Runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        if (oneHp)
        {
            // Synthetic one-HP ally preserves Hawkeye entry behavior for re-defeat evidence.
            var cards = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
            cards["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == "01066")!
                ["attributes"]!["HP"] = "1";
            World = new World(CardCatalog.Parse(cards.ToJsonString()), 1, 12345);
            var seat = World.CreateSeat("Widow");
            seat.IdentityCard = World.CreateCard("08001a,08001b", seat.Hero);
            World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
            World.CreateCard("01097a", World.AreaOf(DeckType.MainSchemesArea));
            for (int i = 0; i < 12; i++)
            {
                World.CreateCard("01088", seat.Deck);
                World.CreateCard("01120", World.AreaOf(DeckType.EncounterDeck));
            }
        }
        else
        {
            World = ThorActualFixture.Board("06001a");
            var seat = World.Seats[0];
            World.MoveToTop(seat.IdentityCard, World.AreaOf(DeckType.RemovedArea));
            seat.IdentityCard = World.CreateCard("08001a,08001b", seat.Hero);
        }
        World.Abilities = Runner;
        World.Seats[0].IdentityCard.Exhaust();
        Rapid = World.CreateCard("08031", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Ally = World.CreateCard("01066", World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
    }

    internal Prompt Defeat()
    {
        DamagePlacement.Resolve(World, World.Facts,
            new(World.TheCardIn(DeckType.VillainArea)!, Ally, 3, "Fixture", "Damage"), Events);
        return Work();
    }

    internal Prompt Work() => Assert.IsType<Prompt>(Sequence.Work(World, World.Facts, Runner, Events));

    internal void Answer(Prompt prompt, int card) => Sequence.Answer(World, World.Facts, Runner, prompt,
        Decision.Take(Assert.Single(prompt.Affordances, offer => offer.AnchorId == card).Id), Events);
}
