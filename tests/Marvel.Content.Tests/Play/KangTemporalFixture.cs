using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// A nonpublic Rhino board with the full authored book for the Kang modular
/// encounter faces, whose public admission stays closed.
/// </summary>
internal static class KangTemporalFixture
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>
    /// Seat 0 is Captain Marvel and every later seat Spider-Man, each with twelve
    /// Energy in deck; the encounter deck holds <paramref name="encounter"/> copies
    /// of Hard to Keep Down, which prints no boost field.
    /// </summary>
    internal static (World World, RedSkullFixture Fixture) Board(int players, int encounter = 6, uint seed = 0)
    {
        var world = new World(Cards, players, seed);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01010a,01010b" : "01001a,01001b", seat.Hero);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < encounter; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner));
    }

    internal static Card Engaged(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));

    /// <summary>Places <paramref name="count"/> copies of Zola's Algorithm in the player's obligations area.</summary>
    internal static void Obligations(World world, int player, int count)
    {
        for (int i = 0; i < count; i++)
            world.CreateCard("04163", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)));
    }

    internal static void Attack(World world, Card enemy, int player) => Activate(world, enemy, false, player);

    /// <summary>Queues one enemy activation against <paramref name="player"/>.</summary>
    internal static void Activate(World world, Card enemy, bool scheme, int player) =>
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 2, Index: player, Subject: enemy.ObjectId, Seat: player));
}
