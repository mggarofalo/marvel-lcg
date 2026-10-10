using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCorrectionTriggerTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    public void AllyAttacksDoNotTriggerIdentityAttackResponses(bool identityActor)
    {
        // Abilities from allies "are not considered to be performed by that player's identity."
        World world = Deal("she_hulk");
        Card identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01019a");
        identity.Exhaust();
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card punch = world.CreateCard("01024", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var actor = identityActor ? identity : ally;
        var occurrence = new Occurrence(1, [Steps.AttackEnds, Steps.BasicAttackEnds], Player: 0,
            Actor: actor.ObjectId, ActorFacts: new OccurrenceCard(actor.ObjectId,
                identityActor ? CardKind.Hero : CardKind.Ally, 0, 0));
        var offers = runner.Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(identityActor, offers.Any(offer => offer.Card == punch.ObjectId));
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void OneTwoPunchRequiresTheCompletedBasicAttackCondition()
    {
        // "A hero or ally can use their basic attack power to attack an enemy."
        World world = Deal("she_hulk");
        Card identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01019a");
        identity.Exhaust();
        Card punch = world.CreateCard("01024", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(1, [Steps.AttackEnds], Player: 0,
            Actor: identity.ObjectId, ActorFacts: new OccurrenceCard(identity.ObjectId, CardKind.Hero, 0, 0));
        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Response), offer => offer.Card == punch.ObjectId);
        occurrence.Also(Steps.BasicAttackEnds);
        Assert.Contains(runner.Waiting(world, occurrence, WindowKind.Response), offer => offer.Card == punch.ObjectId);
    }
    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void ARealBasicAttackOffersOneTwoPunchAfterItsDamage()
    {
        World world = Deal("she_hulk");
        Card identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01019a");
        Card punch = world.CreateCard("01024", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, villain, []);
        var prompt = Sequence.Work(world, Cards, runner, []);
        Assert.NotNull(prompt);
        Assert.Contains(prompt.Affordances, offer => offer.AnchorId == punch.ObjectId);
        // A basic attack "deals damage equal to the character's ATK value".
        Assert.Equal(3, villain.Damage);
        Assert.False(identity.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    public void ChaseThemDownRequiresTheIdentityToDefeatTheEnemy(bool identityActor)
    {
        // Abilities from allies "are not considered to be performed by that player's identity."
        World world = Deal("she_hulk");
        ClearHand(world);
        world.Seats[0].IdentityCard.TurnTo("01019a");
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card chase = world.CreateCard("01052", world.Seats[0].Hand);
        Card enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        enemy.TakeDamage(DamagePlacement.Health(world, Cards, enemy) - 1);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        if (identityActor) BasicPowerInitiation.BasicAttack(world, Cards, 0, enemy, []);
        else AllyBasicPowers.AllyPower(world, Cards, ally, enemy, BasicPowers.AttackVerb, []);
        var prompt = Sequence.Work(world, Cards, runner, []);
        Assert.Equal(identityActor, prompt?.Affordances.Any(offer => offer.AnchorId == chase.ObjectId) == true);
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    public void SuperhumanStrengthIsNotSpentByAnAllyAttack(bool identityActor)
    {
        // Abilities from allies "are not considered to be performed by that player's identity."
        World world = Deal("she_hulk");
        ClearHand(world);
        world.Seats[0].IdentityCard.TurnTo("01019a");
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card strength = world.CreateCard("01028", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card enemy = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        if (identityActor) BasicPowerInitiation.BasicAttack(world, Cards, 0, enemy, []);
        else AllyBasicPowers.AllyPower(world, Cards, ally, enemy, BasicPowers.AttackVerb, []);
        Agendas.Finish(world, Cards, runner);
        Assert.Equal(identityActor ? DeckType.DiscardPile : DeckType.UpgradesArea, strength.Area.Type);
        Assert.Equal(identityActor, Statuses.Has(world, enemy, Statuses.Stunned));
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void UppercutDoesNotOfferOneTwoPunch()
    {
        // "A hero or ally can use their basic attack power to attack an enemy."
        // One-Two Punch specifies that basic power; Uppercut is an attack ability.
        World world = Deal("she_hulk");
        ClearHand(world);
        Card hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01019a");
        hero.Exhaust();
        Card uppercut = world.CreateCard("01054", world.Seats[0].Hand);
        Card genius = world.CreateCard("01089", world.Seats[0].Hand);
        Card energy = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01024", world.Seats[0].Hand);
        world.CreateCard("01090", world.Seats[0].Hand);
        Card enemy = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == uppercut.ObjectId);
        runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []);
        var target = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(enemy.ObjectId), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(5, enemy.Damage);
        Assert.False(hero.Ready);
    }

    [Fact]
    [Rule("rr:stun-stunned.7")]
    public void AStunnedBasicAttackDoesNotOfferOneTwoPunch()
    {
        World world = Deal("she_hulk");
        world.Seats[0].IdentityCard.TurnTo("01019a");
        world.CreateCard("01024", world.Seats[0].Hand);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card enemy = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, enemy, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        // After the replacement, "that character is not considered to have attacked."
        Assert.Equal(0, enemy.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    private static void ClearHand(World world)
    {
        foreach (Card card in world.Seats[0].Hand.Cards.ToList())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
    }

}
