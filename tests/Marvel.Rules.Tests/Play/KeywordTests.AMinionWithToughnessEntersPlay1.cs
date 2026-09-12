using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordAMinionWithToughnessEntersPlayTests : KeywordTestBase
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
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("toughness"), Affects: minion.ObjectId));
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
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        bool defeated = DamagePlacement.Deal(world, printed, minion, minion, 9, "test", "test", []);
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
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Statuses.Give(world, minion, Statuses.Tough);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
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
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), card, 0, []);
        Assert.Equal(expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
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
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("incite"), Affects: card.ObjectId));
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), card, 0, []);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(RevealKeywords.KeywordAbilities(world, printed, card, 0));
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
        var printed = new Printed().With("treachery", ("Incite", "1")).With("scheme", ("TargetThreat", "3"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 2);
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), Treachery(world), 0, []);
        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    [Rule("rr:incite-x")]
    [Fact]
    public void InciteThatDoesNotReachTheTargetLeavesTheGameRunning()
    {
        // The converse, and the reason the check is a comparison rather than
        // "somebody placed threat": one short is not completed.
        var printed = new Printed().With("treachery", ("Incite", "1")).With("scheme", ("TargetThreat", "4"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), Treachery(world), 0, []);
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
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), card, 0, []);
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
        var printed = new Printed().With("treachery", ("Surge", printedSurge.ToString()));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));
        for (int instance = 0; instance < gainedSurge; instance++)
        {
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "surge", Amount: 1, Affects: card.ObjectId));
        }

        world.CreateCard("after", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("next", world.AreaOf(DeckType.EncounterDeck));
        RevealKeywords.Keywords(world, printed, new NoCardAbilities(), card, 0, []);
        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(["next"], queue.Cards.Select(dealt => dealt.FaceId));
        Assert.Equal(["after"], world.AreaOf(DeckType.EncounterDeck).Cards.Select(next => next.FaceId));
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
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
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
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("hinder"), Affects: scheme.ObjectId));
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
        var printed = new Printed().With("sideScheme", ("Hinder", "2"), ("StartingThreat", "3"));
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
        var printed = new Printed().With("hero", ("ATK", "2"), ("HP", "10")).With("minion", ("HP", "9"), ("Retaliate", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate", Amount: 2, Affects: minion.ObjectId));
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }
}
