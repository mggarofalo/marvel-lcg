using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

// Test-local printed interpretations; this does not admit the Black Widow product.
internal sealed class PreparationPrintedFixture
{
    internal CardCatalog Cards { get; } = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private const string Rows = """
        {"cards":[
          {"card":"08010","abilities":[{
            "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"game","form":"hero"},
            "when":{"isKind":{"card":"trigger.subject","kind":"minion"}},
            "cost":{"discard":"this"},"labels":["attack"],
            "effect":{"attack":{"target":"trigger.subject","effect":{"seq":[
              {"dealAttackDamage":{"cards":"chosen","amount":2}},
              {"if":{"test":{"isInPlay":"chosen"},
                "then":{"giveStatus":{"card":"chosen","status":"stunned"}}}}
            ]}}}
          }]},
          {"card":"08001a","abilities":[{
            "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
            "effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"dealDamage":{"cards":"chosen","amount":1}}}}
          }]},
          {"card":"08023","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"choose":{"options":[
              {"giveStatus":{"card":"you","status":"confused"}},
              {"giveStatus":{"card":"you","status":"stunned"}}
            ]}}
          }]},
          {"card":"08009","abilities":[{
            "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you","form":"hero"},
            "cost":{"exhaust":"this"},"effect":{"ready":"you"}
          }]}
        ]}
        """;
    internal AbilityRunner Runner { get; }
    internal World World { get; }
    internal Card Hero { get; }
    internal Card Bite { get; }
    internal Card Suit { get; }
    internal Card Villain { get; }
    internal Card Minion { get; }
    internal List<GameEvent> Events { get; } = [];

    private static AbilityRunner CreateRunner(bool boardRead, string? interruptEffect)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = System.Text.Json.Nodes.JsonNode.Parse(Rows)!["cards"]!.AsArray();
        if (boardRead)
            rows[0]!["abilities"]![0]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray()
                .Add(System.Text.Json.Nodes.JsonNode.Parse("""{"dealDamage":{"cards":"you","amount":{"count":{"cardsIn":{"area":"encounterDeck"}}}}}"""));
        if (interruptEffect is not null)
            rows[2]!["abilities"]![0]!["effect"] = System.Text.Json.Nodes.JsonNode.Parse(interruptEffect);
        foreach (var row in rows)
            root["cards"]!.AsArray().Add(row!.DeepClone());
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    internal PreparationPrintedFixture(string minion = "01167", bool reveal = false,
        bool boardRead = false, string? interruptEffect = null)
    {
        Runner = CreateRunner(boardRead, interruptEffect);
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
                1, 2, Subject: Minion.ObjectId, Seat: 0));
        else EncounterEntry.Schedule(World, EncounterEntryRequest.For(Minion, 0, new CardEntryPlacement()), new EffectCompletion(1, 1));
    }
}
