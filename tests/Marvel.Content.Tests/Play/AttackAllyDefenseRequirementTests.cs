using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackAllyDefenseRequirementTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void TheInterruptRequiresOnlyTheAttackedPlayersReadyAlly()
    {
        // "An ally can exhaust to defend against an enemy attack." The synthetic
        // instruction expresses 07051's "an ally you control, if able" restriction.
        var (world, enemy, runner) = Board();
        var own = Ally(world, 0);
        var helper = Ally(world, 1);
        var exhausted = Ally(world, 0);
        exhausted.Exhaust();
        InstallInterrupt(world, enemy, runner);

        var prompt = Attack.DeclareDefender(world, Cards, runner)!;
        Assert.False(prompt.Cancellable);
        Assert.Equal(0, prompt.Player);
        Assert.Equal(own.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        Assert.False(Attack.CanUseDefenseAbility(world, 0));
        Assert.False(Attack.CanDeclareByAbility(world, Cards, helper));
        Assert.False(Attack.CanDeclareByAbility(world, Cards, world.Seats[0].IdentityCard));
        Assert.True(Attack.CanDeclareByAbility(world, Cards, own));
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(world, Cards, runner, Decision.Decline, []));
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(world, Cards, runner,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), []));
        Assert.Equal(digest, world.Digest().Canonical());
        Attack.Defend(world, Cards, runner, Decision.Take(own.ObjectId), []);
        Assert.Equal(own.ObjectId, world.Attack!.Defender);
        Assert.False(own.Ready);
        Assert.True(helper.Ready);
    }

    [Fact]
    public void ExhaustedAlliesDoNotForceDefenseAndAnotherActivationCannotBorrowTheRequirement()
    {
        var (world, enemy, runner) = Board();
        var ally = Ally(world, 0);
        ally.Exhaust();
        InstallInterrupt(world, enemy, runner);
        Assert.True(Attack.DeclareDefender(world, Cards, runner)!.Cancellable);
        Assert.True(Attack.CanUseDefenseAbility(world, 0));
        ally.Refresh();
        Assert.False(Attack.DeclareDefender(world, Cards, runner)!.Cancellable);
        world.Activation = world.Activation! with { Id = 2 };
        Assert.True(Attack.DeclareDefender(world, Cards, runner)!.Cancellable);
    }

    [Fact]
    public void AnotherAttackedPlayerAndAnotherEnemyCannotBorrowTheRequirement()
    {
        var (world, enemy, runner) = Board();
        Ally(world, 0);
        Ally(world, 1);
        InstallInterrupt(world, enemy, runner);
        world.Attack = world.Attack! with { Player = 1, Target = world.Seats[1].IdentityCard.ObjectId };
        Assert.True(Attack.DeclareDefender(world, Cards, runner)!.Cancellable);
        world.Attack = world.Attack! with { Player = 0, Enemy = -1 };
        Assert.True(Attack.CanUseDefenseAbility(world, 0));
    }

    private static void InstallInterrupt(World world, Card enemy, AbilityRunner runner)
    {
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Cards,
            enemy.ObjectId, world.Seats[0].IdentityCard.ObjectId, 0);
        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.ResolveWindow(world, occurrence, pending, Decision.Take(pending.Card));
        Assert.Single(world.Effects.Registered, effect => effect.ActivationId == 1);
    }

    private static Card Ally(World world, int player) => world.CreateCard("01011",
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static (World World, Card Enemy, AbilityRunner Runner) Board()
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","actor":"this"},
              "effect":{"requireAttackedPlayerAllyDefender":"attack"}
            }]}]}
            """));
        var runner = new AbilityRunner(program);
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Abilities = runner;
        world.Attack = new EnemyAttack(enemy.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, Attacking: true, Id: 1);
        return (world, enemy, runner);
    }
}
