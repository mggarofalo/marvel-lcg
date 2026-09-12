using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class CardPlayTestBase
{
    /// <summary>One object id of a card in hand with the given face.</summary>
    protected static int Pay(World world, string faceId) => world.Seats[0].Hand.Cards.First(card => card.FaceId == faceId).ObjectId;
    protected static Card InHand(World world, string faceId) => world.CreateCard(faceId, world.Seats[0].Hand);
    /// <summary>Clears the hand, so that a test can say what is in it.</summary>
    protected static void Empty(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[0].Deck);
        }
    }

    /// <summary>Two players, each with an empty hand to fill.</summary>
    protected static World Table(Printed printed)
    {
        var world = new World(printed, players: 2);
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard = world.CreateCard("alterego,hero", world.Seats[seat].Hero);
            // A deck with cards in it, for the same reason `Board` has one:
            // `rr:player-deck.4` would otherwise reset an empty deck the moment
            // a payment reached the discard pile.
            for (int card = 0; card < 5; card++)
            {
                world.CreateCard("filler", world.Seats[seat].Deck);
            }
        }

        return world;
    }

    protected static World Board(Printed printed)
    {
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("alterego,hero", world.Seats[0].Hero);
        // A deck with cards in it, because an empty one plus a discard pile
        // gaining its first card is `rr:player-deck.4` -- the deck resets and
        // the card just discarded goes straight back into it, which is correct
        // and not what any of these tests is about.
        for (int card = 0; card < 5; card++)
        {
            world.CreateCard("res", world.Seats[0].Deck);
        }

        for (int card = 0; card < 4; card++)
        {
            world.CreateCard("res", world.Seats[0].Hand);
        }

        return world;
    }

    protected static Printed Cards() => new Printed().With("res", ("RES", "GG")).With("ally", ("Cost", "1"), ("RES", "R")).With("bruiser", ("Cost", "3"), ("HP", "3"), ("Toughness", "1")).With("upgrade", ("Cost", "2"), ("RES", "B")).With("support", ("Cost", "0"), ("RES", "B")).With("event", ("Cost", "0"), ("RES", "Y")).With("free", ("Cost", "0"), ("RES", "Y")).With("expensive", ("Cost", "9"), ("RES", "B")).With("suited2", ("Form", "Suit"))// `rr:requirement-resources` -- thirteen cards in the pool print one.
    // `27006` requires a mental, `27016` a physical, `27049` one of each of
    // energy, mental and physical.
    .With("physical", ("RES", "R")).With("mental", ("RES", "B")).With("demanding", ("Cost", "1"), ("RES", "R"), ("Requirement", "R"))// `rr:team-up` -- 28 cards print one, and every one names two heroes.
    .With("swarm", ("Cost", "0"), ("TeamUp", "Ant-Man;Wasp")).With("Ant-Man", ("HP", "9")).With("Wasp", ("HP", "9")).With("Janet", ("HP", "3")).Sub("Janet", "Wasp").With("panther", ("Cost", "0"), ("TeamUp", "Black Panther/T'Challa;Black Panther/Shuri")).With("Black Panther", ("HP", "9")).With("T'Challa", ("HP", "9")).With("Shuri", ("HP", "9"))// `rr:alliance` -- 13 cards print one, and every one of them is a card
    // about a table.
    .With("together", ("Cost", "3"), ("Alliance", "1")).With("alone", ("Cost", "3"));
    protected sealed class Silent : NoCardAbilities
    {
        public override IReadOnlyList<GameEvent> Resolve(World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];
    }

    protected sealed class Counting : NoCardAbilities
    {
        public int Resolved { get; private set; }

        public override IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
        {
            Resolved += 1;
            return[];
        }

        public override IReadOnlyList<GameEvent> Resolve(World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];
    }

    protected sealed class Targets(params int[] targets) : NoCardAbilities
    {
        public override IReadOnlyList<int>? AttachmentTargets(World world, Card card) => targets;
    }

    protected sealed class ConditionalResources(string matching) : NoCardAbilities
    {
        public override string ResourcesGeneratedBy(World world, Card source, Card? payingFor) => payingFor?.FaceId == matching ? Resources.GeneratedBy(source.FaceId, world.Facts) + Resources.Wild : Resources.GeneratedBy(source.FaceId, world.Facts);
    }

    protected sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> subtitles = new(StringComparer.Ordinal);
        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found) ? found : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var(key, value)in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "res" => CardKind.Resource,
            "ally" or "bruiser" => CardKind.Ally,
            "event" => CardKind.Event,
            "support" => CardKind.Support,
            _ => CardKind.Upgrade,
        };
        /// <summary>A printed subtitle — `rr:team-up.2` matches on it too.</summary>
        public Printed Sub(string faceId, string subtitle)
        {
            subtitles[faceId] = subtitle;
            return this;
        }

        public string Subtitle(string faceId) => subtitles.TryGetValue(faceId, out string? found) ? found : string.Empty;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out string? value) && long.TryParse(value, out long number) ? number : fallback;
        public string? FormKeyword(string faceId) => Attributes(faceId).TryGetValue("Form", out string? form) ? form.ToLowerInvariant() : null;
        public string? RequiredForm(string faceId) => Attributes(faceId).TryGetValue("RequiredForm", out string? form) ? form.ToLowerInvariant() : null;
    }
}
