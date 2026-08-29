using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The keywords that fire when a card is revealed or enters play.
/// </summary>
/// <remarks>
/// <c>rr:keywords</c>: "a keyword is an attribute that conveys specific rules to
/// its card", and each entry writes its keyword out as the ability it is
/// equivalent to. That is what these hold the engine to — the ability, not the
/// reminder text.
/// </remarks>
public sealed class KeywordTests
{
    [Rule("rr:toughness")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AMinionWithToughnessEntersPlayWithAToughStatusCard()
    {
        // "**Forced Response**: after this character enters play, give it a
        // tough status card." A status is a card with its own object id, not a
        // flag -- the recorded board is unambiguous about that.
        var printed = new Printed().With("minion", ("Toughness", "1"), ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, minion, 0, []);

        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:loses")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void ACharacterThatLosesToughnessEntersPlayWithoutAToughStatusCard()
    {
        // A lost keyword does not function even though it remains printed.
        // Toughness therefore provides no forced response when this minion
        // enters play.
        var printed = new Printed().With("minion", ("Toughness", "1"), ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("toughness"),
            Affects: minion.ObjectId));

        Reveal.Resolve(world, printed, minion, 0, []);

        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:tough.2")]
    [Rule("rr:tough.3")]
    [Fact]
    public void AToughStatusPreventsAllTheDamageAndIsDiscarded()
    {
        // "Prevent all of that damage and discard a tough status card from that
        // character instead", and `.3`: the character "is not considered to
        // have taken damage". Nine damage against three hit points, and it
        // survives untouched.
        var printed = new Printed().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);

        bool defeated = Damage.Deal(world, printed, minion, minion, 9, "test", "test", []);

        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:tough.2.1")]
    [Fact]
    public void OnlyOneToughCardGoesPerInstanceOfDamage()
    {
        // "A character with multiple tough status cards discards only **one**
        // tough status card each time it would take damage." Two cards is two
        // instances of damage prevented, not one.
        var printed = new Printed().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Statuses.Give(world, minion, Statuses.Tough);

        Damage.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.Equal(1, minion.Damage);
    }

    [Rule("rr:incite-x")]
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public void InciteXPlacesThreatOnTheMainScheme(int incite, int expected)
    {
        // "**When Revealed**: place X threat on the main scheme."
        var printed = new Printed().With("treachery", ("Incite", incite.ToString()));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));

        Reveal.Keywords(world, printed, new NoCardAbilities(), card, 0, []);

        Assert.Equal(
            expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:loses")]
    [Rule("rr:incite-x")]
    [Fact]
    public void ACardThatLosesIncitePlacesNoThreatAndProvidesNoAbility()
    {
        // Incite remains printed but no longer functions. Both the reveal
        // effect and the occurrence ledger must agree that there is no
        // keyword-provided ability to resolve.
        var printed = new Printed().With("treachery", ("Incite", "2"));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("incite"),
            Affects: card.ObjectId));

        Reveal.Keywords(world, printed, new NoCardAbilities(), card, 0, []);

        Assert.Equal(
            0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(Reveal.KeywordAbilities(world, printed, card, 0));
    }

    [Rule("rr:incite-x")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    [Fact]
    public void InciteThatCompletesTheMainSchemeEndsTheGame()
    {
        // **Threat placed is threat placed, however it arrived.**
        // `rr:main-scheme-main-scheme-deck.2` completes a scheme the moment its
        // threat reaches its target, and says nothing about what put the threat
        // there -- so an incite card that pushes the scheme over the top ends
        // the game exactly as the villain's own scheming does.
        //
        // The engine placed this threat inline and never looked. A game whose
        // main scheme was one short would carry on past its own ending, and
        // every later round would be a round that should not have been played.
        var printed = new Printed()
            .With("treachery", ("Incite", "1"))
            .With("scheme", ("TargetThreat", "3"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 2);

        Reveal.Keywords(world, printed, new NoCardAbilities(), Treachery(world), 0, []);

        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    [Rule("rr:incite-x")]
    [Fact]
    public void InciteThatDoesNotReachTheTargetLeavesTheGameRunning()
    {
        // The converse, and the reason the check is a comparison rather than
        // "somebody placed threat": one short is not completed.
        var printed = new Printed()
            .With("treachery", ("Incite", "1"))
            .With("scheme", ("TargetThreat", "4"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);

        Reveal.Keywords(world, printed, new NoCardAbilities(), Treachery(world), 0, []);

        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Rule("rr:side-scheme.2")]
    [Fact]
    public void ASideSchemeReachingItsTargetThreatIsNotCompleted()
    {
        // A side scheme prints a target threat value like the main scheme does,
        // and reaching it does **nothing**. `rr:side-scheme.2` runs the other
        // way: a side scheme "remains in play until there is no threat on it",
        // so threat piling up on one is threat piling up, and only taking it
        // all off defeats the card.
        //
        // Worth stating because the two cards look alike to `Threat.Place` and
        // the wrong reading ends the game: a Bomb Scare gathering threat would
        // hand the villain the win.
        var printed = new Printed().With("side", ("TargetThreat", "2"));
        var world = Board(printed);
        var side = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));

        Threat.Place(world, printed, new NoCardAbilities(), side, 3, "test", []);

        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(0, side.Tokens.GetValueOrDefault("is_completed"));
        Assert.Equal(3, side.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:surge")]
    [Fact]
    public void SurgeDealsTheRevealingPlayerAnotherEncounterCard()
    {
        // "**When Revealed**: deal yourself 1 facedown encounter card." Dealt,
        // not revealed -- `rr:surge.2` finishes the original card first, and the
        // queue is what makes that happen without any extra rule.
        var printed = new Printed().With("treachery", ("Surge", "1"));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));

        // Two cards in the deck, so "one card" is a claim rather than an
        // accident of the deck running out.
        world.CreateCard("after", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("next", world.AreaOf(DeckType.EncounterDeck));

        Reveal.Keywords(world, printed, new NoCardAbilities(), card, 0, []);

        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(["next"], queue.Cards.Select(dealt => dealt.FaceId));
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    public void AdditionalSurgeInstancesHaveNoEffect(int printedSurge, int gainedSurge)
    {
        // "If a card gains multiple instances of a keyword, any additional
        // instances have no effect unless that keyword is followed by a
        // number." Surge has no number: whether both instances were gained or
        // one was printed, the card has one When Revealed ability and deals
        // exactly one additional encounter card.
        var printed = new Printed().With(
            "treachery", ("Surge", printedSurge.ToString()));
        var world = Board(printed);
        var card = world.CreateCard(
            "treachery", world.AreaOf(DeckType.RevealingArea));
        for (int instance = 0; instance < gainedSurge; instance++)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: "surge",
                Amount: 1,
                Affects: card.ObjectId));
        }

        world.CreateCard("after", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("next", world.AreaOf(DeckType.EncounterDeck));

        Reveal.Keywords(world, printed, new NoCardAbilities(), card, 0, []);

        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(["next"], queue.Cards.Select(dealt => dealt.FaceId));
        Assert.Equal(
            ["after"],
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(next => next.FaceId));
    }

    [Rule("rr:hinder-x")]
    [Rule("rr:hinder-x.1")]
    [Fact]
    public void HinderXPutsThreatOnTheCardItself()
    {
        // "A card with the hinder X keyword enters play with X threat **on
        // it**" -- on the card, not on the main scheme, which is what separates
        // it from incite.
        var printed = new Printed().With("sideScheme", ("Hinder", "3"));
        var world = Board(printed);
        var scheme = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, scheme, 0, []);

        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Equal(
            0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:loses")]
    [Rule("rr:hinder-x")]
    [Fact]
    public void ACardThatLosesHinderEntersPlayWithoutItsThreat()
    {
        // Hinder remains printed, but the lost keyword does not contribute its
        // entry threat.
        var printed = new Printed().With("sideScheme", ("Hinder", "3"));
        var world = Board(printed);
        var scheme = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("hinder"),
            Affects: scheme.ObjectId));

        Reveal.Resolve(world, printed, scheme, 0, []);

        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:side-scheme")]
    [Rule("rr:side-scheme.1")]
    [Rule("rr:hinder-x.2")]
    [Rule("rr:villain-s-play-area.1")]
    [Fact]
    public void ASideSchemeEntersPlayWithItsStartingThreatAndItsHinder()
    {
        // A side scheme "enters play with an amount of threat on it equal to"
        // its starting threat, and hinder is "in addition to any threat it
        // normally enters play with." The two sources therefore add.
        var printed = new Printed()
            .With("sideScheme", ("Hinder", "2"), ("StartingThreat", "3"));
        var world = Board(printed);
        var scheme = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, scheme, 0, []);

        Assert.Equal(5, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:reveal.5")]
    [Rule("rr:reveal.4")]
    [Theory]
    [InlineData("minion", DeckType.EngagedEnemiesArea)]
    [InlineData("sideScheme", DeckType.SideSchemesArea)]
    [InlineData("obligation", DeckType.ObligationsArea)]
    // A treachery is "placed on the table in front of the player revealing it
    // *(it is not in play)*", which is where it already was.
    [InlineData("treachery", DeckType.RevealingArea)]
    public void EachCardTypeGoesWhereItsOwnClauseSays(string faceId, DeckType where)
    {
        var printed = new Printed();
        var world = Board(printed);
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, card, 0, []);

        Assert.Equal(where, card.Area.Type);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:retaliate-x")]
    [Rule("rr:attack-player-ability-type.5.1")]
    [Fact]
    public void NumberedRetaliateInstancesAddTogether()
    {
        // "If a card gains multiple instances of a keyword [...] followed by a
        // number [...] the numbers for each instance are added together." The
        // printed Retaliate 1 and gained Retaliate 2 deal three damage through
        // the keyword's one Forced Response.
        var printed = new Printed()
            .With("hero", ("ATK", "2"), ("HP", "10"))
            .With("minion", ("HP", "9"), ("Retaliate", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "retaliate",
            Amount: 2,
            Affects: minion.ObjectId));

        BasicPowers.BasicAttack(world, printed, 0, minion, []);

        Agendas.Finish(world, printed);

        Assert.Equal(2, minion.Damage);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:retaliate-x.2")]
    [Fact]
    public void ADefeatedCharacterDoesNotRetaliate()
    {
        // "The character with retaliate X **must be in play after the attack
        // resolves** to deal this damage." An attack that defeats it kills the
        // retaliation with it.
        var printed = new Printed()
            .With("hero", ("ATK", "9"), ("HP", "10"))
            .With("minion", ("HP", "2"), ("Retaliate", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, printed, 0, minion, []);

        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:patrol")]
    [Rule("rr:patrol.1")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void PatrolStopsTheMainSchemeBeingThwartedButNotASideScheme()
    {
        // "The engaged player cannot thwart the **main scheme**." A side scheme
        // is still fair game, which is what separates patrol from guard.
        var printed = new Printed().With("minion", ("Patrol", "1"), ("HP", "3"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 2);

        Assert.Equal(2, BasicPowers.Thwartable(world, printed, 0).Count);

        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.Equal([side.ObjectId],
            BasicPowers.Thwartable(world, printed, 0).Select(scheme => scheme.ObjectId));
    }

    [Rule("rr:patrol")]
    [Fact]
    public void AMinionPatrollingSomebodyElseDoesNotStopYou()
    {
        // Engagement-specific, like guard.
        var printed = new Printed().With("minion", ("Patrol", "1"), ("HP", "3"));
        var world = Board(printed, players: 2);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

        Assert.Single(BasicPowers.Thwartable(world, printed, 0));
        Assert.Empty(BasicPowers.Thwartable(world, printed, 1));
    }

    [Rule("rr:assault")]
    [Rule("rr:assault.1")]
    [Fact]
    public void AssaultMakesAThwartUseAttackInstead()
    {
        // "While a character is making a basic thwart against this scheme, that
        // character uses its **ATK instead of its THW**." ATK 4 against THW 1,
        // so the difference is three threat.
        var printed = new Printed()
            .With("hero", ("ATK", "4"), ("THW", "1"))
            .With("sideScheme", ("Assault", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 9);

        BasicPowers.BasicThwart(world, printed, 0, side, []);
        Agendas.Finish(world, printed);

        Assert.Equal(5, side.Tokens["k_threat"]);
    }

    [Rule("rr:retaliate-x")]
    [Rule("rr:attack-enemy-activation.4.1")]
    [Fact]
    public void AHeroWithRetaliateHitsBackAtAnAttackingEnemy()
    {
        // An undefended attack's "targeted character is considered to have been
        // attacked", so the hero's retaliate response fires. Retaliate is a
        // character rule, not a player one, and damages the attacking enemy.
        var printed = new Printed()
            .With("hero", ("HP", "10"), ("Retaliate", "2"))
            .With("villain", ("ATK", "3"), ("HP", "20"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Attack.Initiate(
            world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Undefended(world, printed);

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, villain.Damage);
    }

    [Rule("rr:assault.2")]
    [Fact]
    public void AnAllyThwartingAnAssaultSchemeTakesTheDamageUnderItsAttack()
    {
        // "If the thwarting character is an ally, it takes the consequential
        // damage listed under its **ATK instead of its THW** after the thwart."
        // One icon under ATK and none under THW, so the ally takes damage
        // thwarting a scheme it would normally walk away from.
        var printed = new Printed()
            .With("ally", ("HP", "4"), ("ATK", "2"), ("THW", "2"), ("AtkIcons", "1"))
            .With("sideScheme", ("Assault", "1"));
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 9);

        BasicPowers.AllyPower(world, printed, ally, side, BasicPowers.ThwartVerb, []);
        Agendas.Finish(world, printed);

        // Thwarted for its ATK, and took the icon under it.
        Assert.Equal(7, side.Tokens["k_threat"]);
        Assert.Equal(1, ally.Damage);
    }

    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void AMinionWithoutVillainousTakesNoCardOffTheEncounterDeck()
    {
        // "**Skip this step.**" Skipping matters beyond the icons: taking a
        // card off the encounter deck moves every later deal, so a minion that
        // wrongly took one desynchronises the rest of the game.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("minion", ("ATK", "1"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Attack.Initiate(
            world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0), []);
        Undefended(world, printed);

        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:loses")]
    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void AMinionThatLosesVillainousTakesNoBoostCard()
    {
        // A card that "loses a characteristic" no longer has its printed
        // Villainous keyword. The activation therefore skips the boost-card
        // step exactly like a minion that never printed the keyword.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("minion", ("ATK", "1"), ("HP", "3"), ("Villainous", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("villainous"),
            Affects: minion.ObjectId));

        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0), []);
        Undefended(world, printed);

        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:scheme-enemy-activation.step.1")]
    [Fact]
    public void AMinionWithoutVillainousSchemesWithoutABoostCard()
    {
        // The same clause on the scheming side, and the same consequence.
        var printed = new Printed()
            .With("minion", ("SCH", "2"), ("HP", "3"))
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "5"));
        var world = Board(printed);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);

        // The villain schemes for its 0 plus a boost card worth 5; the minion
        // schemes for its printed 2 and takes no boost card. **7, not 12** --
        // a minion that wrongly took one would double the boost.
        Assert.Equal(7, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:villainous")]
    [Theory]
    // `rr:attack-enemy-activation.step.1`: "if a villain, **or a minion with the
    // villainous keyword**, is attacking, give it one facedown boost card. (If
    // a minion without the villainous keyword is attacking, skip this step.)"
    [InlineData("villain", 0, true)]
    [InlineData("minion", 0, false)]
    [InlineData("minion", 1, true)]
    public void OnlyAVillainOrAVillainousMinionIsGivenABoostCard(
        string faceId, int villainous, bool boosted)
    {
        var printed = new Printed()
            .With(faceId, ("Villainous", villainous.ToString()));
        var world = Board(printed);
        var enemy = world.CreateCard(faceId, world.AreaOf(DeckType.EngagedEnemiesArea));

        Assert.Equal(
            boosted,
            Marvel.Rules.Timing.Keywords.IsBoosted(world, enemy, printed, 1));
    }

    [Rule("rr:temporary")]
    [Rule("rr:temporary.1")]
    [Fact]
    public void ATemporaryCardIsDiscardedWhenTheRoundEnds()
    {
        // "**Forced Interrupt**: when the round ends, discard this card from
        // play." A card without the keyword beside it stays.
        var printed = new Printed()
            .With("temp", ("Temporary", "1"))
            .With("permanentish", ("HP", "3"));
        var world = Board(printed);
        var temporary = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var staying = world.CreateCard(
            "permanentish", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));

        // A deck with a card in it: an empty one beside an empty discard is
        // `rr:player-deck.4`, and the discarded card would go straight back in.
        world.CreateCard("permanentish", world.Seats[0].Deck);

        PhaseEnd.EndVillainPhase(world, printed, []);

        Assert.Equal(DeckType.DiscardPile, temporary.Area.Type);
        Assert.Equal(DeckType.SupportsArea, staying.Area.Type);
    }

    [Rule("rr:victory-x")]
    [Rule("rr:victory-x.2")]
    [Fact]
    public void ADefeatedCardWorthPointsGoesToTheVictoryDisplay()
    {
        // "A character or side scheme with the victory X keyword is placed in
        // the victory display when it is defeated" -- **instead of** its
        // owner's discard pile, not as well as it.
        var printed = new Printed().With("minion", ("HP", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard(
            "attachment",
            world.AreaOf(
                DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        var events = new List<GameEvent>();

        Agendas.Happening(world);

        Damage.Deal(world, printed, minion, minion, 1, "test", "test", events);

        Assert.Equal(DeckType.VictoryDisplay, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Contains(events.OfType<CardDetached>(), detached =>
            detached.Card == attachment.ObjectId && detached.Host == minion.ObjectId);
        Assert.DoesNotContain(
            world.AreaOf(DeckType.EncounterDiscardPile).Cards,
            card => card.ObjectId == minion.ObjectId);
    }

    [Rule("rr:loses")]
    [Rule("rr:victory-x.2")]
    [Fact]
    public void ADefeatedCardThatLosesVictoryIsDiscarded()
    {
        // Losing Victory removes the replacement destination. The defeated
        // minion therefore goes to the encounter discard pile, not the victory
        // display its printed keyword would otherwise name.
        var printed = new Printed().With("minion", ("HP", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("victory"),
            Affects: minion.ObjectId));
        Agendas.Happening(world);

        Damage.Deal(world, printed, minion, minion, 1, "test", "test", []);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VictoryDisplay).Cards);
    }

    [Rule("rr:quickstrike")]
    [Rule("rr:quickstrike.1")]
    [Fact]
    public void AQuickstrikeMinionAttacksTheHeroItEngages()
    {
        // "**Forced Response (Hero)**: after this minion engages a player, it
        // attacks that player." A minion that would otherwise wait for the next
        // villain phase hits at once.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("minion", ("Quickstrike", "1"), ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Reveal.Quickstrike(world, printed, minion, 0, round: 1);
        Undefended(world, printed);

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:quickstrike")]
    [Fact]
    public void AQuickstrikeMinionDoesNothingToAnAlterEgo()
    {
        // "After a minion with the quickstrike keyword engages a player **whose
        // identity is in hero form**." The *(Hero)* on the forced response is
        // the gate.
        var printed = new Printed()
            .With("minion", ("Quickstrike", "1"), ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal.Quickstrike(world, printed, minion, 0, round: 1);

        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionActivatesWhenOneOfItsOwnIsAlreadyThere()
    {
        // "After a minion with teamwork enters play and engages a player, **if
        // there is at least one other minion that shares the specified trait in
        // play**, the minion that just entered play activates against the
        // player it is engaged with."
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3"))
            .With("friend", ("HP", "3"))
            .Trait("acolyte", "ACOLYTE")
            .Trait("friend", "ACOLYTE");
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Reveal.Teamwork(world, printed, arriving, 0, round: 1);
        Undefended(world, printed);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionAloneDoesNothing()
    {
        // "At least one **other** minion." The arriving minion does not count
        // itself, and a minion of a different trait is not one of its own --
        // which is the clause `rr:teamwork.1`'s shorter restatement drops.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3"))
            .With("stranger", ("HP", "3"))
            .Trait("acolyte", "ACOLYTE")
            .Trait("stranger", "HYDRA");
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var alone = world.CreateCard("acolyte", engaged);

        Reveal.Teamwork(world, printed, alone, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);

        world.CreateCard("stranger", engaged);
        Reveal.Teamwork(world, printed, alone, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:loses")]
    [Rule("rr:teamwork")]
    [Fact]
    public void AMinionThatLosesTeamworkDoesNotActivate()
    {
        // Teamwork remains printed, but the lost keyword supplies no forced
        // response when a matching minion is already in play.
        var printed = new Printed()
            .With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3"))
            .With("friend", ("HP", "3"))
            .Trait("acolyte", "ACOLYTE")
            .Trait("friend", "ACOLYTE");
        var world = Board(printed);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("teamwork"),
            Affects: arriving.ObjectId));

        Reveal.Teamwork(world, printed, arriving, 0, round: 1);

        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:teamwork")]
    [Rule("rr:activation.1")]
    [Fact]
    public void ATeamworkMinionSchemesAgainstAnAlterEgo()
    {
        // The difference from quickstrike, which says outright "a player whose
        // identity is in hero form". Teamwork says the minion **activates**,
        // and `rr:activation.1` reads the form to choose between attacking and
        // scheming -- so an alter-ego is schemed at rather than left alone.
        var printed = new Printed()
            .With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("SCH", "2"), ("HP", "3"))
            .With("friend", ("HP", "3"))
            .Trait("acolyte", "ACOLYTE")
            .Trait("friend", "ACOLYTE");
        var world = Board(printed);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);

        Reveal.Teamwork(world, printed, arriving, 0, round: 1);

        var step = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.Scheme, step.What);
        Assert.Equal(arriving.ObjectId, step.Subject);
    }

    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionCountsFriendsInAnotherPlayersArea()
    {
        // "In play", not "engaged with you". A minion in the other player's
        // area is in play, and this is unreachable at one player -- which is
        // the only board the recording has.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3"))
            .With("friend", ("HP", "3"))
            .Trait("acolyte", "ACOLYTE")
            .Trait("friend", "ACOLYTE");
        var world = Board(printed, players: 2);
        var arriving = world.CreateCard(
            "acolyte", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            "friend", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

        Reveal.Teamwork(world, printed, arriving, 0, round: 1);

        Assert.Equal(arriving.ObjectId, Assert.Single(world.Agenda.Outstanding).Subject);
    }

    [Rule("rr:quickstrike")]
    [Fact]
    public void AMinionWithoutQuickstrikeWaitsForTheVillainPhase()
    {
        // The keyword is the whole of it: an ordinary minion engaging a hero
        // does nothing until step 2 of the next villain phase.
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("minion", ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal.Quickstrike(world, printed, minion, 0, round: 1);

        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:uses-x-type")]
    [Fact]
    public void ACardWithUsesEntersPlayWithItsCounters()
    {
        // "When a card with this keyword enters play, place X all-purpose
        // counters from the token pool on the card. The word following the
        // value establishes and identifies the type." Printed as one field
        // holding both -- `"3,web"` -- so the type travels with the count.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, card, 0, []);

        Assert.Equal(3, card.Tokens["c_web"]);
    }

    [Rule("rr:loses")]
    [Rule("rr:uses-x-type")]
    [Fact]
    public void ACardThatLosesUsesEntersPlayWithoutItsCounters()
    {
        // The composite value remains printed, but the lost Uses keyword no
        // longer places its counters as the card enters play.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: card.ObjectId));

        Reveal.Resolve(world, printed, card, 0, []);

        Assert.Equal(0, card.Tokens.GetValueOrDefault("c_web"));
    }

    [Rule("rr:loses")]
    [Rule("rr:uses-x-type.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegainingUsesWithNoCountersImmediatelyDiscardsTheCard(bool expires)
    {
        // Uses is a constant ability: as soon as the loss ends, its live
        // zero-counter condition discards the card. This is true whether a
        // duration expires normally or the registration ends early.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        var loss = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: card.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));
        Reveal.Resolve(world, printed, card, 0, []);
        var events = new List<GameEvent>();

        if (expires)
        {
            world.Effects.Expire(TimingPoints.EndOfPlayerPhase, events);
        }
        else
        {
            loss.Dispose();
        }

        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        if (expires)
        {
            Assert.Contains(events.OfType<CardsMoved>(), moved =>
                moved.Cards.Any(landing => landing.Card == card.ObjectId));
        }
    }

    [Rule("rr:uses-x-type.1")]
    [Rule("rr:permanent.5")]
    [Fact]
    public void RestoringUsesPreflightsEveryDiscardBeforeEndingAnyLoss()
    {
        // Both Uses constants would become active at the same timing point.
        // A Permanent attachment makes the second discard unsupported, so the
        // complete transition refuses before either card or either loss moves.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.RevealingArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.RevealingArea));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: first.ObjectId,
            Lasts: duration));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: second.ObjectId,
            Lasts: duration));
        Reveal.Resolve(world, printed, first, 0, []);
        Reveal.Resolve(world, printed, second, 0, []);
        var attachment = world.CreateCard(
            "permanentish",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));

        Assert.Throws<RulesNotImplementedException>(() =>
            world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []));

        Assert.Equal(2, world.Effects.Registered.Count);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
    }

    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void RestoringUsesRefusesAHostingCycleBeforeEndingEitherLoss()
    {
        // A cyclic hosted component has no root to discard first. It is
        // refused explicitly rather than pruning every candidate as somebody
        // else's child and leaving active Uses cards at zero counters.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        World.MoveToTop(first, world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        World.MoveToTop(second, world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, first.ObjectId));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: first.ObjectId,
            Lasts: duration));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: second.ObjectId,
            Lasts: duration));

        var thrown = Assert.Throws<RulesNotImplementedException>(() =>
            world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []));

        Assert.Contains("hosting cycle", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, world.Effects.Registered.Count);
        Assert.Equal(second.ObjectId, first.Area.Host);
        Assert.Equal(first.ObjectId, second.Area.Host);
    }

    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void RestoringAnotherCardsUsesIgnoresAFacedownDroneUnderlyingUsesCard()
    {
        // A facedown encounter card has no active printed attributes. Ending a
        // different card's Uses loss must not expose and apply the player card
        // text hidden beneath a Drone.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("playerUses", ("Uses", "3,charge"));
        var world = Board(printed);
        world.CreateCard("playerUses", world.Seats[0].Deck);
        var drone = Assert.IsType<Card>(
            FacedownDrones.EngageTop(world, 0, "test", "Drone", []));
        var card = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: card.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));
        Reveal.Resolve(world, printed, card, 0, []);

        world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []);

        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, drone.Area.Type);
        Assert.True(FacedownDrones.Is(drone));
    }

