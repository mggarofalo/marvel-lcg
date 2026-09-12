using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class IndirectDamageUnassignedIdentityTests : IndirectDamageTestBase
{
    [Rule("rr:indirect-damage.3.2")]
    [Rule("rr:damage.step.1")]
    [Fact]
    public void UnassignedIdentityCannotSpendBackflipOnIndirectDamage()
    {
        var runner = BackflipRunner();
        var world = Deal();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var backflip = world.CreateCard("01003", world.Seats[0].Hand);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        int amount = Assert.Single(assign.Affordances).Targets!.Min;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(assign.Affordances[0].Id, [..Enumerable.Repeat(ally.ObjectId, amount)], []), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        DamagePlacement.Deal(world, Cards, villain, world.Seats[0].IdentityCard, 1, "later", "Damage", events);
        Assert.Equal(DeckType.HandsArea, backflip.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.3.2")]
    [Rule("rr:damage.step.1")]
    [Fact]
    public void IdentityAssignedIndirectDamageCanUseBackflipAfterAnAllyDefends()
    {
        var runner = BackflipRunner();
        var world = Deal();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var backflip = world.CreateCard("01003", world.Seats[0].Hand);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.MakeIndirect(world);
        var allyDefense = Assert.Single(defend.Affordances, option => option.AnchorId == ally.ObjectId);
        Sequence.Answer(world, Cards, runner, defend, Decision.Take(allyDefense.Id), events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        int amount = Assert.Single(assign.Affordances).Targets!.Min;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(assign.Affordances[0].Id, [..Enumerable.Repeat(world.Seats[0].IdentityCard.ObjectId, amount)], []), events);
        var interrupt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Opportunity, interrupt.Asking);
        var offeredBackflip = Assert.Single(interrupt.Affordances, option => option.Id == backflip.ObjectId);
        Sequence.Answer(world, Cards, runner, interrupt, Decision.Take(offeredBackflip.Id), events);
        var remaining = Sequence.Work(world, Cards, runner, events);
        while (remaining is not null)
        {
            Assert.Equal(Question.Opportunity, remaining.Asking);
            Sequence.Answer(world, Cards, runner, remaining, Decision.Decline, events);
            remaining = Sequence.Work(world, Cards, runner, events);
        }

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
    }

    [Rule("rr:forced.4")]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:damage.step.1")]
    [Fact]
    public void ForcedReplacementRunsBeforeBackflipIsOffered()
    {
        // `rr:forced.4`: "forced interrupts take priority and initiate before
        // non-forced interrupts." The attachment replaces all of this
        // recipient's assigned damage, and `rr:replacement-effect.1` says a
        // replaced effect "is no longer considered imminent" — so Backflip
        // must never be offered or spent on it. Discarding the attachment in
        // the same forced effect leaves the later damage unambiguous: a leaked
        // Backflip prevention would stop it, while the card correctly left in
        // hand cannot.
        var runner = ForcedSoakAndBackflipRunner();
        var world = Deal();
        world.Abilities = runner;
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        var backflip = world.CreateCard("01003", world.Seats[0].Hand);
        world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var soak = world.CreateCard("01098", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: identity.ObjectId));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        int amount = Assert.Single(assign.Affordances).Targets!.Min;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(assign.Affordances[0].Id, [..Enumerable.Repeat(identity.ObjectId, amount)], []), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, identity.Damage);
        Assert.Equal(amount, soak.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, soak.Area.Type);
        Assert.Equal(DeckType.HandsArea, backflip.Area.Type);
        DamagePlacement.Deal(world, Cards, villain, identity, 1, "later", "Damage", events);
        Assert.Equal(1, identity.Damage);
        Assert.Equal(DeckType.HandsArea, backflip.Area.Type);
    }

    [Rule("rr:indirect-damage.4")]
    [Fact]
    public void ASupportIsNotACharacterAndTakesNone()
    {
        // "Characters that cannot take damage cannot be assigned indirect
        // damage." A support has no hit points at all, so it is not among the
        // heroes and allies however close it sits.
        var world = Deal();
        BombScare(world, threat: 2);
        var support = world.CreateCard(NotACharacter, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Reveal(world, AuthoredCards.Explosion);
        Assert.Equal(0, support.Damage);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void ACharacterWithNothingLeftIsNotAssignedAnyAtAll()
    {
        // "A character cannot be assigned more indirect damage than would cause
        // it to be defeated" -- and for a character already at its last hit
        // point of damage, *any* amount would. So it is not among the eligible
        // at all, which is what stops one damage being asked about between two
        // characters when only one can take it.
        //
        // The board is contrived: an ally is put at its full damage without
        // being defeated, which the rules would not leave standing. What is
        // under test is the eligibility, and a legal board cannot show it.
        var world = Deal();
        BombScare(world, threat: 1);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(DamagePlacement.Health(world, Cards, ally));
        Reveal(world, AuthoredCards.Explosion);
        // One eligible character, so nothing is asked and the identity takes it.
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.4.1")]
    [Fact]
    public void IndirectDamageIsIgnoredWhenNoControlledCharacterCanReceiveIt()
    {
        // If no controlled character can be assigned any of the damage, the
        // whole amount is ignored. This contrived board leaves the identity at
        // zero remaining hit points so the eligibility rule can be isolated.
        var world = Deal();
        BombScare(world, threat: 3);
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(DamagePlacement.Health(world, Cards, identity));
        Reveal(world, AuthoredCards.Explosion);
        Assert.Equal(DamagePlacement.Health(world, Cards, identity), identity.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
