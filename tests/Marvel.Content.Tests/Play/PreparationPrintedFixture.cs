using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

// Actual printed rows with explicitly synthetic optional suspension probes; no public admission.
internal sealed class PreparationPrintedFixture
{
    internal CardCatalog Cards { get; } = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal AbilityRunner Runner { get; }
    internal World World { get; }
    internal Card Hero { get; }
    internal Card Bite { get; }
    internal Card Suit { get; }
    internal Card Villain { get; }
    internal Card Minion { get; }
    internal List<GameEvent> Events { get; } = [];

    private static AbilityRunner CreateRunner(bool boardRead, string? interruptEffect, string? tail)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = root["cards"]!.AsArray();
        var bite = rows.Single(row => row!["card"]!.GetValue<string>() == "08010")!;
        // Synthetic defeat interrupt isolates suspension; the printed chain uses actual rows.
        var observer = System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"08023","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
              "effect":{"choose":{"options":[
                {"giveStatus":{"card":"you","status":"confused"}},
                {"giveStatus":{"card":"you","status":"stunned"}}
              ]}}
            }]}
            """)!;
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "08023"));
        rows.Add(observer);
        if (tail is not null)
            bite["abilities"]![0]!["effect"]!["attack"]!["effect"]!["seq"]![1] =
                System.Text.Json.Nodes.JsonNode.Parse(tail);
        if (boardRead)
            bite["abilities"]![0]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray()
                .Add(System.Text.Json.Nodes.JsonNode.Parse("""{"dealDamage":{"cards":"you","amount":{"count":{"cardsIn":{"area":"encounterDeck"}}}}}"""));
        if (interruptEffect is not null)
            observer["abilities"]![0]!["effect"] = System.Text.Json.Nodes.JsonNode.Parse(interruptEffect);
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    internal PreparationPrintedFixture(string minion = "01167", bool reveal = false,
        bool boardRead = false, string? interruptEffect = null, string? tail = null, int entrySeat = 0)
    {
        Runner = CreateRunner(boardRead, interruptEffect, tail);
        World = new World(Cards, 2, 12345) { Abilities = Runner };
        var first = World.CreateSeat("Spider-Man");
        first.IdentityCard = World.CreateCard("01001a", first.Hero);
        var second = World.CreateSeat("Black Widow");
        Hero = second.IdentityCard = World.CreateCard("08001a", second.Hero);
        Hero.Exhaust();
        for (int i = 0; i < 4; i++)
        {
            World.CreateCard("01087", first.Deck);
            World.CreateCard("01087", second.Deck);
            World.CreateCard("01186", World.AreaOf(DeckType.EncounterDeck));
        }
        Villain = World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
        Bite = World.CreateCard("08010", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Suit = World.CreateCard("08009", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        World.CreateCard("08009", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Minion = World.CreateCard(minion, World.AreaOf(DeckType.RevealingArea));
        if (reveal)
            World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
                1, 2, Subject: Minion.ObjectId, Seat: entrySeat));
        else EncounterEntry.Schedule(World, EncounterEntryRequest.For(Minion, entrySeat, new CardEntryPlacement()), new EffectCompletion(1, 1));
    }
}