    [Rule("rr:ability.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void UsesRestoredWhenAConstantSourceLeavesImmediatelyDiscardsTheCard()
    {
        // A constant exists only while its source is in play. Its departure is
        // preflighted as one transition, then the newly active zero-counter
        // Uses constant discards the affected card.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var card = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, card.ObjectId);
        Reveal.Resolve(world, printed, card, 0, []);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AHostedConstantCannotDiscardItsDepartingHostTwice()
    {
        // The attachment leaves immediately before its host. Ending its
        // constant restores the host's zero-counter Uses, but the host is
        // already part of the same departure and moves exactly once.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var host = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var source = world.CreateCard(
            "temp", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, host.ObjectId);
        var events = new List<GameEvent>();

        Discard.Card(world, host, "test", events);

        Assert.Equal(DeckType.EncounterDiscardPile, host.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(landing => landing.Card == host.ObjectId));
        Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(landing => landing.Card == source.ObjectId));
    }

    [Rule("rr:ability.5")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void DiscardingAHostedConstantCanCascadeToItsAcyclicHost()
    {
        // S is already in the departure plan when ending its constant restores
        // its host H. Walking H's hosted tree reaches S again by deduplication,
        // not by an ancestor cycle; both cards still move exactly once.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var host = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var source = world.CreateCard(
            "temp", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, host.ObjectId);
        var events = new List<GameEvent>();

        Discard.Card(world, source, "test", events);

        Assert.Equal(DeckType.EncounterDiscardPile, host.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(landing => landing.Card == host.ObjectId));
        Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(landing => landing.Card == source.ObjectId));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ADependentConstantEndingRestoresUsesAndDiscardsTheCard()
    {
        // S grants B a trait, and B conditionally makes U lose Uses while it
        // has that trait. S authors no Uses loss itself, but its departure
        // still disables B's condition and restores U at zero counters.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard(
            "bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new DependentConstantUsesLoss(
            source.ObjectId, bridge.ObjectId, uses.ObjectId);
        Assert.True(Characteristics.IsLost(world, uses, "uses"));

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AConstantDeparturePreflightsActiveCrossRootPermanent()
    {
        // S restores U1 and U2 together. U2 grants Permanent to A on U1 while
        // both roots are still in play immediately before the simultaneous
        // departure, so the unsupported host loss is refused atomically.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard(
            "attachment", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, first.ObjectId));
        world.Abilities = new UsesLossWithDependentPermanent(
            source.ObjectId, first.ObjectId, second.ObjectId, attachment.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() =>
            Discard.Card(world, source, "test", []));

        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(first.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ADepartingCardCannotActivateANewConditionalConstantMidCommit()
    {
        // S restores U1 and U2. U2's Permanent grant is dormant until U1 is
        // absent, but U2 is itself already departing and therefore cannot
        // activate a new constant between the two preflighted moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard(
            "attachment", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new UsesLossWithDormantPermanent(
            source.ObjectId, first.ObjectId, second.ObjectId, attachment.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AUsesCascadeDoesNotActivateAPostCascadePermanentEarly()
    {
        // S restores U1 and U2. Surviving B grants Permanent to A only after
        // U1 is absent, which is after the simultaneous Uses roots qualified;
        // the sequential event writes cannot activate it between their moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var surviving = world.CreateCard(
            "bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard(
            "attachment", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new UsesLossWithDormantPermanent(
            source.ObjectId, first.ObjectId, second.ObjectId,
            surviving.ObjectId, attachment.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, surviving.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ASourceDepartureCanActivateASurvivingReplacementUsesLoss()
    {
        // S's loss ending appears to restore U during preflight, but surviving
        // B supplies the same loss as soon as S is actually absent. U still
        // lacks Uses at the commit boundary and therefore is not discarded.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var surviving = world.CreateCard(
            "bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new UsesLossWithSurvivingReplacement(
            source.ObjectId, surviving.ObjectId, uses.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, surviving.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, uses.Area.Type);
        Assert.True(Characteristics.IsLost(world, uses, "uses"));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ARejectedUsesRootKeepsItsDependentRootFromDeparting()
    {
        // S appears to restore U2, and suppressing U2 during preflight appears
        // to restore U3. Once S is absent, U2's own replacement loss keeps U2
        // in play, so its loss on U3 also remains and neither root departs.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard(
            "permanentish", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new ReplacementUsesLossCascade(
            source.ObjectId, second.ObjectId, third.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, third.Area.Type);
        Assert.Equal(second.ObjectId, permanent.Area.Host);
        Assert.True(Characteristics.IsLost(world, second, "uses"));
        Assert.True(Characteristics.IsLost(world, third, "uses"));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AQualifiedUsesDepartureStaysLatchedDuringProjection()
    {
        // S makes U lose Uses, while U grants B the trait that disables B's
        // replacement loss. With S projected absent U qualifies; projecting U
        // absent next removes the trait, but that consequence cannot revoke a
        // departure whose zero-counter condition already qualified.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard(
            "bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new LatchedUsesDeparture(
            source.ObjectId, uses.ObjectId, bridge.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
        Assert.Equal(DeckType.SupportsArea, bridge.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ProjectedAbsenceCanDiscoverAUsesRestoration()
    {
        // B makes U lose Uses only while S is in play. S authors no constant
        // itself, so suppressing emitted source effects cannot discover U;
        // projecting S absent must include every predeparture lost Uses card.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard(
            "bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new PresenceDependentUsesLoss(
            source.ObjectId, bridge.ObjectId, uses.ObjectId);

        Discard.Card(world, source, "test", []);

        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, bridge.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ProjectedAttachmentLegalityKeepsSelfConstantsActive()
    {
        // S restores H's zero-counter Uses. A is hosted by H and its own
        // constant makes A Permanent while it remains in play, so preflight
        // must refuse before projecting A away disables that constant.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var host = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard(
            "attachment", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new UsesLossWithSelfPermanentAttachment(
            source.ObjectId, host.ObjectId, attachment.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() =>
            Discard.Card(world, source, "test", []));

        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, host.Area.Type);
        Assert.Equal(host.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AConstantDeparturePreflightsTheCompleteUsesCascade()
    {
        // S restores U1 and U2; discarding U2 would restore U3, whose
        // Permanent attachment cannot yet be resolved. The complete cascade
        // is refused before S, U1, or U2 moves or emits an event.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard(
            "temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard(
            "permanentish", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, third.ObjectId));
        world.Abilities = new ConstantUsesLoss(
            (source.ObjectId, first.ObjectId),
            (source.ObjectId, second.ObjectId),
            (second.ObjectId, third.ObjectId));
        var events = new List<GameEvent>();

        Assert.Throws<RulesNotImplementedException>(() =>
            Discard.Card(world, source, "test", events));

        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, third.Area.Type);
        Assert.Equal(third.ObjectId, permanent.Area.Host);
        Assert.Empty(events);
    }

    [Rule("rr:lasting-effects.1")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ExpiringLossesPreflightTheCompleteConstantUsesCascade()
    {
        // The two lasting losses end together and restore U1 and U2. U2's
        // ensuing departure would restore U3, whose Permanent attachment is
        // unsupported, so neither registration ends and no card moves.
        var printed = new Printed()
            .With("sideScheme", ("Uses", "3,web"))
            .With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard(
            "sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard(
            "permanentish", world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Villains, third.ObjectId));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: first.ObjectId,
            Lasts: duration));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("uses"),
            Affects: second.ObjectId,
            Lasts: duration));
        world.Abilities = new ConstantUsesLoss(second.ObjectId, third.ObjectId);
        var events = new List<GameEvent>();

        Assert.Throws<RulesNotImplementedException>(() =>
            world.Effects.Expire(TimingPoints.EndOfPlayerPhase, events));

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
        var support = world.CreateCard(
            "support",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: -1));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("linked"),
            Affects: support.ObjectId));

