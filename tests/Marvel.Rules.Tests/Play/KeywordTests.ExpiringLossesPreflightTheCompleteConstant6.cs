using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordExpiringLossesPreflightTheCompleteConstantTests : KeywordTestBase
{
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ExpiringLossesPreflightTheCompleteConstantUsesCascade()
    {
        // The two lasting losses end together and restore U1 and U2. U2's
        // ensuing departure would restore U3, whose Permanent attachment is
        // unsupported, so neither registration ends and no card moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard("permanentish", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, third.ObjectId));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: first.ObjectId, Lasts: duration));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: second.ObjectId, Lasts: duration));
        world.Abilities = new ConstantUsesLoss(second.ObjectId, third.ObjectId);
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() => world.Effects.Expire(TimingPoints.EndOfPlayerPhase, events));
        Assert.Equal(2, world.Effects.Registered.Count);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, third.Area.Type);
        Assert.Equal(third.ObjectId, permanent.Area.Host);
        Assert.Empty(events);
    }

    [Rule("rr:loses")]
    [Rule("rr:linked-card-title.4")]
    [Fact]
    public void ACardThatLosesLinkedDoesNotTransferPrintedOwnership()
    {
        // Linked remains printed, but its ownership rule does not function
        // while the keyword is lost.
        var printed = new Printed().With("support", ("Linked", "Parent"));
        var world = Board(printed);
        var support = world.CreateCard("support", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: -1));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("linked"), Affects: support.ObjectId));
        Reveal.EnterPlay(world, printed, support, []);
        Assert.Equal(-1, support.Owner);
    }

    [Rule("rr:uses-x-type")]
    [Fact]
    public void AUsesKeywordThatIsNotACountAndATypeSaysSo()
    {
        var printed = new Printed().With("sideScheme", ("Uses", "3"));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Reveal.Uses(printed.Attributes("sideScheme")));
        Assert.Contains("not a count and a type", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:crisis-icon")]
    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void ACrisisIconStopsTheMainSchemeBeingThwarted()
    {
        // "While **at least one** crisis icon is in play, threat cannot be
        // removed from the main scheme by player cards." A hero's identity and
        // an ally are both player cards, so it takes the main scheme off
        // everybody's list -- unlike `rr:patrol`, which is one player's.
        //
        // A side scheme is untouched: the rule names the main scheme.
        var printed = new Printed().With("sideScheme", ("Crisis", "1"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 2);
        Assert.Equal([side.ObjectId], BasicPowers.Thwartable(world, printed, 0).Select(scheme => scheme.ObjectId));
    }

    [Rule("rr:crisis-icon")]
    [Fact]
    public void WithoutACrisisIconTheMainSchemeIsThwartableAgain()
    {
        var printed = new Printed().With("sideScheme", ("Crisis", "0"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        Assert.Single(BasicPowers.Thwartable(world, printed, 0));
    }

    [Rule("rr:crisis-icon")]
    [Rule("rr:in-play-and-out-of-play.9")]
    [Theory]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.EncounterDiscardPile)]
    [InlineData(DeckType.VillainDeck)]
    [InlineData(DeckType.MainSchemesDeck)]
    [InlineData(DeckType.DealtEncounterCardsDeck)]
    public void ACrisisIconInAnEncounterOutOfPlayAreaStopsNothing(DeckType area)
    {
        // "While at least one crisis icon is **in play**." The encounter deck
        // and discard, unrevealed villain and main-scheme cards, and facedown
        // cards dealt to a player are all out of play. Counting any of them
        // would make the main scheme unthwartable before that card entered.
        var printed = new Printed().With("sideScheme", ("Crisis", "1"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        var playArea = area == DeckType.DealtEncounterCardsDeck ? PlayArea.Of(0) : PlayArea.Villains;
        world.CreateCard("sideScheme", world.AreaOf(area, playArea));
        Assert.Single(BasicPowers.Thwartable(world, printed, 0));
    }

    [Rule("rr:amplify-icon")]
    [Fact]
    public void AmplifyIconsAddToAnAttacksBoostCardToo()
    {
        // "When a boost card is turned faceup **during an enemy activation**" --
        // an attack is an activation as much as a scheme is. ATK 1 plus a boost
        // card worth 1 plus two amplify icons is 4.
        var printed = new Printed().With("hero", ("HP", "10")).With("villain", ("ATK", "1"), ("HP", "20")).With("boost", ("Boost", "1")).With("sideScheme", ("Amplify", "2"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Undefended(world, printed);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:amplify-icon")]
    [Rule("rr:amplify-icon.1")]
    [Theory]
    [InlineData(0, 3)]
    // "Each boost card gains [boost]." Equivalently, "add one additional
    // boost icon [...] for each amplify icon in play", so a boost card worth
    // 1 with two amplify icons is worth 3.
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public void AmplifyIconsAddToEveryBoostCard(int amplify, int expected)
    {
        var printed = new Printed().With("villain", ("SCH", "2")).With("scheme", ("EscalationThreat", "0")).With("boost", ("Boost", "1")).With("sideScheme", ("Amplify", amplify.ToString()));
        var world = Board(printed);
        world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        Assert.Equal(expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:tough.2.1")]
    [Fact]
    public void EveryStatusTypeIsCappedAtOneIncludingTough()
    {
        // "A character cannot have more than one status card of **each type**
        // at a time." Each type -- tough is not exempt, and `rr:status-cards.1.1`
        // extends the cap for steady on the other two only.
        //
        // `rr:tough.2.1` describes a character "with multiple tough status
        // cards", which is a state a card ability can create by saying so
        // rather than one this default permits.
        var printed = new Printed().With("minion", ("HP", "9")).With("steady", ("HP", "9"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotNull(Reveal.Afflict(world, printed, minion, Statuses.Tough, "test", []));
        Assert.Null(Reveal.Afflict(world, printed, minion, Statuses.Tough, "test", []));
        Assert.Equal(1, Statuses.Count(world, minion, Statuses.Tough));
        // And a steady character gets no extra tough card either: the keyword
        // names confused and stunned.
        var steady = world.CreateCard("steady", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotNull(Reveal.Afflict(world, printed, steady, Statuses.Tough, "test", []));
        Assert.Null(Reveal.Afflict(world, printed, steady, Statuses.Tough, "test", []));
    }

    [Rule("rr:vulnerable")]
    [Rule("rr:vulnerable.1")]
    [Rule("rr:vulnerable.2")]
    [Fact]
    public void AVulnerableCharacterIsDiscardedWhenItIsStunned()
    {
        // "**Forced Interrupt**: when this character becomes confused or
        // stunned, discard it", and `.2`: "it is discarded [...] and **is not
        // considered defeated**" -- so nothing reaches the victory display even
        // though the card is worth points.
        var printed = new Printed().With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VictoryDisplay).Cards);
    }

    [Rule("rr:delayed-effect.1")]
    [Rule("rr:damage.step.5")]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:vulnerable.1")]
    [Fact]
    public void DelayedStunDiscardsALethallyDamagedVulnerableWithoutDefeatingIt()
    {
        // The delayed stun exists only after damage lands, so this is not the
        // simultaneous case in vulnerable.2. Once stunned, Vulnerable's
        // "Forced Interrupt" discards the character without defeating it, and
        // leaving play ends the old copy's remaining damage procedure.
        var printed = new Printed().With("minion", ("HP", "3"), ("Vulnerable", "1"), ("Victory", "2"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect, Kind: DelayedEffects.StunTheSubject, Card: villain.ObjectId, Affects: null, Lasts: Duration.NextTime(Steps.DamageDealt)));
        var result = DamageAttacks.Attack(world, printed, villain, minion, 3, "test", "Attack", []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VictoryDisplay).Cards);
        Assert.Equal([minion], result.Characters);
        Assert.Equal(3, result.Dealt);
        Assert.Equal(3, result.Taken);
        Assert.Equal(0, result.Excess);
    }

    [Rule("rr:vulnerable.3")]
    [Fact]
    public void ASteadyVulnerableCharacterSurvivesTheFirstStatusCard()
    {
        // "If a character has both the steady and vulnerable keywords, the
        // vulnerable keyword does not take effect until that character has two
        // confused or two stunned status cards."
        var printed = new Printed().With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:stalwart.1")]
    [Fact]
    public void AStalwartCharacterIsNotDiscardedByVulnerable()
    {
        // Stalwart stops the status card landing at all, so vulnerable never
        // has a condition to fire on. The two keywords together are inert.
        var printed = new Printed().With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Stalwart", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Null(Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }
}
