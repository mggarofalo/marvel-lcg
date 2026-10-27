using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ranked target instruction; this does not admit Hulk content.
public sealed class SelectedAllyActivationTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:first-player.1")]
    [Rule("rr:attack-enemy-activation.1.1")]
    [Rule("rr:attack-enemy-activation.1.2")]
    [Rule("rr:attack-enemy-activation.1.3")]
    public void ChosenAllysControllerOwnsTheAttackAndDamageFollowsDefense(int selected, bool defend)
    {
        // "The first player selects among the eligible options."
        // An ability can attack "an ally that player controls".
        // Another defender "becomes the new target"; that player's character
        // also makes its controller "the new target" player.
        var (world, runner, allies, enemy, source) = Fixture();
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, choice.Player);
        Assert.Equal(allies.Select(card => card.ObjectId), choice.Affordances.Select(offer => offer.Id));
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(allies[selected].ObjectId), []);
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(enemy.ObjectId, attack.Subject);
        Assert.Equal(allies[selected].ObjectId, attack.Require<EnemyAttackOperation>().Character);
        Assert.Equal(selected, attack.Seat);
        if (defend)
        {
            for (int opportunity = 0; ; opportunity++)
            {
                Assert.True(opportunity < 8);
                var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
                if (prompt.Asking == Question.Defender && prompt.Player == 0)
                {
                    Sequence.Answer(world, Cards, runner, prompt, Decision.Take(allies[0].ObjectId), []);
                    Assert.Equal(0, world.Attack!.Player);
                    Assert.Equal(allies[0].ObjectId, world.Attack.Target);
                    break;
                }
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
            }
        }
        WreckingRemainingSchemeFixture.Finish(world, runner);
        int recipient = defend ? 0 : selected;
        Assert.Equal(3, allies[recipient].Damage);
        Assert.Equal(0, allies[1 - recipient].Damage);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void DepartedTargetAnswerPreservesStateRandomAndPendingWork()
    {
        var (world, runner, allies, _, _) = Fixture();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        World.MoveToTop(allies[1], world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        string before = Snapshot(world);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner,
            prompt, Decision.Take(allies[1].ObjectId), []));
        Assert.Equal(before, Snapshot(world));
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(allies[0].ObjectId), []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, allies[0].Damage);
        Assert.Equal(0, allies[1].Damage);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static string Snapshot(World world) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage, world.Agenda.Outstanding,
        world.Effects.Registered, world.Attack, world.Activation,
    });

    private static (World, AbilityRunner, Card[], Card, Card) Fixture()
    {
        var runner = Runner("""{"maxBy":{"of":{"query":"minions"},"by":"attack"}}""");
        var world = Deal("spider_man", "captain_marvel");
        world.Agenda.Abandon();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        // Both allies are owned by player zero; their current play areas decide control.
        var allies = Enumerable.Range(0, 2).Select(player => world.CreateCard(player == 0 ? "06011" : "01050",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: 0))).ToArray();
        var enemy = world.CreateCard("01167",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.CreateCard("10028", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        return (world, runner, allies, enemy, source);
    }

    internal static AbilityRunner Runner(string enemies) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"10028","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"maxBy":{"of":{"query":"heroesAndAllies"},"by":"attack"}},
            "effect":{"enemyAttacks":{"enemies":ENEMIES,"against":"chosen"}}}}
        }]}]}
        """.Replace("ENEMIES", enemies, StringComparison.Ordinal)));
}
