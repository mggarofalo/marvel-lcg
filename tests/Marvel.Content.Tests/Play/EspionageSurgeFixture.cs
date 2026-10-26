using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Espionage with synthetic keyword/order controls; the pack stays unadmitted.
internal sealed class EspionageSurgeFixture
{
    internal CardCatalog Cards { get; } = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal AbilityRunner Runner { get; }
    internal World World { get; }
    internal List<GameEvent> Events { get; } = [];
    internal Card Encounter { get; }
    internal Card Hero { get; }
    internal Card[] Espionage { get; }
    internal Card? Suit { get; }

    internal EspionageSurgeFixture(int resolver = 1, int controller = 1, int copies = 1,
        bool suit = false, bool printed = false, bool gained = false, bool cancel = false)
    {
        Runner = CreateRunner(printed, gained, cancel);
        World = new World(Cards, 2, 12345) { Abilities = Runner };
        var first = World.CreateSeat("Peter");
        first.IdentityCard = World.CreateCard("01001a", first.Hero);
        var second = World.CreateSeat("Natasha");
        Hero = second.IdentityCard = World.CreateCard("08001a", second.Hero);
        Hero.Exhaust();
        for (int i = 0; i < 12; i++)
        {
            World.CreateCard("01087", first.Deck);
            World.CreateCard("01087", second.Deck);
            World.CreateCard("01186", World.AreaOf(DeckType.EncounterDeck));
        }
        World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
        World.CreateCard("01097a", World.AreaOf(DeckType.MainSchemesArea));
        Espionage = Enumerable.Range(0, copies).Select(_ => World.CreateCard("08033",
            World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(controller), cardOwner: controller))).ToArray();
        if (suit) Suit = World.CreateCard("08009", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        if (cancel) World.CreateCard("08018", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Encounter = World.CreateCard(gained ? "01167" : "01121", World.AreaOf(DeckType.RevealingArea));
        World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: Encounter.ObjectId, Seat: resolver));
    }

    private static AbilityRunner CreateRunner(bool printed, bool gained, bool cancel)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        // This keyword fixture isolates Surge and the actual Synth-Suit response.
        // Widow's Bite is covered by the separate Preparation composition tests.
        var identity = root["cards"]!.AsArray().Single(r => (string?)r!["card"] == "08001a")!;
        identity["abilities"] = new System.Text.Json.Nodes.JsonArray();
        if (cancel)
        {
            // Synthetic cancellation-only control explicitly replaces actual Spycraft.
            var cards = root["cards"]!.AsArray();
            cards.Remove(cards.Single(row => (string?)row!["card"] == "08018"));
            root["cards"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
                {"card":"08018","abilities":[{
                  "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game"},
                  "cost":{"discard":"this"},"effect":{"cancelWhenRevealed":"trigger.subject"}
                }]}
                """));
        }
        if (printed || gained)
        {
            string replaced = gained ? "01167" : "01121";
            var existing = root["cards"]!.AsArray().FirstOrDefault(r => (string?)r!["card"] == replaced);
            if (existing is not null) root["cards"]!.AsArray().Remove(existing);
            root["cards"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse(gained
                ? """{"card":"01167","abilities":[{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},"effect":{"gainSurge":1}}]}"""
                : """{"card":"01121","abilities":[{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},"effect":{"draw":{"player":"you","count":1}}}]}"""));
        }
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    internal Prompt Work() => Assert.IsType<Prompt>(Sequence.Work(World, Cards, Runner, Events));
    internal void Answer(Prompt prompt, int card) => Sequence.Answer(World, Cards, Runner, prompt,
        Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == card).Id), Events);
    internal void Decline(Prompt prompt) => Sequence.Answer(World, Cards, Runner, prompt, Decision.Decline, Events);
    internal void Finish()
    {
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(World, Cards, Runner, Events);
            if (prompt is null) return;
            Assert.True(prompt.Cancellable);
            Decline(prompt);
        }
        throw new InvalidOperationException("encounter did not finish");
    }
}


