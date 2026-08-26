using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Constant abilities — the half of <c>rr:ability.5</c> that has no trigger.
/// </summary>
/// <remarks>
/// <para>
/// "An ability prefaced by a bold timing trigger followed by a colon is
/// referred to as a triggered ability. An ability without a bold timing trigger
/// is referred to as a constant ability." Everything the engine could run until
/// now was the first kind: something happened, a window opened, a card answered
/// it. A constant ability answers nothing, and that is the whole difficulty —
/// there is no moment at which to run it, so it cannot be run at all. It is
/// read.
/// </para>
/// <para>
/// Unus is why the reading has to be continuous rather than done once when the
/// card arrives. His text is <c>rr:ability.9</c> word for word — "some constant
/// abilities continuously seek a specific condition <i>(denoted by words such
/// as 'during', 'if', or 'while')</i>. The effects of such abilities are active
/// anytime the specific condition is met" — so the same card is retaliating on
/// one turn and not the next, with nothing happening to it in between. What
/// changed was a scheme on the other side of the table.
/// </para>
/// </remarks>
public sealed class ConstantAbilityTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:ability.9")]
    [Theory]
    [InlineData(0, false, false, false)]
    [InlineData(2, false, false, false)]
    [InlineData(3, true, false, false)]
    [InlineData(5, true, false, false)]
    [InlineData(6, true, true, false)]
    [InlineData(8, true, true, false)]
    [InlineData(9, true, true, true)]
    [InlineData(30, true, true, true)]
    public void UnusGainsEachKeywordAtItsOwnThresholdAndKeepsTheOnesBelow(
        long threat, bool retaliate, bool stalwart, bool amplify)
    {
        // "If the amount of threat on Gene Pool is at least: 3 -- Unus gains
        // retaliate 1. 6 -- Unus **also** gains stalwart. 9 -- Unus **also**
        // gains a [amplify] icon."
        //
        // "Also" is the word being tested. The three clauses are cumulative
        // rather than a ladder, so at nine threat all three hold -- which is
        // why the card is three independent conditions and not an if/else
        // chain, and why the table walks past each boundary in both directions.
        var (world, unus, genePool) = Board();
        genePool.PlaceTokens("k_threat", threat);

        Assert.Equal(retaliate ? 1 : 0, Modified(world, unus, "retaliate"));
        Assert.Equal(stalwart ? 1 : 0, Modified(world, unus, "stalwart"));
        Assert.Equal(amplify ? 1 : 0, Modified(world, unus, "amplify"));
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void ThwartingGenePoolTakesTheKeywordsBackAgain()
    {
        // The direction that a register-when-it-arrives design gets wrong.
        // Nothing happens to Unus here at all: no card is played on him, no
        // ability resolves, and he does not move. Threat comes off a scheme
        // somewhere else and he stops being stalwart, because "the effects of
        // such abilities are active anytime the specific condition is met" and
        // it is no longer met.
        var (world, unus, genePool) = Board();
        genePool.PlaceTokens("k_threat", 9);
        Assert.Equal(1, Modified(world, unus, "stalwart"));

        genePool.PlaceTokens("k_threat", -6);

        Assert.Equal(1, Modified(world, unus, "retaliate"));
        Assert.Equal(0, Modified(world, unus, "stalwart"));
        Assert.Equal(0, Modified(world, unus, "amplify"));
    }

    [Rule("rr:retaliate-x")]
    [Fact]
    public void TheGrantedRetaliateActuallyHitsBack()
    {
        // A number in a field is not a game effect. `rr:retaliate-x` is
        // "**Forced Response**: after this character is attacked, deal X damage
        // to the attacker", and this is the attack: the hero swings at Unus and
        // takes one back, having taken none the round before.
        var (world, unus, genePool) = Board();
        var hero = world.Seats[0].IdentityCard;

        Damage.Attack(world, Cards, hero, unus, 1, "test", "Attack", []);
        Assert.Equal(0, hero.Damage);

        genePool.PlaceTokens("k_threat", 3);
        Damage.Attack(world, Cards, hero, unus, 1, "test", "Attack", []);

        Assert.Equal(1, hero.Damage);
    }

    [Rule("rr:stalwart.1")]
    [Fact]
    public void TheGrantedStalwartActuallyRefusesTheStatus()
    {
        // "A stalwart character cannot have confused or stunned status cards."
        // `Statuses.Limit` is where the engine asks, and it asks through
        // `StateFields.Modified` -- so a grant that reached the field but not
        // the rule would still stun him.
        var (world, unus, genePool) = Board();

        Assert.Equal(1, Statuses.Limit(world, Cards, unus, Statuses.Stunned));

        genePool.PlaceTokens("k_threat", 6);

        Assert.Equal(0, Statuses.Limit(world, Cards, unus, Statuses.Stunned));
    }

    [Rule("rr:amplify-icon")]
    [Fact]
    public void TheGrantedAmplifyIconIsCountedWithThePrintedOnes()
    {
        // "Add one additional boost icon to that card for each amplify icon in
        // play." `MainScheme.Amplify` totals the icons over everything in play,
        // and a granted one has to be in that total or the boost card it should
        // have swelled is dealt at its printed strength.
        var (world, unus, genePool) = Board();

        Assert.Equal(0, MainScheme.Amplify(world, Cards));

        genePool.PlaceTokens("k_threat", 9);

        Assert.Equal(1, MainScheme.Amplify(world, Cards));
    }

    [Rule("rr:ability")]
    [Fact]
    public void AConstantAbilityLeavesWithItsCard()
    {
        // "A constant ability becomes active as soon as its card enters play
        // and remains active while the card is in play." Unus is defeated and
        // his stage goes to the villain deck; the threat on Gene Pool has not
        // moved, and nothing is retaliating any more.
        var (world, unus, genePool) = Board();
        genePool.PlaceTokens("k_threat", 9);
        Assert.Equal(1, Modified(world, unus, "retaliate"));

        World.MoveToTop(unus, world.AreaOf(DeckType.VillainDeck));

        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:ability.10")]
    [Fact]
    public void TwoCopiesInPlayAffectTheGameIndependently()
    {
        // "If multiple instances of the same constant ability are in play, each
        // instance affects the game independently." Two stages in play at once
        // is not a board the villain deck can build, but the rule is about
        // instances rather than about villains -- and a derivation that keyed
        // effects by printed id rather than by card would silently answer one.
        var (world, unus, genePool) = Board();
        var second = world.CreateCard(
            AuthoredCards.Unus[1], world.AreaOf(DeckType.VillainArea));
        genePool.PlaceTokens("k_threat", 3);

        Assert.Equal(1, Modified(world, unus, "retaliate"));
        Assert.Equal(1, Modified(world, second, "retaliate"));
        Assert.Equal(2, world.Effects.Active().Count);
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void AConstantAbilityIsNeverOfferedInAWindow()
    {
        // It has no triggering condition, so there is no occurrence it answers
        // and no window it belongs in. Asking with every condition the engine
        // produces is the strong form of that: not "it does not answer this
        // one" but "it answers none of them".
        var (world, unus, _) = Board();
        var runner = AuthoredCards.Runner();

        foreach (string condition in Steps.EveryCondition)
        {
            var occurrence = new Occurrence(0, [condition], Subject: unus.ObjectId);
            foreach (var window in Enum.GetValues<WindowKind>())
            {
                Assert.Empty(runner.Waiting(world, occurrence, window));
            }
        }
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void AConstantAbilityCarryingATriggeringConditionIsRefused()
    {
        // The typo this catches is an author's belief rather than a spelling:
        // they thought the card fires on something. Ignoring the key would
        // leave the belief in the file, and the card would work -- until the
        // next reader trusted it.
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Constant" },
                "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
            } ] } ] }
            """));

        Assert.Contains("is constant and triggers on", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATriggeredAbilityWithNoTriggeringConditionIsStillRefused()
    {
        // The other half of the same branch. Making `event` optional for a
        // constant must not make it optional for everything else, or an
        // ability that never fires becomes authorable.
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [ {
                "trigger": { "timing": "WhenRevealed" },
                "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
            } ] } ] }
            """));

        Assert.Contains("has no 'event'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AGrantWithNoAmountGivesOneAndNotNone()
    {
        // The default that decides whether "Unus also gains stalwart" means
        // anything. Zero would parse, derive, and read as not stalwart -- the
        // card's opposite, with no error anywhere.
        var (world, unus, genePool) = Board();
        genePool.PlaceTokens("k_threat", 6);

        var granted = Assert.Single(
            world.Effects.Active(), effect => effect.Kind == "stalwart");

        Assert.Equal(1, granted.Amount);
        Assert.Equal(EffectSource.ConstantAbility, granted.Source);
        Assert.Equal(unus.ObjectId, granted.Affects);

        // Provenance, and not the same question. `Card` is what must stay in
        // play for the effect to be in force, and for a card granting to
        // something else -- an attachment giving the villain a keyword -- the
        // two are different cards.
        Assert.Equal(unus.ObjectId, granted.Card);
        Assert.True(granted.Lasts?.IsWhileInPlay);
    }

    [Fact]
    public void AConstantGrantingSomethingTheEngineDoesNotReadThrowsNamingIt()
    {
        // `stallwart` would otherwise sit in the dataset granting nothing for
        // ever, which is the exact failure the whole ability dataset is held
        // against printed data to avoid.
        var world = Bare();
        world.CreateCard("45059", world.AreaOf(DeckType.VillainArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "grant": { "card": "this", "keyword": "stallwart" } }
            } ] } ] }
            """));

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => world.Effects.Active());

        Assert.Contains("grants 'stallwart'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConstantAbilityThatDoesSomethingRatherThanGrantingThrows()
    {
        // A constant ability has no moment, so it cannot deal damage: there is
        // no answer to "when". A card wanting one wants a design, and until
        // then it says so by name rather than by doing three quarters of what
        // it prints.
        var world = Bare();
        world.CreateCard("45059", world.AreaOf(DeckType.VillainArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "dealDamage": { "card": "this", "amount": 1 } }
            } ] } ] }
            """));

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => world.Effects.Active());

        Assert.Contains("'dealDamage'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConstantAbilityReadingTheEffectListSaysSoRatherThanGuessing()
    {
        // The one thing the derivation cannot do. Working out what is in force
        // is what called the card, so a card asking back would need the list to
        // settle on itself. `minBy` drops permanents, which it learns by
        // reading the effects -- so this is a real route and not a contrived
        // one.
        var world = Bare();
        world.CreateCard(
            AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{AuthoredCards.SpiderMan}}", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "if": {
                    "test": { "exists": {
                        "minBy": { "of": { "query": "supportsYouControl" }, "by": "cost" } } },
                    "then": { "grant": { "card": "this", "keyword": "stalwart" } } } }
            } ] } ] }
            """));

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => world.Effects.Active());

        Assert.Contains("while they were being worked out", refused.Message, StringComparison.Ordinal);

        // And the board is still answerable. One card the engine cannot settle
        // must not leave every later question throwing the same complaint about
        // a card that is no longer being asked.
        world.Abilities = new NoCardAbilities();
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void OneCardsConstantAbilityIsReadWithoutItsTriggeredOnes()
    {
        // Prelate Sidearm is this shape and so are most attachments: a line
        // with no bold trigger and a "Forced Response" underneath it. Reading
        // the card must take the first and leave the second, because the second
        // has a moment and running it here would be that moment happening for
        // no reason.
        var world = Bare();
        var villain = world.CreateCard("45059", world.AreaOf(DeckType.VillainArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [
                {
                  "trigger": { "timing": "Constant" },
                  "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
                },
                {
                  "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed" },
                  "effect": { "dealDamage": { "card": "this", "amount": 1 } }
                }
            ] } ] }
            """));

        var granted = Assert.Single(world.Effects.Active());

        Assert.Equal("stalwart", granted.Kind);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:star-icon.2")]
    [Fact]
    public void AConstantOnAnAttachmentGrantsToWhatItIsAttachedTo()
    {
        // The shape every setup attachment prints: "Attached villain gains the
        // [[BRUTE]] trait **and steady**." The card whose text this is and the
        // card the keyword lands on are two different cards, and a derivation
        // that granted to the source would make Super Strength steady rather
        // than the villain wearing it — with the villain looking entirely
        // normal and the attachment doing nothing anyone could see.
        var (world, unus, _) = Board();
        var strength = world.CreateCard(
            "40155",
            world.AreaOf(
                DeckType.UpgradesArea, unus.Area.PlayArea, unus.ObjectId, unus.Area.CardOwner));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "40155", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "grant": { "card": "attachedTo", "keyword": "steady" } }
            } ] } ] }
            """));

        Assert.Equal(2, Statuses.Limit(world, Cards, unus, Statuses.Stunned));
        Assert.Equal(1, Statuses.Limit(world, Cards, strength, Statuses.Stunned));

        // The two halves the effect records, which are two different cards
        // here and the same card everywhere else: whose text this is, and who
        // it lands on.
        var granted = Assert.Single(world.Effects.Active());
        Assert.Equal(strength.ObjectId, granted.Card);
        Assert.Equal(unus.ObjectId, granted.Affects);

        // "Remains active while the card is in play" — the attachment's card,
        // not the villain's. Unus has not moved.
        World.MoveToTop(strength, world.AreaOf(DeckType.EncounterDiscardPile));

        Assert.Equal(1, Statuses.Limit(world, Cards, unus, Statuses.Stunned));
    }

    [Rule("rr:overkill")]
    [Fact]
    public void AConstantCanGrantAKeywordThatIsNotAPrintedField()
    {
        // Overkill, piercing and ranged are not attributes on any card in the
        // pool -- they arrive by being granted, and `Keywords.Has` looks for
        // them by name rather than by reading a field. So the check that
        // refuses `stallwart` has to know about two vocabularies, and a check
        // that knew only about printed fields would refuse Flight's star line
        // as a typo.
        var world = Bare();
        var villain = world.CreateCard("45059", world.AreaOf(DeckType.VillainArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45059", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "grant": { "card": "this", "keyword": "overkill" } }
            } ] } ] }
            """));

        Assert.True(Keywords.Has(world, villain, Keywords.Overkill, Cards));
    }

    private static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    private static World Bare()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(AuthoredCards.SpiderMan, seat.Hero);
        return world;
    }

    /// <summary>Unus in the villain area, Gene Pool in play, no threat on it.</summary>
    /// <remarks>
    /// Built by hand rather than dealt, because Gene Pool prints "Setup" and
    /// the deal cannot yet resolve a setup ability — which is what puts it into
    /// play in a real game. What is under test is what Unus reads, not how the
    /// scheme got there.
    /// </remarks>
    private static (World World, Card Unus, Card GenePool) Board()
    {
        var world = Bare();
        var unus = world.CreateCard(AuthoredCards.Unus[0], world.AreaOf(DeckType.VillainArea));
        var genePool = world.CreateCard(
            AuthoredCards.GenePool, world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = AuthoredCards.Runner();
        return (world, unus, genePool);
    }
}
