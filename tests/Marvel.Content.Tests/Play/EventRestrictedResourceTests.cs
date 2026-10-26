using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local printed Suit composition; does not admit the Ms. Marvel pack.</summary>
public sealed class EventRestrictedResourceTests
{
    [Theory]
    [InlineData("05003", true)]
    [InlineData("04006", true)]
    [InlineData("27018", true)]
    [InlineData("01063", false)]
    [InlineData("05009", false)]
    [InlineData("05002", false)]
    public void RestrictionUsesTheActualCostCard(string face, bool eligible)
    {
        var (world, runner, suit) = Board();
        var target = world.CreateCard(face, world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Equal(eligible, CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, target).Any(source => source.Effect == suit.ObjectId));
        Assert.Equal(eligible, CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, abilitySource: target).Any(source => source.Effect == suit.ObjectId));
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner),
            source => source.Effect == suit.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SuitPaysForBigHandsAndExhaustsExactlyOnce()
    {
        // 05003: "Deal 4 damage to an enemy." 05009: "Exhaust ... generate a wild resource for an event."
        var (world, runner, suit) = Board();
        var attack = world.CreateCard("05003", world.Seats[0].Hand);
        var resource = world.CreateCard("05004", world.Seats[0].Hand);
        var villain = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        var action = Assert.Single(runner.Actions(world, 0));
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Equal("G", price.Generators.Single(source => source.Effect == suit.ObjectId).Generates);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        // rr:initiating-abilities.step.5: abort "without paying any costs".
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [suit.ObjectId], [villain.ObjectId])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        runner.ExecuteAction(world,
            new PlayerAction(action, [suit.ObjectId, resource.ObjectId], [villain.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(suit.Ready);
        Assert.Equal(4, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, attack.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Empty(runner.ResourceAbilities(world, 0));
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void EventAbilityCostCanSpendSuitButAnAllyCostCannot()
    {
        // ruling:5b44b2adcc60d4b7 distinguishes Across the Spider-Verse's repeat
        // cost from Make the Call's chosen ally cost. This probes payment only.
        var (world, runner, suit) = Board();
        var ally = world.CreateCard("05002", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => AbilityResourcePayment.Spend(world, runner,
            new(ally, 0, [suit.ObjectId], 1, ""), []));
        Assert.Equal(before, world.Digest().Canonical());
        var source = world.CreateCard("27018", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        // rr:resource-ability.1: "anytime the player who controls the ability is
        // generating resources to pay a cost"; printed repeat payment is three.
        var payment = AbilityResourcePayment.Spend(world, runner,
            new(source, 0, [suit.ObjectId, energy.ObjectId], 3, ""), []);
        Assert.True(payment.IsComplete);
        Assert.False(suit.Ready);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void SuitRequiresItsControllersHeroForm(bool hero, int payer)
    {
        var (world, runner, suit) = Board(hero, players: 2);
        var target = world.CreateCard("05003", world.Seats[payer].Hand);
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[payer], runner, target),
            source => source.Effect == suit.ObjectId);
        if (hero)
            Assert.Contains(runner.ResourceAbilities(world, 0), source => source.Effect == suit.ObjectId);
        else
            Assert.Empty(runner.ResourceAbilities(world, 0));
    }

    private static (World World, AbilityRunner Runner, Card Suit) Board(bool hero = true, int players = 1)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"05009","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game","form":"hero"},
                "resourceRestriction":"event","printedResources":"G",
                "cost":{"exhaust":"this"},"effect":{"generate":"G"}}]},
              {"card":"05003","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game","form":"hero"},
                "effect":{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{"attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":4}}}}}}}]}
            ]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, players, 17);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(hero ? "05001a" : "05001b", seat.Hero);
            for (int index = 0; index < 8; index++) world.CreateCard("01003", seat.Deck);
        }
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var suit = world.CreateCard("05009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        _ = Game.Begin(world, facts, runner);
        return (world, runner, suit);
    }
}
