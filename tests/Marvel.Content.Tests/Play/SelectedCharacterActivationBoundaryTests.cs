using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic instructions isolate activation target boundaries without admitting a pack.
public sealed class SelectedCharacterActivationBoundaryTests : ChoosingCardsTestBase
{
    private static readonly string[] EnemyFaces = ["01167", "01102"];

    [Fact]
    public void OmittedTargetSchedulesTheResolvingPlayersIdentity()
    {
        var (world, runner, source) = Fixture("");
        runner.ExecuteReveal(world, source, 1);
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(1, attack.Seat);
        Assert.Equal(-1, attack.Require<EnemyAttackOperation>().Character);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ScenarioControlledTargetRaisesBeforeAnAttackIsScheduled()
    {
        var (world, runner, source) = Fixture("this");
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source, 1));
        Assert.Contains("no attacked player", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void DepartedSelectedTargetCannotBecomeTheResolvingPlayersIdentityAfterEnemyOrdering(bool reenter)
    {
        // A reentered card is "a new copy of the card". The accepted choice
        // cannot follow that copy or authorize an attack against another identity.
        var runner = SelectedAllyActivationTests.Runner("""{"query":"minions"}""");
        var world = Deal("spider_man", "captain_marvel");
        world.Agenda.Abandon();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        var selected = world.CreateCard("06011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var enemies = EnemyFaces.Select(face => world.CreateCard(face,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))).ToArray();
        var source = world.CreateCard("10028", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(selected.ObjectId), []);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        int incarnation = selected.Incarnation;
        World.MoveToTop(selected, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        if (reenter)
        {
            World.MoveToTop(selected, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
            Assert.NotEqual(incarnation, selected.Incarnation);
        }
        int finalIncarnation = selected.Incarnation;
        string before = Snapshot(world);
        // Engine choice: an order whose accepted explicit target is lost is an
        // unsupported stale continuation. Reject before scheduling or committing it.
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, order,
            Decision.Take(Assert.Single(order.Affordances).Id, enemies.Select(card => card.ObjectId).ToArray(), []), []));
        Assert.Equal(before, Snapshot(world));
        Assert.Equal(finalIncarnation, selected.Incarnation);
        Assert.Equal(reenter ? DeckType.AlliesArea : DeckType.DiscardPile, selected.Area.Type);
        Assert.Equal(0, selected.Damage);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Null(world.Activation);
        Assert.Null(world.Attack);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.Attack);
    }

    private static string Snapshot(World world) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage, world.Agenda.Outstanding,
        world.Agenda.Current, world.Agenda.Occurrence, world.Effects.Registered, world.Attack, world.Activation,
    });

    private static (World, AbilityRunner, Card) Fixture(string target)
    {
        string against = target.Length == 0 ? "" : $",\"against\":\"{target}\"";
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"10028","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"minions"}AGAINST}}
            }]}]}
            """.Replace("AGAINST", against, StringComparison.Ordinal)));
        var world = Deal("spider_man", "captain_marvel");
        world.Agenda.Abandon();
        world.Abilities = runner;
        world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.CreateCard("10028", world.AreaOf(DeckType.RevealingArea));
        return (world, runner, source);
    }
}
