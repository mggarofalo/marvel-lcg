using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class AttackAdditionalBoostCardsFlipTests : AttackTestBase
{
    [Rule("rr:attack-enemy-activation.step.3.a")]
    [Rule("rr:attack-enemy-activation.step.3.e")]
    [Fact]
    public void AdditionalBoostCardsFlipAndResolveOneAtATimeInDealtOrder()
    {
        // Each card is turned faceup, its boost ability resolves, and it is
        // discarded before the next card is turned faceup. The recorder sees
        // the first card already discarded when the second ability begins.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Abandon();
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, villain, "test", []);
        Attack.GiveAdditionalBoostCard(world, villain, "test", []);
        var abilities = new BoostOrderRecorder();
        Attack.FlipBoostCards(world, printed, abilities, []);
        Assert.Equal(["boost", "filler"], abilities.Faces);
        Assert.Equal([0, 1], abilities.DiscardedBeforeResolution);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
    }

    [Rule("rr:boost-boost-icon.3")]
    [Fact]
    public void BoostAbilityDamageIsNotCountedAsActivationDamage()
    {
        // The boost ability deals two damage during the activation, then the
        // attack deals three. Only the latter is damage dealt by the activation.
        var printed = Printed(atk: 3, boost: 0);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Abandon();
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, villain, "test", []);
        Attack.FlipBoostCards(world, printed, new DamagingBoost(), []);
        Attack.CalculateDamage(world, printed);
        Attack.DealDamage(world, printed, []);
        Assert.Equal(5, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, world.Activation!.DamageDealt);
    }

    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Fact]
    public void BoostAbilityChangesAreVisibleWhenIconsAreApplied()
    {
        // The Boost ability is step 3b. It gives its own card two icons, which
        // step 3c must read afterwards; applying the printed zero first would
        // leave only the villain's two ATK.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Abandon();
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, villain, "test", []);
        Attack.FlipBoostCards(world, printed, new IconChangingBoost(), []);
        Attack.CalculateDamage(world, printed);
        Attack.DealDamage(world, printed, []);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:activation.6")]
    [Rule("rr:attack-enemy-activation.step.3.d")]
    [Fact]
    public void LethalBoostDamageCleansUpWithoutContinuingTheEndedActivation()
    {
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Seats[0].IdentityCard.TakeDamage(9);
        world.Agenda.Abandon();
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, villain, "test", []);
        Attack.FlipBoostCards(world, printed, new DamagingBoost(), []);
        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        Assert.Equal("boost", Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards).FaceId);
    }

    [Rule("rr:indirect-damage.5")]
    [Rule("rr:indirect-damage.5.1")]
    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void IndirectAttackDamageIsAssignedAfterDefenseWithoutAttackingRecipients()
    {
        // The defender decision happens before indirect damage is assigned.
        // The undefended hero remains the attacked character even though most
        // damage is assigned to an ally; that ally's retaliate cannot fire.
        var printed = Printed(atk: 4, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", Amount: 2, Affects: ally.ObjectId));
        var abilities = new CompletionRecorder();
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, printed, abilities, events)!;
        Assert.Equal(Question.Defender, defend.Asking);
        Attack.MakeIndirect(world);
        Sequence.Answer(world, printed, abilities, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, printed, abilities, events)!;
        Assert.Equal(Question.Element, assign.Asking);
        var targets = Assert.Single(assign.Affordances).Targets!;
        Assert.Equal([world.Seats[0].IdentityCard.ObjectId, ally.ObjectId], targets.Legal);
        // "A character cannot be assigned more indirect damage than would
        // cause it to be defeated." The prompt carries those remaining-health
        // capacities so a client does not have to derive them.
        Assert.Equal(new Dictionary<int, int> { [world.Seats[0].IdentityCard.ObjectId] = 10, [ally.ObjectId] = 3, }, targets.MaximumOccurrences);
        Sequence.Answer(world, printed, abilities, assign, Decision.Take(assign.Affordances[0].Id, [ally.ObjectId, ally.ObjectId, ally.ObjectId, world.Seats[0].IdentityCard.ObjectId], []), events);
        Sequence.Finish(world, printed, abilities, events);
        Assert.False(DeckTypes.IsInPlay(ally.Area.Type));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, world.FinishedAttack!.Target);
        Assert.Equal(4, Assert.Single(abilities.Results).DamageDealt);
    }

    [Rule("rr:indirect-damage.3")]
    [Fact]
    public void IndirectAttackDamageIsPlacedOnEveryRecipientSimultaneously()
    {
        // A delayed effect from the identity's damage stuns it. This test's
        // damage guard makes that stun prohibit later damage to the ally. The
        // ally must already have received its assigned point before the delayed
        // effect occurs, because all assigned indirect damage is simultaneous.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var identity = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new StunSensitiveDamage(identity.ObjectId, ally.ObjectId);
        world.Abilities = abilities;
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect, Kind: DelayedEffects.StunTheSubject, Card: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Affects: null, Lasts: Duration.NextTime(Steps.DamageDealt)));
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, printed, abilities, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, printed, abilities, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, printed, abilities, events)!;
        Sequence.Answer(world, printed, abilities, assign, Decision.Take(Assert.Single(assign.Affordances).Id, [identity.ObjectId, ally.ObjectId], []), events);
        Sequence.Finish(world, printed, abilities, events);
        Assert.Equal(1, identity.Damage);
        Assert.Equal(1, ally.Damage);
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void IndirectAttackDamageRejectsAnUnofferedAffordance()
    {
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, printed, abilities, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, printed, abilities, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, printed, abilities, events)!;
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, printed, abilities, assign, Decision.Take(-999, [world.Seats[0].IdentityCard.ObjectId, ally.ObjectId], []), events));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, ally.Damage);
    }

    [Rule("rr:indirect-damage.5")]
    [Rule("rr:player-elimination.5.1")]
    [Fact]
    public void LethalIndirectAttackDamageFinishesAfterEliminatingItsTargetPlayer()
    {
        // With one eligible character the assignment is automatic. Eliminating
        // that player ends the live attack. The assignment cap limits the
        // twenty calculated damage to ten, and the remaining player keeps the
        // game alive.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed, players: 2);
        var abilities = new CompletionRecorder();
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, printed, abilities, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, printed, abilities, defend, Decision.Decline, events);
        Sequence.Finish(world, printed, abilities, events);
        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.True(world.FinishedAttack!.Damaged);
        Assert.Equal(10, Assert.Single(abilities.Results).DamageDealt);
    }

    [Rule("rr:indirect-damage.3")]
    [Rule("rr:player-elimination.5.1")]
    [Fact]
    public void SimultaneouslyLethalAllyFinishesDefeatBeforeIdentityElimination()
    {
        // All thirteen assigned points land together. The ally's own lethal
        // damage must complete its defeat callback before eliminating the
        // identity clears the rest of that player's play area.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var identity = world.Seats[0].IdentityCard;
        var abilities = new DefeatRecorder();
        world.Abilities = abilities;
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, printed, abilities, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, printed, abilities, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, printed, abilities, events)!;
        Sequence.Answer(world, printed, abilities, assign, Decision.Take(Assert.Single(assign.Affordances).Id, [..Enumerable.Repeat(identity.ObjectId, 10), ..Enumerable.Repeat(ally.ObjectId, 3)], []), events);
        Sequence.Finish(world, printed, abilities, events);
        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Contains(ally.ObjectId, abilities.Defeated);
    }

    [Rule("rr:attack-enemy-activation.step.1")]
    [Rule("rr:boost-boost-icon")]
    [Fact]
    public void TheBoostCardIsStillFacedownWhenTheDefenderIsDeclared()
    {
        // "During the activation (and after any defenders are declared if the
        // villain is attacking), each boost card on the enemy is turned face
        // up." A defender is chosen without knowing what the boost card is, so
        // flipping it with the same call that gives it would hand the player
        // information the rules withhold.
        var printed = Printed(atk: 2, boost: 3);
        var world = Board(printed);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);
        var boost = world.AreaOf(DeckType.BoostCardsDeck, PlayArea.Villains, host: world.TheCardIn(DeckType.VillainArea)!.ObjectId);
        Assert.Single(boost.Cards);
        Assert.False(boost.Cards[0].FaceUp);
    }

    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Rule("rr:lasting-effects.5")]
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(3, 5)]
    public void BoostIconsAddToTheAttackValueForThatAttackOnly(int boost, int expected)
    {
        // "Increase the attacking enemy's ATK value by one for each boost icon
        // on the card" -- for this attack. A modifier with a stated duration is
        // a lasting effect, so it goes on the same list as everything else
        // continuously in force and comes off when the attack ends.
        var printed = Printed(atk: 2, boost: boost);
        var world = Board(printed);
        Finish(world, printed);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:loses")]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Fact]
    public void ABoostCardThatLosesItsBoostIconsAddsNothing()
    {
        // The icons remain printed, but a lost characteristic does not
        // function. Only the villain's ATK reaches the damage calculation.
        var printed = Printed(atk: 2, boost: 3);
        var world = Board(printed);
        var boost = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        world.CreateCard("amplify", world.AreaOf(DeckType.SideSchemesArea));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("boost_const"), Affects: boost.ObjectId));
        Finish(world, printed);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }
}
