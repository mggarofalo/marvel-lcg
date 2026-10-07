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
            "01129", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.ResolveSpecial(world, claws, 0, finalStep: true);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(minion.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Contains(events.OfType<Marvel.Rules.Events.FieldSet>(), change =>
            change.Card == minion.ObjectId && change.Field == "health" && change.From == 7 && change.To == 3);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
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
        drone.AssignProfile(AuthoredCards.DroneProfile);
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

    [Rule("rr:target.3")]
    [Rule("rr:move.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullHealthSuitDoesNotStartAnAttack(bool finalStep)
    {
        // rr:target.3: "A target is valid ... if any part of that ability can
        // affect that target." With no damage to move, the Suit has none.
        var world = Board();
        var suit = Upgrade(world, "01049");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var events = runner.ResolveSpecial(world, suit, 0, finalStep);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Empty(events);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:move.3")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(1, false, 1)]
    [InlineData(1, true, 1)]
    [InlineData(2, true, 2)]
    public void SuitMovesAvailableDamageAndRetaliationStillHitsTheHero(int damage, bool finalStep, int moved)
    {
        // rr:move.3 moves the same amount between dials. rr:retaliate-x.1:
        // "After this character is attacked, deal X damage to the attacker."
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(damage);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var blasters = world.CreateCard("01153",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host: enemy.ObjectId));
        var suit = Upgrade(world, "01049");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ResolveSpecial(world, suit, 0, finalStep);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Contains("choose where to move damage", prompt.DisplayQuestion);
        Assert.Contains($"Move up to {(finalStep ? 2 : 1)} damage from Black Panther", prompt.Description);
        Assert.Contains($"{damage} damage available", prompt.Description);
        Affordance target = Assert.Single(prompt.Affordances, option => option.AnchorId == enemy.ObjectId);
        Assert.Equal($"Move {moved} damage to Ultron", target.CommitLabel);
        Assert.Contains($"Move {moved} damage from Black Panther", target.Description);
        Assert.Contains("Retaliate 1", target.Description);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(enemy.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(moved, enemy.Damage);
        Assert.Equal(damage - moved + 1, hero.Damage);
        Assert.Contains(events.OfType<Marvel.Rules.Events.FieldSet>(), change =>
            change.Card == hero.ObjectId && change.Verb == "Retaliate");
    }

    [Rule("rr:special")]
    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 2)]
    public void SkippingAnUnresolvableSuitPreservesTheChosenFinalStep(bool suitLast, int clawsDamage)
    {
        // rr:special: a Special resolves only when another ability instructs it.
        // Wakanda Forever's printed text marks the final ability in the chosen
        // sequence; an unresolvable member does not promote an earlier member.
        var world = Board();
        var suit = Upgrade(world, "01049");
        var claws = Upgrade(world, "01047");
        var played = world.CreateCard("01043a", world.Seats[0].Hand);
        var resource = world.CreateCard("01044", world.Seats[0].Hand);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.Act(world, new PendingAbility(played.ObjectId, AbilityType.Action, 0),
            [resource.ObjectId], []);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var ordering = Sequence.Work(world, Cards, runner, events)!;
        int[] order = suitLast ? [claws.ObjectId, suit.ObjectId] : [suit.ObjectId, claws.ObjectId];
        Sequence.Answer(world, Cards, runner, ordering, Decision.Take(played.ObjectId, order, []), events);
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(enemy.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(clawsDamage, enemy.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:stun-stunned")]
    [Rule("rr:tough")]
    [Theory]
    [InlineData(true, 2)]
    [InlineData(true, 0)]
    [InlineData(false, 2)]
    public void AdmittedSuitPreservesStatusReplacement(bool stunned, int damage)
    {
        // rr:stun-stunned replaces the attack; rr:tough prevents damage to its owner.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(damage);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, stunned ? hero : enemy, stunned ? Statuses.Stunned : Statuses.Tough);
        var suit = Upgrade(world, "01049");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ResolveSpecial(world, suit, 0, finalStep: true);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        if (stunned)
            Assert.Contains("Stunned cancels this attack; no damage moves", prompt.Affordances.Single().Description);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(enemy.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(stunned ? damage : 0, hero.Damage);
        Assert.Equal(0, enemy.Damage);
        Assert.False(Statuses.Has(world, stunned ? hero : enemy, stunned ? Statuses.Stunned : Statuses.Tough));
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
