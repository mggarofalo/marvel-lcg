using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class AttackDamageIsFixedTests : AttackTestBase
{
    [Rule("rr:attack-enemy-activation.step.4")]
    [Rule("rr:attack-enemy-activation.step.5")]
    [Fact]
    public void DamageIsFixedBeforeTheSeparateStepThatDealsIt()
    {
        // Step 4 calculates the damage. Step 5 then deals "the amount of
        // damage calculated in the previous step". Changing the attacker's
        // ATK between those occurrences therefore does not rewrite the number
        // that step 5 was told to deal.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Attack.CalculateDamage(world, printed);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "attack", Amount: 5, Affects: villain.ObjectId));
        Attack.DealDamage(world, printed, []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:loses")]
    [Rule("rr:defend-defense.2")]
    [Fact]
    public void AHeroThatLosesDefenseReducesNoAttackDamage()
    {
        // DEF remains printed, but the lost power does not function while the
        // hero uses basic defense.
        var printed = Printed(atk: 5, boost: 0, def: 3);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("defense"), Affects: hero.ObjectId));
        long amount = Attack.Amount(world, printed, new EnemyAttack(villain.ObjectId, Player: 0, Target: hero.ObjectId, Defender: hero.ObjectId, BasicDefense: true));
        Assert.Equal(5, amount);
    }

    [Rule("rr:modifiers")]
    [Rule("rr:defend-defense.2")]
    [Fact]
    public void ModifiedDefenseReducesAttackDamageByItsLiveValue()
    {
        // The basic defense reduction is the hero's DEF value, including a
        // continuous modifier currently changing that value.
        var printed = Printed(atk: 5, boost: 0, def: 1);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "defense", Amount: 2, Affects: hero.ObjectId));
        long amount = Attack.Amount(world, printed, new EnemyAttack(villain.ObjectId, Player: 0, Target: hero.ObjectId, Defender: hero.ObjectId, BasicDefense: true));
        Assert.Equal(2, amount);
    }

    [Rule("rr:attack-enemy-activation.step.3.d")]
    [Rule("rr:boost-boost-icon.5")]
    [Fact]
    public void TheBoostCardIsDiscardedAfterItIsApplied()
    {
        // "After applying a boost card to an activation, discard it." A card
        // left on the enemy would be given again next activation and counted
        // twice -- `rr:boost-boost-icon.4` makes them cumulative.
        var printed = Printed(atk: 1, boost: 2);
        var world = Board(printed);
        Finish(world, printed);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(["boost"], discard.Cards.Select(card => card.FaceId));
    }

    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.6")]
    [Fact]
    public void DecliningToDefendLeavesTheAttackUndefended()
    {
        // "If no character is used to defend against an enemy attack, that
        // attack is considered undefended", and it still resolves. The hero
        // stays ready, which is the visible half of not having exhausted to
        // defend.
        var printed = Printed(atk: 2, boost: 0, def: 1);
        var world = Board(printed);
        Finish(world, printed);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(world.FinishedAttack!.IsDefended);
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void AReadyAllyIsOfferedAsADefenderBesideTheHero()
    {
        // "An ally can exhaust to defend against an enemy attack." Both
        // characters can, so both are offered -- `rr:defend-defense.1` limits
        // the *player*, not the number of candidates.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);
        Assert.Equal([world.Seats[0].IdentityCard.ObjectId, ally.ObjectId], asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:ownership-and-control.2.1")]
    [Fact]
    public void AControlledAllyCanDefendWhenAnotherPlayerOwnsIt()
    {
        // An ally put into play under another player's control remains owned by
        // its original player. Defense follows control: the ally is in player
        // zero's play area, so player zero can exhaust it to defend even though
        // it returns to player one's discard pile when it leaves play.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.Equal(1, asked!.Player);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked, Decision.Decline, []);
        asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.NotNull(asked);
        var offered = Assert.Single(asked.Affordances, option => option.AnchorId == ally.ObjectId);
        Assert.Equal(0, offered.AnchorPlayer);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked, Decision.Take(ally.ObjectId), []);
        Assert.Equal(ally.ObjectId, world.Attack!.Target);
        Assert.Equal(0, world.Attack.Player);
        Assert.False(ally.Ready);
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:exhausted.2")]
    [Fact]
    public void AnExhaustedAllyIsNotOfferedAsADefender()
    {
        // "An ally can **exhaust** to defend", and `rr:exhausted.2`: a card
        // that must exhaust to pay for an ability "cannot be used until the
        // card is ready". Offering one would be an option that could not be
        // taken.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.Exhaust();
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.NotNull(asked);
        Assert.Equal([world.Seats[0].IdentityCard.ObjectId], asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:defend-defense.3.1")]
    [Rule("rr:attack-enemy-activation.1.2")]
    [Rule("rr:attack-enemy-activation.3")]
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.2")]
    [Rule("rr:hit-points.3")]
    [Rule("rr:sustained-damage.2")]
    [Fact]
    public void AnAllyDefendingTakesTheDamageAndTheHeroTakesNone()
    {
        // "If a character other than the attacked character defends the
        // attack, that character becomes the new target", and all damage is
        // dealt to that ally. The two damage placed on the ally are its damage
        // tokens and therefore its sustained damage.
        //
        // **And its DEF does not apply.** `rr:defend-defense.2`'s reduction is
        // the hero's basic defense power; an ally exhausting to defend is a
        // different clause with no reduction in it. Printed DEF 3 here against
        // an attack of 2, so a hero defending would take nothing -- the ally
        // takes all of it.
        var printed = Printed(atk: 2, boost: 0, def: 3);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);
        Assert.Equal(2, ally.Damage);
        Assert.False(ally.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:ally.1")]
    [Rule("rr:damage.step.8")]
    [Fact]
    public void AnAllyDefeatedByTheAttackIsDiscarded()
    {
        // "An ally or minion with zero or fewer remaining hit points is
        // defeated and placed in the appropriate discard pile." Discarding is
        // damage step 8. Three hit points against an attack of four, and no DEF
        // to reduce it.
        var printed = Printed(atk: 4, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        // A deck with a card in it. An empty one beside an empty discard pile
        // is `rr:player-deck.4`, and the defeated ally landing in the discard
        // resets the deck and takes the ally straight back into it -- correct,
        // and not what this is about.
        world.CreateCard("ally", world.Seats[0].Deck);
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }


}
