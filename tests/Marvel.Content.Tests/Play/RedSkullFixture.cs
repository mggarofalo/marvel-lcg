using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Red Skull setup recipe with a candidate ability fragment; not product admission.</summary>
internal sealed record RedSkullFixture(World World, AbilityRunner Runner)
{
    internal static AbilityRunner Abilities()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var fragment = JsonNode.Parse(RedSkullFragment.Json)!;
        var rows = book["cards"]!.AsArray();
        foreach (var row in fragment["cards"]!.AsArray())
        {
            foreach (var existing in rows.Where(existing => (string?)existing!["card"] == (string?)row!["card"]).ToArray())
                rows.Remove(existing);
            rows.Add(row!.DeepClone());
        }
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    internal static RedSkullFixture Create(bool hero = true, int players = 1)
    {
        var runner = Abilities();
        string[] heroes = players == 1 ? ["spider_man"] : ["spider_man", "captain_marvel"];
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "red_skull", heroes), CardCatalogData),
            [.. heroes.Select(name => SetupCatalogData.Hero(name).Name)], 12345, runner);
        var game = Game.Begin(world, CardCatalogData, runner);
        while (game.Pending is { } prompt && prompt.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(prompt.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        if (hero) world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        return new(world, runner);
    }

    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;
    internal Card Main => World.TheCardIn(DeckType.MainSchemesArea)!;
    internal Card Card(string face, DeckType zone = DeckType.RevealingArea) => World.CreateCard(face, World.AreaOf(zone));
    internal Card Reveal(string face, int player = 0)
    {
        var card = Card(face);
        World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, 2, Subject: card.ObjectId, Seat: player));
        return card;
    }

    internal List<GameEvent> Finish(Func<Prompt, Decision>? answer = null)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(World, World.Facts, Runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 60, prompt.Description);
            Decision decision;
            if (prompt.Cancellable) decision = Decision.Decline;
            else if (prompt.Affordances is [{ Verb: "Order", Targets: { } targets } order])
                decision = new Decision(order.Id, targets.Legal.ToArray());
            else decision = answer?.Invoke(prompt) ?? throw new InvalidOperationException(prompt.Description);
            Sequence.Answer(World, World.Facts, Runner, prompt, decision, events);
            prompt = Sequence.Work(World, World.Facts, Runner, events);
        }
        return events;
    }
}
