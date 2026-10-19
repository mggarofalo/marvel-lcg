using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Printed expansion cards on a synthetic board; not product scenario admission.</summary>
internal sealed record AbsorbingManFixture(World World, AbilityRunner Runner, Card Villain, Card Scheme)
{
    internal static AbsorbingManFixture Create(bool hero = false, int players = 1, string extra = "")
    {
        var book = JsonNode.Parse(AbsorbingManFragment.Json)!;
        if (extra.Length > 0) book["cards"]!.AsArray().Add(JsonNode.Parse(extra));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (_, world) = Playing(_ => { }, hero: hero,
            heroes: players == 1 ? ["spider_man"] : ["spider_man", "captain_marvel"], abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(scheme, world.AreaOf(DeckType.RemovedArea));
        villain = world.CreateCard("04076", world.AreaOf(DeckType.VillainArea));
        scheme = world.CreateCard("04079b", world.AreaOf(DeckType.MainSchemesArea));
        return new(world, runner, villain, scheme);
    }

    internal Card Card(string face, DeckType zone = DeckType.RevealingArea) =>
        World.CreateCard(face, World.AreaOf(zone));

    internal void Reveal(string face, int player = 0) => Runner.ExecuteReveal(World, Card(face), player);

    internal List<GameEvent> Finish(Func<Prompt, Decision>? answer = null)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(World, World.Facts, Runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Xunit.Assert.True(step < 30, prompt.Description);
            if (answer is null && !prompt.Cancellable) throw new InvalidOperationException("Test must answer mandatory choice: " + prompt.Description);
            Sequence.Answer(World, World.Facts, Runner, prompt, answer?.Invoke(prompt) ?? Decision.Decline, events);
            prompt = Sequence.Work(World, World.Facts, Runner, events);
        }
        return events;
    }
}
