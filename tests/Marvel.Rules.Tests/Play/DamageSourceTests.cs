using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class DamageSourceTests
{
    [Rule("rr:cannot")]
    [Rule("rr:cannot.1")]
    [Fact]
    public void ForbiddenDamageDoesNotOpenReplacementHandlingOrSpendTough()
    {
        // "Cannot" is absolute. The prohibited instance never becomes damage
        // the target would be dealt or take, so neither a replacement effect
        // nor its tough status card gets a chance to consume it.
        var facts = new Printed();
        var world = Board(facts);
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.CreateCard(
            "target", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, target, Statuses.Tough);
        var abilities = new Prohibition(source);
        world.Abilities = abilities;

        bool defeated = Damage.Deal(
            world, facts, source, target, 3, "test", "Deal_Damage", []);

        Assert.False(defeated);
        Assert.Equal(0, target.Damage);
        Assert.True(Statuses.Has(world, target, Statuses.Tough));
        Assert.False(abilities.ReplacementVisited);
    }

    [Rule("rr:damage.step.1")]
    [Fact]
    public void ReplacementHandlingReceivesTheActualDamageSource()
    {
        var facts = new Printed();
        var world = Board(facts);
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.CreateCard(
            "target", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var abilities = new Prohibition(null);
        world.Abilities = abilities;

        Damage.Deal(world, facts, source, target, 1, "test", "Deal_Damage", []);

        Assert.Same(source, abilities.ReplacementSource);
    }

    [Rule("rr:damage.3")]
    [Rule("rr:damage.3.1")]
    [Rule("rr:damage.3.2")]
    [Rule("rr:damage.3.3")]
    [Fact]
    public void DealtDamageIsModifiedBeforeTakenDamageIsModified()
    {
        // Increasing the three damage dealt to five also makes five the amount
        // the character would take. Preventing one at the later take boundary
        // leaves four to place without changing the already-fixed five dealt.
        var facts = new Printed();
        var world = Board(facts);
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.CreateCard(
            "target", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var abilities = new DamageModifier();
        world.Abilities = abilities;

        Damage.Deal(world, facts, source, target, 3, "test", "Deal_Damage", []);

        Assert.Equal(5, abilities.AmountThatWouldBeTaken);
        Assert.Equal(4, target.Damage);
    }

    [Rule("rr:attack-player-ability-type.2")]
    [Rule("rr:cannot")]
    [Rule("rr:retaliate-x")]
    [Fact]
    public void AnAttackAbilityKeepsItsActorSeparateFromItsDamageSource()
    {
        // A labelled attack is performed by the hero, while the damage can
        // come from the upgrade carrying the ability. The prohibition reads
        // the latter; retaliate still hits the former.
        var facts = new Printed();
        var world = Board(facts);
        var attacker = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.CreateCard(
            "retaliator", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Abilities = new Prohibition(source);

        Damage.Attack(
            world, facts, attacker, source, target, 3, "test", "Attack", []);

        Assert.Equal(0, target.Damage);
        Assert.Equal(1, attacker.Damage);
    }

    private static World Board(Printed facts)
    {
        var world = new World(facts, 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        return world;
    }

    private sealed class Prohibition(Card? blockedSource) : NoCardAbilities
    {
        public bool ReplacementVisited { get; private set; }

        public Card? ReplacementSource { get; private set; }

        public override bool CanTakeDamage(World world, Card target, Card source) =>
            !ReferenceEquals(source, blockedSource);

        public override long WouldBeDealt(
            World world, Card target, Card source, long amount, List<GameEvent> events)
        {
            ReplacementVisited = true;
            ReplacementSource = source;
            return amount;
        }
    }

    private sealed class DamageModifier : NoCardAbilities
    {
        public long AmountThatWouldBeTaken { get; private set; }

        public override long WouldBeDealt(
            World world, Card target, Card source, long amount, List<GameEvent> events) =>
            amount + 2;

        public override long WouldTake(
            World world, Card target, Card source, long amount, List<GameEvent> events)
        {
            AmountThatWouldBeTaken = amount;
            return amount - 1;
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "source" => CardKind.Upgrade,
            "target" or "retaliator" => CardKind.Minion,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId switch
            {
                "hero" => new Dictionary<string, string> { ["HP"] = "10" },
                "target" => new Dictionary<string, string> { ["HP"] = "5" },
                "retaliator" => new Dictionary<string, string>
                    { ["HP"] = "5", ["Retaliate"] = "1" },
                _ => new Dictionary<string, string>(),
            };

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        public long ConsequentialDamage(string faceId, string attribute) => 0;
    }
}
