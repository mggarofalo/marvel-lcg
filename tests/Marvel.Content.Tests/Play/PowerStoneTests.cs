using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The production Power Stone (16149) on synthetic boards. "Forced Response: After a hero or
// villain deals 3 or more damage to attached character with a single attack, attach Power
// Stone to the attacking hero or villain."
public sealed class PowerStoneTests : CoreCardTestBase
{
    private const string SheHulk = "01019a,01019b";
    private const string Haymaker = "01087";
    private const string RelentlessAssault = "01053";
    private const string HulkAlly = "01050";
    private const string WeaponsRunner = "01121";

    private static World Board(string identity = SheHulk, int players = 1, string villain = "01094") =>
        ShipCommandBoards.PowerStoneBoard(identity, players, villain);

    private static Card Stone(World world, Card host, string face = ShipCommandBoards.PowerStone) =>
        ShipCommandBoards.Attach(world, host, face);

    private static Card Host(World world, Card stone) => world.Cards[stone.Area.Host];

    [Fact]
    [Rule("rr:attachment.1")]
    public void ThePowerStoneGivesTheVillainOneAttackAndOneScheme()
    {
        var world = Board();
        var villain = GrootBoards.Villain(world);
        Stone(world, villain);

        Assert.Equal(3, Modified(world, villain, "attack"));
        Assert.Equal(2, Modified(world, villain, "scheme"));
    }

    [Fact]
    [Rule("rr:attachment.1.1")]
    public void AHeroBasicAttackOfThreeTakesThePowerStone()
    {
        // She-Hulk's ATK 3 deals 3 damage to Rhino; she then has ATK 4 and THW 2.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);
        var hero = world.Seats[0].IdentityCard;

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        ShipCommandBoards.Choose(world);

        Assert.Equal(hero, Host(world, stone));
        Assert.True(DeckTypes.IsInPlay(stone.Area.Type));
        Assert.Equal(4, Modified(world, hero, "attack"));
        Assert.Equal(2, Modified(world, hero, "thwart"));
        Assert.Equal(2, Modified(world, villain, "attack"));
    }

    [Fact]
    [Rule("rr:attack-player-ability-type")]
    public void ABasicAttackOfTwoLeavesThePowerStone()
    {
        var world = Board(ShipCommandBoards.SpiderMan);
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        ShipCommandBoards.Choose(world);

        Assert.Equal(villain, Host(world, stone));
        Assert.Equal(2, villain.Damage);
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void DamageATougStatusCardPreventsStillTakesThePowerStone()
    {
        // Ruling ea69fd70a070ee2c: Power Stone needs damage dealt, not taken.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);
        Statuses.Give(world, villain, Statuses.Tough);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        ShipCommandBoards.Choose(world);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(world.Seats[0].IdentityCard, Host(world, stone));
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void AnAllysAttackDoesNotTakeThePowerStone()
    {
        // The Hulk ally deals 3 or more, but an ally is neither a hero nor a villain.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);
        var hulk = GrootBoards.InPlay(world, HulkAlly, DeckType.AlliesArea);

        AllyBasicPowers.AllyPower(world, world.Facts, hulk, villain, BasicPowers.AttackVerb, []);
        ShipCommandBoards.Choose(world);

        Assert.True(villain.Damage >= 3);
        Assert.Equal(villain, Host(world, stone));
    }

    [Fact]
    [Rule("rr:you-your.12")]
    public void AnAttackEventIsTheHerosAttack()
    {
        // Haymaker: "Deal 3 damage to an enemy." rr:you-your.12 makes it Spider-Man's attack.
        var world = Board(ShipCommandBoards.SpiderMan);
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);
        var haymaker = GrootBoards.Hand(world, Haymaker);

        GrootBoards.Act(world, haymaker, [GrootBoards.Hand(world, ShipCommandBoards.Energy)], [villain.ObjectId]);
        ShipCommandBoards.Choose(world);

        Assert.Equal(3, villain.Damage);
        Assert.Equal(world.Seats[0].IdentityCard, Host(world, stone));
    }

    [Fact]
    [Rule("rr:overkill.2")]
    public void OverkillDamageToTheVillainCountsTowardTheAttack()
    {
        // Relentless Assault paid with [physical] deals 5 to a 2-hit-point Weapons Runner with
        // overkill; the 3 excess is damage the attack deals Rhino (ruling b6fbf7538dd6b8d2).
        var world = Board(ShipCommandBoards.SpiderMan);
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, villain);
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var assault = GrootBoards.Hand(world, RelentlessAssault);

        GrootBoards.Act(world, assault, [GrootBoards.Hand(world, ShipCommandBoards.Strength)], [runner.ObjectId]);
        ShipCommandBoards.Choose(world);

        Assert.Equal(3, villain.Damage);
        Assert.Equal(world.Seats[0].IdentityCard, Host(world, stone));
    }

    [Theory]
    [InlineData("01094", false)]
    [InlineData("01095", true)]
    [Rule("rr:attack-enemy-activation.step.6")]
    public void TheVillainTakesThePowerStoneBackWithAnAttackOfThree(string villainFace, bool takes)
    {
        // Rhino I (ATK 2) leaves it; Rhino II (ATK 3) takes it back, undefended.
        var world = Board(ShipCommandBoards.SpiderMan, villain: villainFace);
        var villain = GrootBoards.Villain(world);
        var hero = world.Seats[0].IdentityCard;
        var stone = Stone(world, hero);

        KangTemporalFixture.Attack(world, villain, 0);
        ShipCommandBoards.Choose(world);

        Assert.Equal(takes ? 3 : 2, hero.Damage);
        Assert.Equal(takes ? villain : hero, Host(world, stone));
    }

