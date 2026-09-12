using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class VillainPhaseTestBase
{
    /// <summary>Schedules the villain phase and walks it to the end.</summary>
    protected static List<GameEvent> Run(World world, Printed printed, ICardAbilities? abilities = null)
    {
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, abilities ?? new NoCardAbilities(), events);
        return events;
    }

    /// <summary>Records which player each completed villain activation targeted.</summary>
    protected sealed class ActivationObserver : NoCardAbilities
    {
        public List<int> Players { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            Players.Add(result.Player);
            return[];
        }
    }

    protected sealed class EnemyOrderObserver : NoCardAbilities
    {
        public List<string> Enemies { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            Enemies.Add(world.Cards[result.Enemy].FaceId);
            return[];
        }
    }

    protected sealed class EngageAfterVillain(int villain) : NoCardAbilities
    {
        public List<string> Completed { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            Completed.Add(world.Cards[result.Enemy].FaceId);
            if (result.Enemy == villain)
            {
                world.CreateCard("arriving", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(result.Player)));
            }

            return[];
        }
    }

    protected sealed class BoostWindowOffer(int card) : NoCardAbilities
    {
        public bool SawBoostFlipped { get; private set; }
        public bool SawThreat { get; private set; }
        public bool SawSchemeEnds { get; private set; }

        public override IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window)
        {
            SawBoostFlipped |= occurrence.Is("WhenBoostCardsFlipped");
            SawThreat |= occurrence.Is(Steps.ThreatWouldBePlaced);
            SawSchemeEnds |= occurrence.Is(Steps.SchemeEnds);
            return window == WindowKind.Interrupt && occurrence.Is("WhenBoostCardGiven") ? [new PendingAbility(card, AbilityType.Interrupt, 0)] : [];
        }

        public override Affordance Describe(World world, PendingAbility ability) => new(77, "Interrupt", ability.Card, 0, "interrupt boost");
    }

    /// <summary>A villain, a main scheme, one identity per seat, two encounter cards each.</summary>
    protected static World Board(Printed printed, int players)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("identity", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        // The deck is drawn from the top, which is the end of the list, so the
        // boost card has to be appended last to be taken first.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateCard("encounter", deck);
            world.CreateCard("boost", deck);
        }

        return world;
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    protected sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> traits = new(StringComparer.Ordinal);
        public Dictionary<string, CardKind> Kinds { get; } = new(StringComparer.Ordinal)
        {
            ["identity"] = CardKind.AlterEgo,
            ["villain"] = CardKind.EncounterVillain,
            ["scheme"] = CardKind.MainScheme,
            ["boost"] = CardKind.Treachery,
            ["encounter"] = CardKind.Treachery,
            ["minion"] = CardKind.Minion,
            ["charge"] = CardKind.Attachment,
        };

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found) ? found : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var(key, value)in values)
            {
                table[key] = value;
            }

            return this;
        }

        public Printed WithTrait(string faceId, string trait)
        {
            if (!traits.TryGetValue(faceId, out var found))
            {
                traits[faceId] = found = [];
            }

            found.Add(trait);
            return this;
        }

        public CardKind Kind(string faceId) => Kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;
        public IReadOnlyList<string> Traits(string faceId) => traits.TryGetValue(faceId, out var found) ? found : [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out string? printed) && long.TryParse(printed, out long value) ? value : fallback;
    }
}
