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

    [Rule("rr:side-scheme")]
    [Fact]
    public void ASideSchemeEntersPlayWithItsStartingThreatAndItsHinder()
    {
        // Two different sources of threat on one card, and they add: the
        // printed starting threat every scheme has, and the keyword.
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
    [Fact]
    public void AHeroWithRetaliateHitsBackAtAnAttackingEnemy()
    {
        // The other direction: `rr:retaliate-x` is a character rule, not a
        // player one, so an enemy attacking a hero with retaliate takes the
        // damage the same way.
        var printed = new Printed()
            .With("hero", ("HP", "10"), ("Retaliate", "2"))
            .With("villain", ("ATK", "3"), ("HP", "20"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
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

        Assert.Equal(boosted, Marvel.Rules.Timing.Keywords.IsBoosted(enemy, printed, 1));
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
    [Fact]
    public void ACrisisIconOutOfPlayStopsNothing()
    {
        // "While at least one crisis icon is **in play**." The encounter deck
        // is full of them in an ordinary game, and counting those would make
        // the main scheme permanently unthwartable.
        var printed = new Printed().With("sideScheme", ("Crisis", "1"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("sideScheme", world.AreaOf(DeckType.EncounterDeck));

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
}
