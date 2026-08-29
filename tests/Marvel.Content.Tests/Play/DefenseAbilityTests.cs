using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DefenseAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:defend-defense.4")]
    [Rule("rr:defend-defense.4.1")]
    [Rule("rr:defend-defense.4.3")]
    [Rule("rr:defend-defense.4.4")]
    [Rule("rr:defend-defense.5")]
    [Fact]
    public void CrossPlayerDefenseAbilityEstablishesAnUnexhaustedNonBasicDefender()
    {
        // The identity becomes the defender "as soon as the defense-labeled
        // ability begins resolving." It takes over the target player and
        // character, but does not exhaust, use DEF, or become a basic defense.
        var (world, runner, _, second, villain) = Board();
        var source = DefenseCard(world, 1);
        var drawn = world.CreateCard("01087", world.Seats[1].Deck);
        var occurrence = AttackWindow(world, villain);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == source.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Equal(second.ObjectId, world.Attack!.Defender);
        Assert.Equal(second.ObjectId, world.Attack.Target);
        Assert.Equal(1, world.Attack.Player);
        Assert.False(world.Attack.BasicDefense);
        Assert.True(second.Ready);
        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
    }

    [Rule("rr:defend-defense.4.6")]
    [Rule("rr:defend-defense.4.5")]
    [Fact]
    public void FirstDefenseAbilityLocksOtherPlayersOutOfThatAttack()
    {
        // Once one player resolves a defense ability, only that player can
        // resolve further defense abilities during this attack.
        var (world, runner, _, _, villain) = Board();
        var firstPlayers = DefenseCard(world, 0);
        var secondPlayers = DefenseCard(world, 1);
        world.CreateCard("01087", world.Seats[1].Deck);
        var occurrence = AttackWindow(world, villain);

        var taken = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == secondPlayers.ObjectId);
        runner.Resolve(world, occurrence, taken, [], []);

        var next = new Occurrence(
            2, ["WhenBoostCardGiven"], Player: world.Attack!.Player);
        var waiting = runner.Waiting(world, next, WindowKind.Interrupt);
        Assert.DoesNotContain(waiting, ability => ability.Card == firstPlayers.ObjectId);
        Assert.Contains(waiting, ability => ability.Card == secondPlayers.ObjectId);
    }

    [Rule("rr:labeled-ability.3.1")]
    [Rule("rr:defend-defense.4.6")]
    [Fact]
    public void IllegalEnvelopeDefenseIsRefusedBeforeItsCost()
    {
        // Once another player is defending, this player "cannot defend" the
        // attack. A top-level defense label is therefore absent from the offer,
        // and forging it is rejected before its exhaust cost changes the board.
        var (world, _, first, _, villain) = Board();
        var source = DefenseCard(world, 1);
        world.Attack = world.Attack! with
        {
            Defender = first.ObjectId,
            Target = first.ObjectId,
            Player = 0,
        };
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01081", "abilities": [ {
              "trigger": { "event": "WhenBoostCardGiven", "timing": "Interrupt", "subject": "game" },
              "labels": [ "defense" ],
              "cost": { "exhaust": "this" },
              "effect": { "draw": { "player": "you", "count": 1 } }
            } ] } ] }
            """));
        var occurrence = AttackWindow(world, villain);

        Assert.DoesNotContain(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == source.ObjectId);

        var forged = new PendingAbility(source.ObjectId, AbilityType.Interrupt, 1, 0);
        Assert.Throws<RulesNotImplementedException>(
            () => runner.Resolve(world, occurrence, forged, [], []));
        Assert.True(source.Ready);
    }

    [Rule("rr:defend-defense.4.2")]
    [Fact]
    public void TheIdentityIsDefendingBeforeTheDefenseEffectResolves()
    {
        // The effect tests whether the attack is still undefended. Establishing
        // the identity first makes that false, so moving BeginDefenseAbility
        // below the inner effect would draw the card and fail this test.
        var (world, _, _, _, villain) = Board();
        var source = DefenseCard(world, 0);
        world.CreateCard("01087", world.Seats[0].Deck);
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01081", "abilities": [ {
              "trigger": { "event": "WhenBoostCardGiven", "timing": "Interrupt", "subject": "game" },
              "effect": { "defense": { "effect": { "if": {
                "test": { "undefendedAttack": "you" },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "seq": [] }
              } } } }
            } ] } ] }
            """));
        int held = world.Seats[0].Hand.Cards.Count;
        var occurrence = AttackWindow(world, villain);

        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, world.Attack!.Defender);
        Assert.Equal(source.ObjectId, pending.Card);
    }

    [Rule("rr:defend-defense.4.7")]
    [Fact]
    public void DefenseAbilityDoesNotReplaceThePlayersDefendingAlly()
    {
        // A player whose ally is defending may use defense abilities, but their
        // identity does not replace that ally as the defender.
        var (world, runner, _, second, villain) = Board();
        var ally = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var source = DefenseCard(world, 1);
        world.CreateCard("01087", world.Seats[1].Deck);
        world.Attack = world.Attack! with
        {
            Defender = ally.ObjectId,
            Target = ally.ObjectId,
            Player = 1,
        };
        var occurrence = AttackWindow(world, villain);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == source.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Equal(ally.ObjectId, world.Attack!.Defender);
        Assert.Equal(ally.ObjectId, world.Attack.Target);
        Assert.NotEqual(second.ObjectId, world.Attack.Defender);
    }

    [Rule("rr:defend-defense.4.8")]
    [Fact]
    public void DefenseAbilityOutsideAnAttackCreatesNoDefender()
    {
        var (world, runner, _, _, _) = Board();
        var source = DefenseCard(world, 0);
        world.CreateCard("01087", world.Seats[0].Deck);
        world.Attack = null;
        var occurrence = new Occurrence(1, ["WhenBoostCardGiven"], Player: 0);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == source.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Null(world.Attack);
    }

    private static (World World, AbilityRunner Runner, Card First, Card Second, Card Villain)
        Board()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }

        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Attack = new EnemyAttack(
            villain.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01081", "name": "Test Defense", "abilities": [
                { "trigger": { "event": "WhenBoostCardGiven", "timing": "Interrupt", "subject": "game" },
                  "effect": { "defense": { "effect": { "draw": { "player": "you", "count": 1 } } } } }
              ] }
            ] }
            """));
        world.Abilities = runner;
        return (world, runner, world.Seats[0].IdentityCard, world.Seats[1].IdentityCard, villain);
    }

    private static Card DefenseCard(World world, int player) => world.CreateCard(
        "01081",
        world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player));

    private static Occurrence AttackWindow(World world, Card villain) =>
        Occurrence.ForAttack(
            1, ["WhenBoostCardGiven"], world, Cards,
            villain.ObjectId, world.Attack!.Target, world.Attack.Player);
}
