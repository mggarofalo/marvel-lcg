using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class BasicPowerTestBase
{
    protected static int[] Ids(IReadOnlyList<Card> cards) => [..cards.Select(card => card.ObjectId).Order()];
    protected sealed class ProtectedScheme : NoCardAbilities
    {
        private readonly int protectedScheme;
        private readonly int[] sources;

        public ProtectedScheme(int scheme, params int[] sources)
        {
            protectedScheme = scheme;
            this.sources = sources;
        }

        public override bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => scheme.ObjectId != protectedScheme || sources.Length > 0 && sources.All(source => source == ignoredSource);
    }

    protected sealed class RecordingCardPowers : NoCardAbilities
    {
        public List<int> Targets { get; } = [];
        public List<long> Amounts { get; } = [];

        public override void ResolveCardAttack(World world, CharacterAttack attack, Marvel.Rules.Timing.Occurrence occurrence, List<GameEvent> events)
        {
            Targets.Add(attack.Enemy);
            Amounts.Add(attack.Amount);
        }
    }

    protected sealed class StarredPowerObserver(int identity) : NoCardAbilities
    {
        public bool Checked { get; private set; }

        public override IReadOnlyList<Marvel.Rules.Timing.PendingAbility> Waiting(World world, Marvel.Rules.Timing.Occurrence occurrence, Marvel.Rules.Timing.WindowKind window)
        {
            Checked |= window == Marvel.Rules.Timing.WindowKind.Response && occurrence.Is(Steps.AttackEnds) && occurrence.Actor == identity;
            return[];
        }
    }

    /// <summary>A villain, a main scheme, and one identity per seat.</summary>
    protected static World Board(Printed printed, int players = 1, bool hero = true)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("alterego,hero", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
            if (hero)
            {
                identity.TurnTo("hero");
            }
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    protected sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);
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
            "ally" => CardKind.Ally,
            "scheme" => CardKind.MainScheme,
            "minion" => CardKind.Minion,
            "upgrade" => CardKind.Attachment,
            "attachment" => CardKind.Attachment,
            "tough" or "stunned" => CardKind.Status,
            _ => CardKind.EncounterVillain,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out string? value) && long.TryParse(value.TrimEnd('*'), out long number) ? number : fallback;
        public long ConsequentialDamage(string faceId, string attribute) => faceId == "ally" ? 1 : 0;
        /// <summary>`villain` and `villain2` are two stages of one character.</summary>
        public string Title(string faceId) => faceId.StartsWith("villain", StringComparison.Ordinal) ? "villain" : faceId;
    }
}
