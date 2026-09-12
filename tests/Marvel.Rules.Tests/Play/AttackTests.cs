using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class AttackTestBase
{
    /// <summary>Runs the attack, declining every question.</summary>
    /// <remarks>
    /// Bounded, because the failure this is most likely to meet is a step that
    /// asks the same question forever — and a test that hangs says far less
    /// than one that fails.
    /// </remarks>
    protected static void Finish(World world, ICardFacts facts, ICardAbilities? abilities = null)
    {
        var events = new List<GameEvent>();
        abilities ??= new NoCardAbilities();
        var asked = Sequence.Work(world, facts, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked after 10 answers");
            Sequence.Answer(world, facts, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, abilities, events);
        }
    }

    /// <summary>A villain, one hero-form identity per seat, one boost card.</summary>
    protected static World Board(ICardFacts facts, int players = 1)
    {
        var world = new World(facts, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard = world.CreateCard("hero", world.Seats[seat].Hero);
        }

        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("filler", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(Steps.Attack, Round: 1, Number: 2, Index: 0, Subject: villain.ObjectId, Seat: 0));
        return world;
    }

    protected static Facts Printed(int atk, int boost, int def = 0) => new(atk, boost, def);
    protected sealed class CombatWindowObserver : NoCardAbilities
    {
        public bool SawAttackInitiation { get; private set; }
        public bool SawDamageWouldBeDealt { get; private set; }
        public bool SawDamageDealt { get; private set; }
        public bool SawBoostCardsFlipped { get; private set; }
        public bool SawAttackEnds { get; private set; }
        public int AttackInitiationInterrupts { get; private set; }
        public int AttackInitiationResponses { get; private set; }
        public List<EnemyActivation> CompletedActivations { get; } = [];

        public override IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window)
        {
            SawAttackInitiation |= occurrence.Is(Steps.AttackInitiated);
            if (occurrence.Is(Steps.AttackInitiated))
            {
                if (window == WindowKind.Interrupt)
                {
                    AttackInitiationInterrupts += 1;
                }
                else
                {
                    AttackInitiationResponses += 1;
                }
            }

            SawDamageWouldBeDealt |= window == WindowKind.Interrupt && occurrence.Is(Steps.DamageWouldBeDealt);
            SawDamageDealt |= window == WindowKind.Response && occurrence.Is(Steps.DamageDealt);
            SawBoostCardsFlipped |= occurrence.Is("WhenBoostCardsFlipped");
            SawAttackEnds |= occurrence.Is(Steps.AttackEnds);
            return[];
        }

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            CompletedActivations.Add(result);
            return[];
        }
    }

    protected sealed class RequiresAlly(int player) : NoCardAbilities
    {
        public override DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
        {
            var allies = candidates.Where(card => card.Owner == player && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
            return allies.Count > 0 ? new DefenderChoice(allies, Required: true) : new DefenderChoice(candidates, Required: false);
        }
    }

    protected sealed class CompletionRecorder : NoCardAbilities
    {
        public List<EnemyActivation> Results { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            Results.Add(result);
            return[];
        }
    }

    protected sealed class DefeatRecorder : NoCardAbilities
    {
        public List<int> Defeated { get; } = [];

        public override IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated)
        {
            Defeated.Add(card.ObjectId);
            return[];
        }
    }

    protected sealed class BoostOrderRecorder : NoCardAbilities
    {
        public List<string> Faces { get; } = [];
        public List<int> DiscardedBeforeResolution { get; } = [];

        public override IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
        {
            Assert.Equal(DeckType.BoostingArea, card.Area.Type);
            Assert.True(card.FaceUp);
            Faces.Add(card.FaceId);
            DiscardedBeforeResolution.Add(world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
            return[];
        }
    }

    protected sealed class DamagingBoost : NoCardAbilities
    {
        public override IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
        {
            var events = new List<GameEvent>();
            DamagePlacement.Deal(world, world.Facts, card, world.Seats[player].IdentityCard, 2, "Boost", "Deal_Damage", events);
            return events;
        }
    }

    protected sealed class IconChangingBoost : NoCardAbilities
    {
        public override IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
        {
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "boost_const", Amount: 2, Card: card.ObjectId, Affects: card.ObjectId));
            return[];
        }
    }

    protected sealed class StunSensitiveDamage(int identity, int ally) : NoCardAbilities
    {
        public override bool CanTakeDamage(World world, Card target, Card source) => target.ObjectId != ally || !Statuses.Has(world, world.Cards[identity], Statuses.Stunned);
    }

    protected sealed class Facts(int atk, int boost, int def) : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal)
        {
            ["villain"] = new(StringComparer.Ordinal)
            {
                ["ATK"] = atk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["HP"] = "16",
            },
            ["boost"] = new(StringComparer.Ordinal)
            {
                ["Boost"] = boost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            ["amplify"] = new(StringComparer.Ordinal)
            {
                ["Amplify"] = "1"
            },
            ["hero"] = new(StringComparer.Ordinal)
            {
                ["HP"] = "10",
                ["DEF"] = def.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            ["ally"] = new(StringComparer.Ordinal)
            {
                ["HP"] = "3"
            },
            ["alter"] = new(StringComparer.Ordinal)
            {
                ["HP"] = "9"
            },
        };
        private readonly Dictionary<string, CardKind> kinds = new(StringComparer.Ordinal)
        {
            ["hero"] = CardKind.Hero,
            ["villain"] = CardKind.EncounterVillain,
            ["boost"] = CardKind.Treachery,
            ["amplify"] = CardKind.EncounterSideScheme,
            ["ally"] = CardKind.Ally,
            ["alter"] = CardKind.AlterEgo,
        };
        public CardKind Kind(string faceId) => kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out var printed) && long.TryParse(printed, out long value) ? value : fallback;
    }
}
