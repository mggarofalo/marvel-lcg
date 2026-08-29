using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The six steps of an enemy attack — <c>rr:attack-enemy-activation</c>.
/// </summary>
/// <remarks>
/// Synthetic boards, because the recorded game never reaches an attack: its
/// hero never leaves alter-ego form, and <c>rr:activation.1</c> makes a villain
/// facing an alter-ego scheme instead. <c>CardsInWindowsTests</c> runs the same
/// steps on the real Rhino board with real card data; these separate the rules
/// from each other.
/// </remarks>
public sealed class AttackTests
{
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

        var boost = world.AreaOf(
            DeckType.BoostCardsDeck, PlayArea.Villains,
            host: world.TheCardIn(DeckType.VillainArea)!.ObjectId);
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
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("boost_const"),
            Affects: boost.ObjectId));

        Finish(world, printed);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

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
        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);

        Attack.CalculateDamage(world, printed);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "attack",
            Amount: 5,
            Affects: villain.ObjectId));
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
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("defense"),
            Affects: hero.ObjectId));

        long amount = Attack.Amount(
            world, printed,
            new EnemyAttack(
                villain.ObjectId, Player: 0, Target: hero.ObjectId,
                Defender: hero.ObjectId, BasicDefense: true));

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
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "defense",
            Amount: 2,
            Affects: hero.ObjectId));

        long amount = Attack.Amount(
            world, printed,
            new EnemyAttack(
                villain.ObjectId, Player: 0, Target: hero.ObjectId,
                Defender: hero.ObjectId, BasicDefense: true));

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
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, ally.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
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
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        var offered = Assert.Single(
            asked.Affordances, option => option.AnchorId == ally.ObjectId);
        Assert.Equal(0, offered.AnchorPlayer);

        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked,
            Decision.Take(ally.ObjectId), []);

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
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.Exhaust();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
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
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
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
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        // A deck with a card in it. An empty one beside an empty discard pile
        // is `rr:player-deck.4`, and the defeated ally landing in the discard
        // resets the deck and takes the ally straight back into it -- correct,
        // and not what this is about.
        world.CreateCard("ally", world.Seats[0].Deck);
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    [Rule("rr:defend-defense.1")]
    [Fact]
    public void EveryPlayersCharactersAreOfferedAsDefenders()
    {
        // `rr:defend-defense.5` lets somebody else's hero defend, so the offer
        // is every player's. "Only one player at a time can defend" (`.1`) is a
        // limit on the answer rather than on the offer -- one prompt, and each
        // affordance says whose character it is.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(0, asked.Player);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
        Assert.Equal([0, 1], asked.Affordances.Select(option => option.AnchorPlayer));
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void ACardCanRequireOnePlayersAllyToDefendIfAble()
    {
        // "Must defend ... with an ally they control, if able" narrows both
        // halves of the ordinary defense question: the other legal characters
        // are absent and declining is no longer an answer.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var required = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var abilities = new RequiresAlly(0);

        var asked = Sequence.Work(world, printed, abilities, []);

        Assert.NotNull(asked);
        Assert.False(asked.Cancellable);
        Assert.Equal(required.ObjectId, Assert.Single(asked.Affordances).AnchorId);
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(
            world, printed, abilities, Decision.Decline, []));
    }

    [Rule("rr:attack-enemy-activation.4")]
    [Fact]
    public void TheOrdinaryOptionalDefenseReturnsWhenTheRequiredAllyIsNotAble()
    {
        // "If able" ends the card's requirement when its matching ally is not
        // ready. The normal attack rule then permits any legal defender or no
        // defender, rather than making the whole step impossible.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var exhausted = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        exhausted.Exhaust();
        var abilities = new RequiresAlly(0);

        var asked = Sequence.Work(world, printed, abilities, []);

        Assert.NotNull(asked);
        Assert.True(asked.Cancellable);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.5")]
    [Rule("rr:defend-defense.5.1")]
    [Rule("rr:defend-defense.5.2")]
    [Rule("rr:attack-enemy-activation.1.3")]
    [Rule("rr:attack-enemy-activation.2")]
    [Fact]
    public void DefendingForAnotherPlayerMakesYouTheTarget()
    {
        // "If a player other than the attacked player defends the attack with a
        // character they control, that player becomes the new target of that
        // attack." A hero was declared, so the damage is dealt to that hero.
        // **Both** the target character and the target player move.
        var printed = Printed(atk: 5, boost: 0, def: 2);
        var world = Board(printed, players: 2);
        var rescuer = world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!,
            Decision.Take(rescuer.ObjectId), events);

        // **The target *player* moved, not just the character.** The attack is
        // now against seat 1, which is what `rr:defend-defense.5` says outright
        // and what `.5.2` needs -- "any constant or boost abilities that refer
        // to 'you' refer to the defending player".
        //
        // Asserted on the state because nothing reads it yet: `.5.1` splits
        // "when [enemy] attacks you" from "after [enemy] attacks you", and the
        // first is the window that already opened. No ability triggers on the
        // second.
        Assert.Equal(1, world.Attack!.Player);
        Assert.Equal(rescuer.ObjectId, world.Attack.Target);
        Assert.Equal(1, world.Activation!.Player);

        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        // ATK 5 less the rescuer's own DEF 2. The player it was aimed at takes
        // nothing at all.
        Assert.Equal(3, rescuer.Damage);
        Assert.False(rescuer.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:damage.1")]
    [Rule("rr:hit-points.2.1")]
    [Rule("rr:player-elimination")]
    [Fact]
    public void AHeroReducedToZeroIsDefeatedAndTheirPlayerEliminated()
    {
        // "When a character has damage on it equal to or in excess of its hit
        // points, it is defeated", and `rr:hit-points.2.1`: "if a player's hit
        // point dial is reduced to zero, that player is defeated and eliminated
        // from the game."
        //
        // A hero left standing on 0 hit points is the dangerous board:
        // everything else about it is right.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed);
        var identity = world.Seats[0].IdentityCard;

        Finish(world, printed);

        Assert.True(world.Seats[0].Eliminated);

        // `rr:defeat.2` -- an identity is removed from the game, not discarded.
        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);

        // `rr:player-elimination.4` -- "if all players are eliminated, the game
        // ends and the players lose." One player, so this is that.
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:player-elimination.5.1")]
    [Fact]
    public void EliminatingTheAttackedPlayerEndsTheAttackImmediately()
    {
        // A second player keeps the game alive, making the attack cleanup
        // observable instead of having game-over abandon the entire agenda.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed, players: 2);
        var observer = new CombatWindowObserver();

        Finish(world, printed, observer);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.Damaged);
        Assert.Equal(10, Assert.Single(observer.CompletedActivations).DamageDealt);
        Assert.True(observer.SawDamageDealt);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:damage.1")]
    [Rule("rr:hit-points")]
    [Rule("rr:remaining-hit-points.1")]
    [Rule("rr:sustained-damage.1")]
    [Fact]
    public void HealthIsWhatIsLeftAfterDamage()
    {
        // "An identity's or villain's hit point dial represents their remaining
        // hit points." Its sustained damage is maximum minus that remaining
        // value: four damage changes nine remaining hit points to five. The
        // digest records `health` and no damage key, so it exposes the dial.
        var printed = Printed(atk: 1, boost: 0);
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        var alterEgo = world.CreateCard("alter", world.Seats[0].Hero);

        long Health() => StateFields
            .For(alterEgo, printed, 1, inPlay: true, hasHeldPools: true,
                 hasFirstPlayerToken: false, world: world)["health"];

        Assert.Equal(9, Health());
        alterEgo.TakeDamage(4);
        Assert.Equal(5, Health());
    }

    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void TheAttackIsOverWhenItsLastStepIsTaken()
    {
        var printed = Printed(atk: 1, boost: 0);
        var world = Board(printed);
        Finish(world, printed);

        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
    }

    /// <summary>Runs the attack, declining every question.</summary>
    /// <remarks>
    /// Bounded, because the failure this is most likely to meet is a step that
    /// asks the same question forever — and a test that hangs says far less
    /// than one that fails.
    /// </remarks>
    private static void Finish(
        World world, ICardFacts facts, ICardAbilities? abilities = null)
    {
        var events = new List<GameEvent>();
        abilities ??= new NoCardAbilities();
        var asked = Sequence.Work(world, facts, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked after 10 answers");
            Sequence.Answer(world, facts, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, abilities, events);
        }
    }

    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:attack-enemy-activation.7")]
    [Rule("rr:tough.3")]
    [Fact]
    public void AnAttackRecordsWhetherItLandedAndOnlyItsOwnWindowSeesIt()
    {
        // `.step.6.a` lists "after [character] attacks **and damages** ... you"
        // as a trigger of its own, so "it attacked" and "it landed" are two
        // facts -- and by the time those abilities run, the attack is over and
        // the damage is on a dial that had damage on it before. The attack
        // carries what it did.
        //
        // `rr:tough.3` is what pulls the two apart in an ordinary game, and it
        // is the second half of this test: a tough card absorbs the attack, and
        // a character who "is not considered to have taken damage" was not
        // damaged by it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);

        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.Damaged);

        // A second attack, absorbed. The record is the new attack's own -- the
        // first one's `true` must not still be standing when this window opens.
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 2, Number: 2, Index: 0,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        Finish(world, facts);

        Assert.False(world.FinishedAttack!.Damaged);
    }

    [Rule("rr:status-cards.2")]
    [Fact]
    public void StunCancelsBeforeAttackInitiationAbilitiesCanTrigger()
    {
        // "Status card abilities have timing priority over all conflicting
        // triggered abilities." The stun cancels this attack, so an authored
        // "when the villain attacks" interrupt must never observe it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Stunned);
        var observer = new CombatWindowObserver();

        Finish(world, facts, observer);

        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        Assert.False(observer.SawAttackInitiation);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:initiating-abilities.2")]
    [Fact]
    public void AttackInitiationOpensOneInterruptAndOneResponseWindow()
    {
        var facts = Printed(atk: 1, boost: 0);
        var world = Board(facts);
        var observer = new CombatWindowObserver();

        Finish(world, facts, observer);

        Assert.Equal(1, observer.AttackInitiationInterrupts);
        Assert.Equal(1, observer.AttackInitiationResponses);
    }

    [Rule("rr:damage.step.4")]
    [Fact]
    public void DamageThatLandsCreatesTheDamageResponseCondition()
    {
        var facts = Printed(atk: 1, boost: 0);
        var world = Board(facts);
        var observer = new CombatWindowObserver();

        Finish(world, facts, observer);

        Assert.True(observer.SawDamageWouldBeDealt);
        Assert.True(observer.SawDamageDealt);
    }

    [Rule("rr:status-cards.2")]
    [Rule("rr:damage.step.2")]
    [Fact]
    public void ToughPreventionCreatesNoDamageDealtResponse()
    {
        // Tough sits after "would be dealt" effects and before the later
        // damage triggers. The interrupt therefore sees imminent damage, but
        // the response cannot say damage was dealt when Tough prevented it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);
        var observer = new CombatWindowObserver();

        Finish(world, facts, observer);

        Assert.True(observer.SawDamageWouldBeDealt);
        Assert.False(observer.SawDamageDealt);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:attack-enemy-activation")]
    [Fact]
    public void AnAttackInProgressHasNoFinishedAttackToRead()
    {
        // The record belongs to one attack. While the next one is resolving
        // there is no finished attack, rather than the previous one's facts
        // sitting there looking current -- an interrupt on the second attack
        // that asked what the attack did would otherwise be answered about the
        // first.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);
        Assert.NotNull(world.FinishedAttack);

        Attack.Initiate(
            world,
            facts,
            new PhaseStep(
                Steps.Attack, 2, 2, Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                Seat: 0),
            []);

        Assert.Null(world.FinishedAttack);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void AnAttackerThatLeavesPlayMidAttackStopsThere()
    {
        // "If an activating minion **leaves play**, that minion's activation
        // ends immediately and **no further steps of that activation
        // resolve**." An attack is six steps and any of them can be the one
        // that takes the attacker off the table -- an interrupt answering the
        // attack by defeating the thing making it is the ordinary case.
        //
        // Here the attack is begun and the attacker removed between its steps,
        // which is what such an interrupt would do. Nothing after it may
        // happen: no boost card off the encounter deck, and no damage.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        Attack.Initiate(
            world, facts,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        Finish(world, facts);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(deck, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Rule("rr:defend-defense.7")]
    [Rule("rr:defend-defense.7.1")]
    [Rule("rr:attack-enemy-activation.6")]
    [Fact]
    public void AnAttackThatEndsEarlyRemainsDefendedAtItsAfterAttackWindow()
    {
        // "If an effect causes a defended attack to end before fully
        // resolving, the attack is still considered to have been defended."
        // Removing an activating minion after the defender is declared skips
        // its remaining activation steps, but the final attack occurrence must
        // retain the defense for after-defense abilities.
        var facts = Printed(atk: 3, boost: 0, def: 1);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "villain", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var abilities = new CombatWindowObserver();

        var defend = Sequence.Work(world, facts, abilities, events)!;
        Sequence.Answer(
            world, facts, abilities, defend,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), events);
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile));
        Sequence.Finish(world, facts, abilities, events);

        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.IsDefended);
        Assert.True(world.FinishedAttack.BasicDefense);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.VillainArea, villain.Area.Type);
        Assert.False(abilities.SawBoostCardsFlipped);
        Assert.False(abilities.SawDamageWouldBeDealt);
        Assert.True(abilities.SawAttackEnds);
    }

    [Rule("rr:attack-enemy-activation")]
    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void OneAttackCanResolveAgainstBothHeroesWithOneBoostAndOneCompletion()
    {
        // Whirlwind 01130: "also resolve his attack against each other hero."
        // "His attack" is the attack already initiating, not another attack:
        // its one boost card is reused and its activation completes once.
        var facts = Printed(atk: 2, boost: 1);
        var world = Board(facts, players: 2);
        var abilities = new CompletionRecorder();

        Attack.AlsoResolveAgainstEachOtherHero(world);
        Assert.Equal(2, world.Agenda.Count); // one attack root and its sentinel

        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, facts, abilities, events);
        while (asked is not null)
        {
            Sequence.Answer(world, facts, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, abilities, events);
        }

        Assert.Equal([3L, 3L], world.Seats.Select(seat => seat.IdentityCard.Damage));
        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        var result = Assert.Single(abilities.Results);
        Assert.True(result.Made);
        Assert.Equal(6, result.DamageDealt);
    }

    [Rule("rr:defend-defense.2")]
    [Rule("rr:attack-enemy-activation.step.4")]
    [Fact]
    public void AdditionalHeroesResolveInSeatOrderWithTheirOwnDefenderWindows()
    {
        // The engine chooses seat order as its deterministic player order.
        // Each hero gets step 2 and a fresh damage calculation, while the
        // already-flipped boost remains bounded to the one attack.
        var facts = Printed(atk: 2, boost: 1, def: 1);
        var world = Board(facts, players: 3);
        var abilities = new CompletionRecorder();
        Attack.AlsoResolveAgainstEachOtherHero(world);
        var events = new List<GameEvent>();

        var first = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, facts, abilities, first, Decision.Decline, events);

        var second = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(
            world, facts, abilities, second,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);

        var third = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(2, third.Player);
        Sequence.Answer(world, facts, abilities, third, Decision.Decline, events);
        Sequence.Finish(world, facts, abilities, events);

        Assert.Equal([3L, 2L, 3L], world.Seats.Select(seat => seat.IdentityCard.Damage));
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(
            world.Seats.Select(seat => seat.IdentityCard.ObjectId),
            events.OfType<FieldSet>()
                .Where(change => change.Field == "health")
                .Select(change => change.Card));
        Assert.Equal(8, Assert.Single(abilities.Results).DamageDealt);
    }

    /// <summary>A villain, one hero-form identity per seat, one boost card.</summary>
    private static World Board(ICardFacts facts, int players = 1)
    {
        var world = new World(facts, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("hero", world.Seats[seat].Hero);
        }

        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("filler", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: 0, Subject: villain.ObjectId, Seat: 0));
        return world;
    }

    private static Facts Printed(int atk, int boost, int def = 0) => new(atk, boost, def);

    private sealed class CombatWindowObserver : NoCardAbilities
    {
        public bool SawAttackInitiation { get; private set; }
        public bool SawDamageWouldBeDealt { get; private set; }
        public bool SawDamageDealt { get; private set; }
        public bool SawBoostCardsFlipped { get; private set; }
        public bool SawAttackEnds { get; private set; }
        public int AttackInitiationInterrupts { get; private set; }
        public int AttackInitiationResponses { get; private set; }
        public List<EnemyActivation> CompletedActivations { get; } = [];

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            SawAttackInitiation |= occurrence.Is(Steps.AttackInitiated);
            if (occurrence.Is(Steps.AttackInitiated))
            {
                if (window == WindowKind.Interrupt)
                {
                    AttackInitiationInterrupts += 1;
                }
                else
                {
                    AttackInitiationResponses += 1;
                }
            }
            SawDamageWouldBeDealt |= window == WindowKind.Interrupt
                && occurrence.Is(Steps.DamageWouldBeDealt);
            SawDamageDealt |= window == WindowKind.Response
                && occurrence.Is(Steps.DamageDealt);
            SawBoostCardsFlipped |= occurrence.Is("WhenBoostCardsFlipped");
            SawAttackEnds |= occurrence.Is(Steps.AttackEnds);
            return [];
        }

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            CompletedActivations.Add(result);
            return [];
        }
    }

    private sealed class RequiresAlly(int player) : NoCardAbilities
    {
        public override DefenderChoice Defenders(
            World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
        {
            var allies = candidates.Where(card =>
                card.Owner == player && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
            return allies.Count > 0
                ? new DefenderChoice(allies, Required: true)
                : new DefenderChoice(candidates, Required: false);
        }
    }

    private sealed class CompletionRecorder : NoCardAbilities
    {
        public List<EnemyActivation> Results { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Results.Add(result);
            return [];
        }
    }

    private sealed class Facts(int atk, int boost, int def) : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal)
            {
                ["villain"] = new(StringComparer.Ordinal)
                {
                    ["ATK"] = atk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["HP"] = "16",
                },
                ["boost"] = new(StringComparer.Ordinal)
                {
                    ["Boost"] = boost.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                ["amplify"] = new(StringComparer.Ordinal) { ["Amplify"] = "1" },
                ["hero"] = new(StringComparer.Ordinal)
                {
                    ["HP"] = "10",
                    ["DEF"] = def.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                ["ally"] = new(StringComparer.Ordinal) { ["HP"] = "3" },
                ["alter"] = new(StringComparer.Ordinal) { ["HP"] = "9" },
            };

        private readonly Dictionary<string, CardKind> kinds = new(StringComparer.Ordinal)
        {
            ["hero"] = CardKind.Hero,
            ["villain"] = CardKind.EncounterVillain,
            ["boost"] = CardKind.Treachery,
            ["amplify"] = CardKind.EncounterSideScheme,
            ["ally"] = CardKind.Ally,
            ["alter"] = CardKind.AlterEgo,
        };

        public CardKind Kind(string faceId) =>
            kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out var printed)
            && long.TryParse(printed, out long value) ? value : fallback;
    }
}
