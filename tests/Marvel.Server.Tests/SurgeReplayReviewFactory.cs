using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

/// <summary>Test-local full encounter queue using the existing durable factory seam.</summary>
internal sealed class SurgeReplayReviewFactory(bool gained) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Preparation { get; private set; }
    internal int Suit { get; private set; }
    internal int Encounter { get; private set; }
    internal int Extra { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var additions = System.Text.Json.Nodes.JsonNode.Parse("""
            {"cards":[
              {"card":"08033","abilities":[{
                "trigger":{"event":"WhenSurgeWouldResolve","timing":"Interrupt","subject":"game"},
                "cost":{"discard":"this"},"effect":{"draw":{"player":"you","count":2}}
              }]},
              {"card":"08009","abilities":[{
                "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you","form":"hero"},
                "cost":{"exhaust":"this"},"effect":{"ready":"you"}
              }]},
              {"card":"01104","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
                "effect":{"seq":[{"gainSurge":1},{"giveStatus":{"card":"you","status":"confused"}}]}
              }]}
            ]}
            """)!["cards"]!.AsArray();
        var book = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        foreach (var row in additions)
        {
            var existing = book["cards"]!.AsArray().FirstOrDefault(r => (string?)r!["card"] == (string?)row!["card"]);
            if (existing is not null) book["cards"]!.AsArray().Remove(existing);
            book["cards"]!.AsArray().Add(row!.DeepClone());
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = new World(cards, 1, specification.Seed) { Abilities = runner };
        var seat = world.CreateSeat("Peter");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        seat.IdentityCard.Exhaust();
        for (int i = 0; i < 20; i++)
        {
            world.CreateCard("01087", seat.Deck);
            world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        Preparation = world.CreateCard("08033", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Suit = world.CreateCard("08009", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Encounter = world.CreateCard(gained ? "01104" : "01121",
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0))).ObjectId;
        Extra = world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck)).ObjectId;
        // Synthetic setup schedules the complete villain-step-four queue. No public admission changes.
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        var events = new List<GameEvent>();
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            events.AddRange(Game.Resolve(Decision.Take(prompt.Affordances.Single(a =>
                a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id)).Events);
        return new(Game, events);
    }
}
