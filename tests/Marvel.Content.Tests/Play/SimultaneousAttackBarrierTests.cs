using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousAttackBarrierTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.8")]
    public void NestedDefeatPreventionCompletesBeforeEitherAttackRecipientDeparts()
    {
        // rr:damage.step.6: resolve "when [character] would be defeated" before discarding.
        const string book = """
            {"cards":[
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
                "effect":{"dealDamage":{"cards":{"titled":"Hydra Bomber"},"amount":10}}}]},
              {"card":"01110","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"this"},
                "effect":{"heal":{"card":"this","amount":10}}}]}
            ]}
            """;
        var world = Board(book);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard("01101", area);
        var second = world.CreateCard("01102", area);
        var nested = world.CreateCard("01110", area);
        var events = new List<GameEvent>();
        var result = Resolve(world, [first, second], 10, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(nested.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        Assert.All(new[] { first, second }, card => Assert.Same(area, card.Area));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(0, nested.Damage);
        Assert.Same(area, nested.Area);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Equal(2, result.Recipients[0].Execution.Occurrence.Defeats.Count);
    }

    [Fact]
    public void NonlethalSharesCompleteAndEmptyCohortsAreRejected()
    {
        var world = Board("{\"cards\":[]}");
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard("01101", area);
        var second = world.CreateCard("01102", area);
        Assert.Throws<ArgumentException>(() => Resolve(world, [], 1, []));
        var result = Resolve(world, [first, second], 1, []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.True(result.IsComplete);
        Assert.All(new[] { first, second }, card => { Assert.Equal(1, card.Damage); Assert.Same(area, card.Area); });
        Assert.Empty(result.Execution.Occurrence.Defeats);
    }

    [Fact]
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void SurvivorRetaliatesOnlyAfterTheOtherDefeatChoice()
    {
        // rr:retaliate-x.1: "after this character is attacked" deal damage to the attacker.
        var world = Board("""
            {"cards":[{"card":"01101","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"choose":{"options":[
                {"placeCounters":{"card":"this","counter":"settled","count":1}},
                {"placeCounters":{"card":"this","counter":"settled","count":2}}
              ]}}
            }]}]}
            """);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard("01101", area);
        var survivor = world.CreateCard("01102", area);
        Statuses.Give(world, survivor, Statuses.Tough);
        world.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1, Affects: survivor.ObjectId));
        var hero = world.Seats[0].IdentityCard;
        var events = new List<GameEvent>();
        var result = Resolve(world, [first, survivor], 10, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Same(area, first.Area);
        Assert.Equal(0, hero.Damage);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Same(area, survivor.Area);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:damage.step.8")]
    public void TerminalDefeatStopsLaterDeparturesAndAttackAftermath(bool villainFirst)
    {
        // rr:player-elimination.4: if all players are eliminated, "the game ends".
        // rr:villain-defeat.2: defeating the final stage wins the game.
        var world = Board("{\"cards\":[]}");
        var hero = world.Seats[0].IdentityCard;
        var villain = world.CreateCard("01115", world.AreaOf(DeckType.VillainArea));
        var targets = villainFirst ? new[] { villain, hero } : new[] { hero, villain };
        var events = new List<GameEvent>();
        Resolve(world, targets, 100, events);
        Assert.True(world.IsOver);
        Assert.Equal(villainFirst ? Outcome.PlayersWin : Outcome.PlayersLose, world.Result);
        Assert.Equal(villainFirst ? DeckType.HeroArea : DeckType.VillainArea,
            targets[1].Area.Type);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:damage.step.6")]
    public void OverkillUsesDefeatReceiptRatherThanCurrentIncarnation(bool prevented)
    {
        // rr:overkill.1: excess is dealt only "if this attack defeats an ally".
        // rr:leaves-play.1: a card returning to play is a new copy.
        string trigger = prevented ? "WhenCardWouldBeDefeated" : "WhenCardDefeated";
        string effect = prevented ? "{\"heal\":{\"card\":\"this\",\"amount\":10}}"
            : "{\"seq\":[{\"returnToHand\":\"this\"},{\"putIntoPlay\":{\"card\":\"this\",\"where\":\"yourPlayArea\"}}]}";
        var book = "{\"cards\":[{\"card\":\"01083\",\"abilities\":[{\"trigger\":{\"event\":\"" + trigger
            + "\",\"timing\":\"ForcedInterrupt\",\"subject\":\"this\"},\"effect\":" + effect + "}]}]}";
        var world = Board(book);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "overkill", 1, Affects: hero.ObjectId));
        int incarnation = ally.Incarnation;
        var result = Resolve(world, [ally, other], 5, []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.True(result.IsComplete);
        Assert.Equal(prevented ? incarnation : incarnation + 1, ally.Incarnation);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(prevented ? 0 : 2, hero.Damage);
        Assert.Equal(prevented ? 0 : 2, result.Recipients[0].Excess);
    }

    private static SimultaneousAttackDamageResult Resolve(World world, Card[] targets, long amount, List<GameEvent> events) =>
        SimultaneousAttackDamage.Resolve(world, Cards, new(world.Seats[0].IdentityCard, world.Seats[0].IdentityCard,
            targets, amount, "synthetic", "Attack"), events);

    private static World Board(string book)
    {
        var world = new World(Cards, 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(book));
        return world;
    }
}

