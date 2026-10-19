using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackMaximumTests : CoreCardTestBase
{
    [Theory]
    [InlineData("Attack", true)]
    [InlineData("Attack", false)]
    [InlineData("Activation", false)]
    [Rule("rr:max-maximum.5")]
    public void CopiesShareTheWholeAttackOrActivationAcrossDifferentOccurrences(string period, bool playerAttack)
    {
        // Maximum applies across all copies by title for the specified period.
        // Synthetic windows distinguish an attack from its individual damage occurrences.
        var runner = Runner("01006", "Response",
            """{"placeCounters":{"card":"you","counter":"observed","count":1}}""",
            eventName: "WhenDamageWouldBeDealt", maximum: period);
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        for (int attack = 0; attack < 2; attack++)
        {
            if (playerAttack)
            {
                world.Agenda.Abandon();
                world.Agenda.Now(new PhaseStep(new CharacterAttackOperation(
                    new CharacterAttack(world.Seats[0].IdentityCard.ObjectId, enemy.ObjectId, 0)), 1, 2));
                world.Agenda.Begin(world, Cards);
            }
            else world.Activation = new EnemyActivation(enemy.ObjectId, 0, period == "Attack", Id: attack);
            var occurrence = new Occurrence(100 + attack * 2, ["WhenDamageWouldBeDealt"], Player: 0);
            var offered = runner.Waiting(world, occurrence, WindowKind.Response);
            Assert.Equal(2, offered.Count);
            runner.ResolveWindow(world, occurrence, offered[0], Decision.Take(offered[0].Card));
            Assert.Empty(runner.Waiting(world,
                new Occurrence(101 + attack * 2, ["WhenDamageWouldBeDealt"], Player: 0), WindowKind.Response));
            Assert.Equal(attack + 1, world.Seats[0].IdentityCard.Tokens["c_observed"]);
        }
    }

    [Fact]
    public void AQueuedAttackDoesNotBecomeTheCurrentScopeUntilItsWindowBegins()
    {
        var world = Hero("01001a");
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var attack = new CharacterAttack(world.Seats[0].IdentityCard.ObjectId, enemy.ObjectId, 0);
        world.Agenda.Now(new PhaseStep(new CharacterAttackOperation(attack), 1, 2));
        Assert.Null(world.Agenda.PlayerAttackScope());
        world.Agenda.Begin(world, Cards);
        var outer = world.Agenda.PlayerAttackScope();
        Assert.NotNull(outer);
        world.Agenda.Now(new PhaseStep(new CharacterAttackOperation(attack), 1, 2));
        Assert.Equal(outer, world.Agenda.PlayerAttackScope());
        world.Agenda.Begin(world, Cards);
        Assert.NotEqual(outer, world.Agenda.PlayerAttackScope());
        world.Agenda.Advance();
        world.Agenda.Advance();
        world.Agenda.Advance();
        Assert.Equal(outer, world.Agenda.PlayerAttackScope());
        world.Agenda.Abandon();
        Assert.Null(world.Agenda.PlayerAttackScope());
    }

    [Fact]
    public void ActivationScopeIgnoresQueuedWorkAndTracksNestedInterrupts()
    {
        var world = Hero("01001a");
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var step = new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: enemy.ObjectId, Seat: 0);
        world.Agenda.Now(step);
        var outerOccurrence = world.Agenda.Begin(world, Cards);
        var outer = world.Agenda.EnemyActivationScope(outerOccurrence);
        Assert.NotNull(outer);
        world.Agenda.Now(step);
        Assert.Equal(outer, world.Agenda.EnemyActivationScope(outerOccurrence));
        var innerOccurrence = world.Agenda.Begin(world, Cards);
        Assert.NotEqual(outer, world.Agenda.EnemyActivationScope(innerOccurrence));
        Assert.Equal(outer, world.Agenda.EnemyActivationScope(outerOccurrence));
        Assert.Null(world.Agenda.EnemyActivationScope(new Occurrence(999, [])));
    }
}
