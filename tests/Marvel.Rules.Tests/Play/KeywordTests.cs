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

        bool defeated = Damage.Deal(world, printed, minion, 9, "test", "test", []);

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

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
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

        Reveal.Keywords(world, printed, card, 0, []);

        Assert.Equal(
            expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
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

        Reveal.Keywords(world, printed, card, 0, []);

        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(["next"], queue.Cards.Select(dealt => dealt.FaceId));
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

    [Rule("rr:retaliate-x")]
    [Rule("rr:attack-player-ability-type.5.1")]
    [Fact]
    public void RetaliateHitsTheAttackerBack()
    {
        // "**Forced Response**: after this character is attacked, deal X damage
        // to the attacker."
        var printed = new Printed()
            .With("hero", ("ATK", "2"), ("HP", "10"))
            .With("minion", ("HP", "9"), ("Retaliate", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, printed, 0, minion, []);

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

        Damage.Deal(world, printed, minion, 1, "test", "test", []);

        Assert.Equal(DeckType.VictoryDisplay, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
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
            "minion" => CardKind.Minion,
            "sideScheme" => CardKind.EncounterSideScheme,
            "obligation" => CardKind.Obligation,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

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
