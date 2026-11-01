using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// rr:damage applies tough status cards (step 2) before abilities that trigger "when
// [character] would take any amount of damage" (step 3). Ruling c8268c2138f6c809:
// "status cards like 'Tough' should have priority over constant effects and interrupts."
public sealed class WouldTakeAfterToughTests : CoreCardTestBase
{
    private const string Crossbones = "04058";
    private const string Armor = "04065";

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 2)]
    [Rule("rr:damage")]
    [Rule("rr:tough.2")]
    public void CrossbonesArmorTakesOnlyTheDamageToughDoesNotPrevent(bool tough, int onArmor)
    {
        // 04065: "When Crossbones would take any amount of damage, place it here instead."
        // Groot's basic attack deals 2.
        var world = GrootBoards.Board(villain: Crossbones);
        var villain = GrootBoards.Villain(world);
        var armor = world.CreateCard(Armor, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        if (tough) Statuses.Give(world, villain, Statuses.Tough);

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(onArmor, armor.Damage);
        Assert.Equal(0, villain.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
    }

    [Fact]
    [Rule("rr:damage")]
    public void DamageThatIgnoresToughStillReachesCrossbonesArmor()
    {
        // 06006: with the Aerial trait, Lightning Strike's damage ignores Tough without
        // discarding it, so Crossbones would take it and the Armor holds it.
        var world = ThorActualFixture.Board("06001a", villain: Crossbones);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var armor = world.CreateCard(Armor, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        Statuses.Give(world, villain, Statuses.Tough);

        AerialLightningStrike(world);

        Assert.Equal((1, 0, true), (armor.Damage, villain.Damage, Statuses.Has(world, villain, Statuses.Tough)));
    }

    [Fact]
    [Rule("rr:damage")]
    public void DamageThatIgnoresToughStillReachesNormanOsbornsReplacement()
    {
        // Lightning Strike's ignore-Tough damage passes Norman's tough status card, so he
        // would take it and removes 1 infamy instead.
        var world = ThorActualFixture.Board("06001a", villain: "02001a,02001b");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 5);
        Statuses.Give(world, villain, Statuses.Tough);

        AerialLightningStrike(world);

        Assert.Equal((4L, 0, true), (environment.Tokens["c_infamy"], villain.Damage, Statuses.Has(world, villain, Statuses.Tough)));
    }

    // Thor plays Lightning Strike for X = 1 with Mjolnir (06009) giving him the Aerial trait.
    private static void AerialLightningStrike(World world)
    {
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = (Marvel.Cards.Run.AbilityRunner)world.Abilities;
        var strike = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == strike.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, mental.ObjectId], [],
            new Dictionary<string, long> { ["X"] = 1 },
            [new(mental.ObjectId, Cost: 0, PaidAs: "B"), new(energy.ObjectId, Cost: 1, PaidAs: "Y")]));
        Sequence.Finish(world, world.Facts, runner, []);
    }

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, 3)]
    [Rule("rr:damage")]
    [Rule("rr:tough.2")]
    public void NormanOsbornRemovesInfamyOnlyForDamageToughDoesNotPrevent(bool tough, long infamy)
    {
        // 02001a: "When Norman Osborn would take any amount of damage, remove that many
        // infamy counters from Criminal Enterprise instead."
        var world = GrootBoards.Board(villain: "02001a,02001b");
        var villain = GrootBoards.Villain(world);
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 5);
        if (tough) Statuses.Give(world, villain, Statuses.Tough);

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(infamy, environment.Tokens["c_infamy"]);
        Assert.Equal(0, villain.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
    }
}
