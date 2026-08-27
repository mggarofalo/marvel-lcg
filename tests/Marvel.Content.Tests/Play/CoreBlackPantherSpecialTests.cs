using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreBlackPantherSpecialTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:special")]
    [Fact]
    public void WakandaForeverQueuesTheChosenPermutationAndMarksOnlyItsLastStepFinal()
    {
        var world = Board();
        var eventCard = world.CreateCard("01043a", world.Seats[0].Hand);
        var resource = world.CreateCard("01044", world.Seats[0].Hand);
        var daggers = Upgrade(world, "01046");
        var claws = Upgrade(world, "01047");
        var runner = AuthoredCards.Runner();

        runner.Act(
            world,
            new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0),
            [resource.ObjectId],
            []);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);

        runner.Chose(
            world, eventCard, 0, choice.Index,
            Decision.Take(eventCard.ObjectId, [claws.ObjectId, daggers.ObjectId], []),
            choice.Tier,
            choice.FinalStep);

        var specials = world.Agenda.Outstanding
            .Where(step => step.What == Steps.ResolveSpecial)
            .ToList();
        Assert.Equal([claws.ObjectId, daggers.ObjectId], specials.Select(step => step.Subject));
        Assert.False(specials[0].FinalStep);
        Assert.True(specials[1].FinalStep);
    }

    [Rule("rr:special")]
    [Fact]
    public void FinalPantherClawsDealsFourFromTheUpgrade()
    {
        var world = Board();
        var claws = Upgrade(world, "01047");
        var minion = world.CreateCard(
            "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.ResolveSpecial(world, claws, 0, finalStep: true);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(minion.ObjectId), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(4, minion.Damage);
    }

    [Rule("rr:cannot")]
    [Rule("rr:special")]
    [Fact]
    public void VibraniumSuitDoesNotOfferKillmongerAsAnAttackTarget()
    {
        var world = Board();
        world.Seats[0].IdentityCard.TakeDamage(2);
        var suit = Upgrade(world, "01049");
        var killmonger = world.CreateCard(
            "01157", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.ResolveSpecial(world, suit, 0, finalStep: true);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = Sequence.Work(world, Cards, runner, [])!;

        Assert.DoesNotContain(
            prompt.Affordances, affordance => affordance.AnchorId == killmonger.ObjectId);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, killmonger.Damage);
    }

    [Rule("rr:simultaneous-resolution")]
    [Rule("rr:cannot")]
    [Fact]
    public void EnergyDaggersDoesNotExposeUltronThreeWhileItsSimultaneousDroneDamageResolves()
    {
        // The villain is evaluated first while the Drone still exists. That
        // preserves the printed simultaneous effect: defeating the last Drone
        // cannot make Ultron a legal recipient midway through that effect.
        var world = Board();
        var oldVillain = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(oldVillain, world.AreaOf(DeckType.VillainDeck));
        var ultron = world.CreateCard("01136", world.AreaOf(DeckType.VillainArea));
        var daggers = Upgrade(world, "01046");
        var drone = world.CreateCard(
            "01087",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: 0));
        drone.TurnFaceDown();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.ResolveSpecial(world, daggers, 0, finalStep: true);
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, daggers, 0, choice.Index,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId),
            AbilityType.Special, finalStep: true);

        Assert.Equal(0, ultron.Damage);
        Assert.False(DeckTypes.IsInPlay(drone.Area.Type));
    }

    private static World Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    private static Card Upgrade(World world, string face) => world.CreateCard(
        face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
}