        Reveal.EnterPlay(world, printed, support, []);

        Assert.Equal(-1, support.Owner);
    }

    [Rule("rr:uses-x-type")]
    [Fact]
    public void AUsesKeywordThatIsNotACountAndATypeSaysSo()
    {
        var printed = new Printed().With("sideScheme", ("Uses", "3"));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Reveal.Uses(printed.Attributes("sideScheme")));

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

        Assert.Equal([side.ObjectId],
            BasicPowers.Thwartable(world, printed, 0).Select(scheme => scheme.ObjectId));
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
        var playArea = area == DeckType.DealtEncounterCardsDeck
            ? PlayArea.Of(0)
            : PlayArea.Villains;
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
        var printed = new Printed()
            .With("hero", ("HP", "10"))
            .With("villain", ("ATK", "1"), ("HP", "20"))
            .With("boost", ("Boost", "1"))
            .With("sideScheme", ("Amplify", "2"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Undefended(world, printed);

        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:amplify-icon")]
    [Theory]
    [InlineData(0, 3)]
    // "Add one additional boost icon to that card for each amplify icon in
    // play", so a boost card worth 1 with two amplify icons is worth 3.
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public void AmplifyIconsAddToEveryBoostCard(int amplify, int expected)
    {
        var printed = new Printed()
            .With("villain", ("SCH", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "1"))
            .With("sideScheme", ("Amplify", amplify.ToString()));
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
        var printed = new Printed()
            .With("minion", ("HP", "9"))
            .With("steady", ("HP", "9"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.NotNull(Reveal.Afflict(world, printed, minion, Statuses.Tough, "test", []));
        Assert.Null(Reveal.Afflict(world, printed, minion, Statuses.Tough, "test", []));
        Assert.Equal(1, Statuses.Count(world, minion, Statuses.Tough));

        // And a steady character gets no extra tough card either: the keyword
        // names confused and stunned.
        var steady = world.CreateCard(
            "steady", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

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
        var printed = new Printed()
            .With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VictoryDisplay).Cards);
    }

    [Rule("rr:vulnerable.3")]
    [Fact]
    public void ASteadyVulnerableCharacterSurvivesTheFirstStatusCard()
    {
        // "If a character has both the steady and vulnerable keywords, the
        // vulnerable keyword does not take effect until that character has two
        // confused or two stunned status cards."
        var printed = new Printed()
            .With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

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
        var printed = new Printed()
            .With("minion", ("HP", "9"), ("Vulnerable", "1"), ("Stalwart", "1"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.Null(Reveal.Afflict(world, printed, minion, Statuses.Stunned, "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    /// <summary>Runs the attack, declining the defender prompt.</summary>
    private static void Undefended(World world, Printed printed)
    {
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked");
            Sequence.Answer(world, printed, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, printed, abilities, events);
        }
    }

    /// <summary>A treachery in the revealing area, ready for its keywords.</summary>
    private static Card Treachery(World world) =>
        world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));

    private static World Board(Printed printed, int players = 1)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("identity,hero", world.Seats[seat].Hero);
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    /// <summary>Gives non-exhaustion tests a replacement encounter deck.</summary>
    private static void KeepEncounterDeckLive(World world) =>
        world.CreateCard("replacement", world.AreaOf(DeckType.EncounterDeck));

    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, string[]> traits = new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "temp" => CardKind.Support,
            "ally" => CardKind.Ally,
            "boost" => CardKind.Treachery,
            "permanentish" => CardKind.Support,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "minion" or "steady" => CardKind.Minion,
            "sideScheme" => CardKind.EncounterSideScheme,
            "obligation" => CardKind.Obligation,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        /// <summary>Traits, upper-cased as the digest spells them.</summary>
        public Printed Trait(string faceId, params string[] names)
        {
            traits[faceId] = names;
            return this;
        }

        public IReadOnlyList<string> Traits(string faceId) =>
            traits.TryGetValue(faceId, out string[]? found) ? found : [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        /// <summary>Stated directly rather than as stars inside ATK/THW.</summary>
        public long ConsequentialDamage(string faceId, string attribute) =>
            attribute == "ATK" ? PrintedValue(faceId, "AtkIcons", 1) : 0;
    }

    private sealed class ConstantUsesLoss : NoCardAbilities
    {
        private readonly IReadOnlyList<(int Source, int Affected)> losses;

        public ConstantUsesLoss(int source, int affected)
            : this((source, affected))
        {
        }

        public ConstantUsesLoss(params (int Source, int Affected)[] losses)
        {
            this.losses = losses;
        }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) =>
        [
            .. losses
                .Where(loss => loss.Source == card.ObjectId)
                .Select(loss => new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    Characteristics.LossOf("uses"),
                    Card: loss.Source,
                    Affects: loss.Affected,
                    Lasts: Duration.WhileInPlay)),
        ];
    }

    private sealed class DependentConstantUsesLoss(
        int source, int bridge, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Traits.Granted + "ENABLED",
                        Card: source,
                        Affects: bridge,
                        Lasts: Duration.WhileInPlay),
                ];
            }

            return card.ObjectId == bridge
                && Traits.Has(world, card, "ENABLED", world.Facts)
                    ?
                    [
                        new ContinuousEffect(
                            EffectSource.ConstantAbility,
                            Characteristics.LossOf("uses"),
                            Card: bridge,
                            Affects: affected,
                            Lasts: Duration.WhileInPlay),
                    ]
                    : [];
        }
    }

    private sealed class UsesLossWithDependentPermanent(
        int source, int first, int second, int attachment) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: source,
                        Affects: first,
                        Lasts: Duration.WhileInPlay),
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: source,
                        Affects: second,
                        Lasts: Duration.WhileInPlay),
                ];
            }

            return card.ObjectId == second
                ?
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        "permanent",
                        Amount: 1,
                        Card: second,
                        Affects: attachment,
                        Lasts: Duration.WhileInPlay),
                ]
                : [];
        }
    }

    private sealed class UsesLossWithDormantPermanent : NoCardAbilities
    {
        private readonly int source;
        private readonly int first;
        private readonly int second;
        private readonly int grantor;
        private readonly int attachment;

        public UsesLossWithDormantPermanent(
            int source, int first, int second, int attachment)
            : this(source, first, second, second, attachment)
        {
        }

        public UsesLossWithDormantPermanent(
            int source, int first, int second, int grantor, int attachment)
        {
            this.source = source;
            this.first = first;
            this.second = second;
            this.grantor = grantor;
            this.attachment = attachment;
        }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: source,
                        Affects: first,
                        Lasts: Duration.WhileInPlay),
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: source,
                        Affects: second,
                        Lasts: Duration.WhileInPlay),
                ];
            }

            return card.ObjectId == grantor
                && !DeckTypes.IsInPlay(world.Cards[first].Area.Type)
                    ?
                    [
                        new ContinuousEffect(
                            EffectSource.ConstantAbility,
                            "permanent",
                            Amount: 1,
                            Card: grantor,
                            Affects: attachment,
                            Lasts: Duration.WhileInPlay),
                    ]
                    : [];
        }
    }

    private sealed class UsesLossWithSurvivingReplacement(
        int source, int surviving, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            bool loses = card.ObjectId == source
                || card.ObjectId == surviving
                && !DeckTypes.IsInPlay(world.Cards[source].Area.Type);
            return loses
                ?
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: card.ObjectId,
                        Affects: affected,
                        Lasts: Duration.WhileInPlay),
                ]
                : [];
        }
    }

    private sealed class ReplacementUsesLossCascade(
        int source, int second, int third) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return [Loss(source, second)];
            }
            if (card.ObjectId != second)
            {
                return [];
            }

            var effects = new List<ContinuousEffect> { Loss(second, third) };
            if (!DeckTypes.IsInPlay(world.Cards[source].Area.Type))
            {
                effects.Add(Loss(second, second));
            }
            return effects;
        }

        private static ContinuousEffect Loss(int source, int affected) => new(
            EffectSource.ConstantAbility,
            Characteristics.LossOf("uses"),
            Card: source,
            Affects: affected,
            Lasts: Duration.WhileInPlay);
    }

    private sealed class LatchedUsesDeparture(
        int source, int uses, int bridge) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return [Loss(source, uses)];
            }
            if (card.ObjectId == uses)
            {
                return
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Traits.Granted + "ENABLED",
                        Card: uses,
                        Affects: bridge,
                        Lasts: Duration.WhileInPlay),
                ];
            }
            return card.ObjectId == bridge
                && !Traits.Has(world, card, "ENABLED", world.Facts)
                    ? [Loss(bridge, uses)]
                    : [];
        }

        private static ContinuousEffect Loss(int source, int affected) => new(
            EffectSource.ConstantAbility,
            Characteristics.LossOf("uses"),
            Card: source,
            Affects: affected,
            Lasts: Duration.WhileInPlay);
    }

    private sealed class PresenceDependentUsesLoss(
        int presence, int bridge, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) =>
            card.ObjectId == bridge
            && DeckTypes.IsInPlay(world.Cards[presence].Area.Type)
                ?
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: bridge,
                        Affects: affected,
                        Lasts: Duration.WhileInPlay),
                ]
                : [];
    }

    private sealed class UsesLossWithSelfPermanentAttachment(
        int source, int host, int attachment) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        Characteristics.LossOf("uses"),
                        Card: source,
                        Affects: host,
                        Lasts: Duration.WhileInPlay),
                ];
            }
            return card.ObjectId == attachment
                ?
                [
                    new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        "permanent",
                        Amount: 1,
                        Card: attachment,
                        Affects: attachment,
                        Lasts: Duration.WhileInPlay),
                ]
                : [];
        }
    }
}
