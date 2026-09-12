using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class StatusTestBase
{
    protected sealed class PreventThree : NoCardAbilities
    {
        public override long WouldTake(World world, Card target, Card source, long amount, List<Marvel.Rules.Events.GameEvent> events) => target.FaceId == "minion" ? Math.Max(0, amount - 3) : amount;
    }

    protected sealed class GrantsStalwart(int source, int target) : NoCardAbilities
    {
        public override IReadOnlyList<Marvel.Rules.Timing.ContinuousEffect> Constant(World world, Card card) => card.ObjectId == source ? [new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.ConstantAbility, "stalwart", Amount: 1, Card: source, Affects: target, Lasts: Marvel.Rules.Timing.Duration.WhileInPlay)] : [];
    }

    /// <summary>Grants a keyword the way a card ability does.</summary>
    protected static void Grant(World world, Card card, string keyword) => world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.LastingEffect, Kind: keyword, Card: card.ObjectId, Affects: card.ObjectId));
    protected static World Board(Printed printed, int players = 1)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("alterego,hero", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
            identity.TurnTo("hero");
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("filler", world.Seats[0].Deck);
        return world;
    }

    protected static Printed Cards() => new Printed().With("hero", ("ATK", "2"), ("THW", "2"), ("HP", "10")).With("villain", ("ATK", "3"), ("SCH", "2"), ("HP", "20")).With("scheme", ("EscalationThreat", "1"), ("TargetThreat", "99")).With("boost", ("Boost", "0"));
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
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "minion" => CardKind.Minion,
            "ally" => CardKind.Ally,
            "tough" or "stunned" or "confused" => CardKind.Status,
            _ => CardKind.Treachery,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out string? value) && long.TryParse(value, out long number) ? number : fallback;
        public long ConsequentialDamage(string faceId, string attribute) => attribute == "ATK" ? PrintedValue(faceId, "AtkIcons", 1) : 0;
    }
}
