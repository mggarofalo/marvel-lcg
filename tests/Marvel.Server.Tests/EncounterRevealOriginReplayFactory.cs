using System.Text.Json.Nodes;
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

// Synthetic rows use the existing durable factory seam without pack admission.
internal sealed class EncounterRevealOriginReplayFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Outer { get; private set; }
    internal int Inner { get; private set; }
    internal int NestedReveal { get; private set; }
    internal int Ward { get; private set; }
    internal int Payment { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var replacements = JsonNode.Parse("""
            [
             {"card":"09007","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game","form":"hero"},
              "when":{"and":[{"isKind":{"card":"trigger.subject","kind":"treachery"}},{"revealedFromEncounterDeck":"trigger"}]},
              "effect":{"seq":[{"cancelOccurrence":"trigger"},{"discard":"trigger.subject"}]}}]},
             {"card":"01075","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game"},
              "when":{"isTitle":{"card":"trigger.subject","title":"Shadow of the Past"}},
              "cost":{"exhaust":"this"},
              "effect":{"reveal":{"cardsIn":{"area":"encounterDiscardPile"}}}}]},
             {"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}}]}
            ]
            """)!.AsArray();
        foreach (var row in replacements)
        {
            var rows = book["cards"]!.AsArray();
            var existing = rows.SingleOrDefault(r => (string?)r!["card"] == (string?)row!["card"]);
            if (existing is not null) rows.Remove(existing);
            rows.Add(row!.DeepClone());
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = new World(cards, 1, specification.Seed) { Abilities = runner };
        var seat = world.CreateSeat("Peter");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        for (int i = 0; i < 20; i++)
        {
            world.CreateCard("01087", seat.Deck);
            world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        NestedReveal = world.CreateCard("01075", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Ward = world.CreateCard("09007", seat.Hand).ObjectId;
        Payment = world.CreateCard("01088", seat.Hand).ObjectId;
        // The encounter card is deliberately dealt from the player deck. The
        // current ruling treats it as eligible without persistent card history.
        var outer = world.CreateCard("01190", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(outer, seat.Deck);
        Outer = outer.ObjectId;
        Inner = world.CreateCard("01105", world.AreaOf(DeckType.EncounterDiscardPile)).ObjectId;
        var events = new List<GameEvent>();
        Deal.EncounterCard(world, outer, 0, "synthetic non-encounter-deck deal", events);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            events.AddRange(Game.Resolve(Decision.Take(prompt.Affordances.Single(a =>
                a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id)).Events);
        return new(Game, events);
    }
}
