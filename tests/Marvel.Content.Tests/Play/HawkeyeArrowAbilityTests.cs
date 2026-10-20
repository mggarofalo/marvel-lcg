using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HawkeyeArrowAbilityTests : CoreCardTestBase
{
    [Fact]
    public void CableArrowRemovesMainSchemeThreatWithoutRemovingTheCrisisRestriction()
    {
        var world = Hero("04001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var bow = world.CreateCard("04002", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        main.PlaceTokens("k_threat", 5);
        var crisis = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        crisis.PlaceTokens("k_threat", 4);
        Assert.True(MainScheme.Crisis(world, Cards));
        var arrow = world.CreateCard("04008", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == arrow.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(main.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(4, crisis.Tokens["k_threat"]);
        Assert.True(MainScheme.Crisis(world, Cards));
        Assert.False(bow.Ready);
    }

    [Fact]
    public void MockingbirdPaysAndReturnsDuringTheVillainsInitiationWindow()
    {
        var world = Hero("04001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("04004", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        var step = new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: enemy.ObjectId, Seat: 0);
        Attack.Prepare(world, Cards, step);
        var occurrence = step.OccurrenceOf(world, Cards);
        var offer = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == ally.ObjectId);
        var events = runner.ResolveWindow(world, occurrence, offer,
            new Decision(ally.ObjectId, [], [payment.ObjectId])).Events.ToList();
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.True(world.Attack!.PreventAllDamage);
        world.Attack = world.Attack with { CalculatedDamage = 5 };
        Attack.DealDamage(world, Cards, events);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("04005", "confused", false, 3)]
    [InlineData("04005", "confused", true, 5)]
    [InlineData("04007", "stunned", false, 3)]
    [InlineData("04007", "stunned", true, 5)]
    public void ArrowUsesTheTargetsStatusBeforeApplyingItsOwn(string face, string status, bool present, long damage)
    {
        var world = Hero("04001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var bow = world.CreateCard("04002", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        if (present) Statuses.Give(world, enemy, status);
        var arrow = world.CreateCard(face, world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == arrow.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(enemy.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(damage, enemy.Damage);
        Assert.True(Statuses.Has(world, enemy, status));
        Assert.False(bow.Ready);
        Assert.Equal(DeckType.DiscardPile, arrow.Area.Type);
    }
}
