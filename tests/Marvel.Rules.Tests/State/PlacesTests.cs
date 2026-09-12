using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.State;
public abstract class PlacesTestBase
{
    // ------------------------------------------------------------------ boards
    /// <summary>A world with seats and the single default game area.</summary>
    protected static World Ordinary(int players)
    {
        var world = new World(new Printed(), players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
        }

        return world;
    }

    /// <summary>Kang's split: one game area per player, plus the villain's.</summary>
    protected static (GameArea Mine, GameArea Theirs) Split(World world)
    {
        var mine = world.CreateGameArea();
        var theirs = world.CreateGameArea();
        Join(world, PlayArea.Of(0), mine);
        Join(world, PlayArea.Of(1), theirs);
        return (mine, theirs);
    }

    protected static List<GameEvent> Join(World world, PlayArea area, GameArea destination)
    {
        var events = new List<GameEvent>();
        world.Join(area, destination, "test", events);
        return events;
    }

    protected static Card MainScheme(World world, PlayArea where) => InPlayArea(world, DeckType.MainSchemesArea, where);
    protected static Card InPlayArea(World world, DeckType type, PlayArea where) => world.CreateCard("01097b", world.CreateArea(type, where.Player, where));
    /// <summary>Enough printed data to make a card. None of it is read here.</summary>
    protected sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