    [Fact]
    [Rule("rr:overkill.2")]
    public void TheVillainsOverkillExcessToTheHeroTakesThePowerStone()
    {
        // Charge (01099) gives Rhino (I) +3 ATK and overkill: 5 damage to Maria Hill
        // (01067, 2 hit points) carries 3 excess to her controller, dealt by the same attack
        // (ruling b6fbf7538dd6b8d2).
        var world = Board(ShipCommandBoards.SpiderMan);
        var villain = GrootBoards.Villain(world);
        var hero = world.Seats[0].IdentityCard;
        var stone = Stone(world, hero);
        Stone(world, villain, "01099");
        var maria = GrootBoards.InPlay(world, "01067", DeckType.AlliesArea);

        KangTemporalFixture.Attack(world, villain, 0);
        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == maria.ObjectId) is { } defense
                ? Decision.Take(defense.Id)
                : !prompt.Cancellable && prompt.Affordances.Count == 1 ? Decision.Take(prompt.Affordances[0].Id) : null);

        Assert.Equal(DeckType.DiscardPile, maria.Area.Type);
        Assert.Equal(3, hero.Damage);
        Assert.Equal(villain, Host(world, stone));
    }

    [Theory]
    [InlineData("01001a,01001b", false)]
    [InlineData(SheHulk, true)]
    [Rule("rr:triggering-condition.2")]
    public void OnlyAnAttackThatDealsDamageAddsTheDealtDamageCondition(string identity, bool dealt)
    {
        // Rhino (II) attacks with ATK 3 and the hero defends: Spider-Man (DEF 3) is dealt
        // nothing, She-Hulk (DEF 2) is dealt 1. Indomitable (01082) answers the attack's
        // completion, whose window names the conditions it shares.
        var world = Board(identity, villain: "01095");
        var hero = world.Seats[0].IdentityCard;
        var indomitable = GrootBoards.InPlay(world, "01082", DeckType.UpgradesArea);
        string? label = null;

        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);
        GrootBoards.Drive(world, prompt =>
        {
            if (prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == hero.ObjectId) is { } defense)
                return Decision.Take(defense.Id);
            if (prompt.Affordances.Any(a => a.AnchorId == indomitable.ObjectId)) label = prompt.Label;
            return !prompt.Cancellable && prompt.Affordances.Count == 1 ? Decision.Take(prompt.Affordances[0].Id) : null;
        });

        Assert.Equal(dealt ? 1 : 0, hero.Damage);
        Assert.NotNull(label);
        Assert.Equal(dealt, label.Contains(AttackDealtDamageTiming.AttackDealtDamage, StringComparison.Ordinal));
    }

    [Fact]
    [Rule("rr:attack-enemy-activation")]
    public void AMinionAttackDoesNotTakeThePowerStone()
    {
        var world = Board(ShipCommandBoards.SpiderMan);
        var hero = world.Seats[0].IdentityCard;
        var stone = Stone(world, hero);
        var sandman = GrootBoards.Engaged(world, "01102");

        KangTemporalFixture.Attack(world, sandman, 0);
        ShipCommandBoards.Choose(world);

        Assert.Equal(3, hero.Damage);
        Assert.Equal(hero, Host(world, stone));
    }

    [Fact]
    [Rule("rr:villain-defeat.3.1")]
    public void DefeatingAnEarlierStageMovesThePowerStoneFromTheNextStage()
    {
        // She-Hulk's ATK 3 defeats Rhino (I) with 11 of 14 damage on him. Rhino (II) is
        // "the same character" and keeps the stone (rr:villain-defeat.3.2), so it moves to her.
        var world = Board();
        var first = GrootBoards.Villain(world);
        world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        first.TakeDamage(11);
        var stone = Stone(world, first);
        var hero = world.Seats[0].IdentityCard;

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, first, []);
        ShipCommandBoards.Choose(world);

        Assert.Equal("01095", GrootBoards.Villain(world).FaceId);
        Assert.Equal(hero, Host(world, stone));
    }

    [Fact]
    [Rule("rr:overkill.2")]
    public void OverkillThatDefeatsAnEarlierStageMovesThePowerStoneFromTheNextStage()
    {
        // Relentless Assault's 3 overkill excess defeats Rhino (I) with 11 of 14 damage on him.
        var world = Board(ShipCommandBoards.SpiderMan);
        var first = GrootBoards.Villain(world);
        world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        first.TakeDamage(11);
        var stone = Stone(world, first);
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var assault = GrootBoards.Hand(world, RelentlessAssault);

        GrootBoards.Act(world, assault, [GrootBoards.Hand(world, ShipCommandBoards.Strength)], [runner.ObjectId]);
        ShipCommandBoards.Choose(world);

        Assert.Equal("01095", GrootBoards.Villain(world).FaceId);
        Assert.Equal(world.Seats[0].IdentityCard, Host(world, stone));
    }
}
