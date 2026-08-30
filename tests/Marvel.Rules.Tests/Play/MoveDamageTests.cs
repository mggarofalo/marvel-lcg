using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class MoveDamageTests
{
    [Rule("rr:move.1")]
    [Fact]
    public void DamageCannotMoveBackToItsCurrentPlacement()
    {
        // "When an element moves, it cannot move to its same placement."
        var facts = new Printed();
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        hero.TakeDamage(2);

        long moved = Damage.MoveDamage(
            world, facts, source, hero, hero, 2, "test", "Move_Damage", []);

        Assert.Equal(0, moved);
        Assert.Equal(2, hero.Damage);
    }

    [Rule("rr:move.3.1")]
    [Rule("rr:move.4")]
    [Rule("rr:move.5")]
    [Rule("rr:heal.2")]
    [Fact]
    public void MovingDamageHealsTheOriginAndDealsTheSameAmountToTheDestination()
    {
        // "Effects that move damage off of a character are considered to heal
        // that character." The source loses the two damage the enemy gains.
        var facts = new Printed();
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard(
            "enemy", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        hero.TakeDamage(3);

        long moved = Damage.MoveDamage(
            world, facts, source, hero, enemy, 2, "test", "Move_Damage", []);

        Assert.Equal(2, moved);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(2, enemy.Damage);
    }

    [Rule("rr:move.3.1")]
    [Fact]
    public void AMoveIsBoundedByTheDamageAtItsOrigin()
    {
        var facts = new Printed();
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard(
            "enemy", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        hero.TakeDamage(1);

        long moved = Damage.MoveDamage(
            world, facts, source, hero, enemy, 2, "test", "Move_Damage", []);

        Assert.Equal(1, moved);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(1, enemy.Damage);
    }

    [Rule("rr:move.2")]
    [Rule("rr:cannot")]
    [Fact]
    public void AForbiddenDestinationInvalidatesTheWholeMove()
    {
        var facts = new Printed();
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "source", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard(
            "enemy", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        hero.TakeDamage(2);
        world.Abilities = new CannotTake(source);

        long moved = Damage.MoveDamage(
            world, facts, source, hero, enemy, 2, "test", "Move_Damage", []);

        Assert.Equal(0, moved);
        Assert.Equal(2, hero.Damage);
        Assert.Equal(0, enemy.Damage);
    }

    private static World Board(Printed facts)
    {
        var world = new World(facts, 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        return world;
    }

    private sealed class CannotTake(Card source) : NoCardAbilities
    {
        public override bool CanTakeDamage(World world, Card target, Card actualSource) =>
            !ReferenceEquals(source, actualSource);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "source" => CardKind.Upgrade,
            "enemy" => CardKind.Minion,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) => faceId switch
        {
            "hero" => new Dictionary<string, string> { ["HP"] = "10" },
            "enemy" => new Dictionary<string, string> { ["HP"] = "5" },
            _ => new Dictionary<string, string>(),
        };

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        public long ConsequentialDamage(string faceId, string attribute) => 0;
    }
}
