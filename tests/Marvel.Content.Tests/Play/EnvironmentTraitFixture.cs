using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Printed Absorbing Man constants on a synthetic board, not scenario admission.</summary>
internal sealed record EnvironmentTraitFixture(World World, Card Villain)
{
    private static readonly string[] Stages = ["04076", "04077", "04078"];

    internal static EnvironmentTraitFixture Create(string stage = "04076", string extra = "")
    {
        string rows = string.Join(",", Stages.Select(face => $$$$"""
            {"card":"{{{{face}}}}","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"gainEnvironmentTraits":"this"}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{{{{rows}}}}{{{{extra}}}}]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 17);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        for (int index = 0; index < 8; index++) world.CreateCard("01003", seat.Deck);
        var villain = world.CreateCard(stage, world.AreaOf(DeckType.VillainArea));
        _ = Game.Begin(world, facts, runner);
        return new(world, villain);
    }

    internal Card Environment(string face, DeckType zone = DeckType.EnvironmentArea) =>
        World.CreateCard(face, World.AreaOf(zone));

    internal IReadOnlyList<string> Traits() => Marvel.Rules.State.Traits.Of(World, Villain, World.Facts);
}
