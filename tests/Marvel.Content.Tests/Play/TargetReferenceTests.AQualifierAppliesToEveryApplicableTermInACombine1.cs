using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class TargetReferenceAQualifierAppliesToEveryApplicableTermInACombineTests : TargetReferenceTestBase
{
    [Rule("rr:qualifiers")]
    [Fact]
    public void AQualifierAppliesToEveryApplicableTermInACombinedSelector()
    {
        // “If ability text includes a qualifier followed by multiple terms,
        // the qualifier applies to each item in the list, if applicable.” The
        // SHIELD qualifier filters both “upgrade” and “support”; it is not
        // consumed by the first term.
        var runner = Runner("01006", """{ "exhaust": { "withTrait": { "cards": { "query": "upgradesAndSupportsYouControl" }, "trait": "S.H.I.E.L.D" } } }""");
        Card? source = null;
        Card? support = null;
        Card? upgrade = null;
        Card? unqualified = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            support = InPlay(board, "01092", DeckType.SupportsArea);
            upgrade = InPlay(board, "27182a", DeckType.UpgradesArea);
            unqualified = InPlay(board, "01007", DeckType.UpgradesArea);
        }, runner);
        ResolveAction(game, source!);
        Assert.False(support!.Ready);
        Assert.False(upgrade!.Ready);
        Assert.True(unqualified!.Ready);
    }

    [Rule("rr:referential-ability")]
    [Rule("rr:referential-ability.step.1")]
    [Fact]
    public void ACardReferringToItsOwnSharedTitleMeansItself()
    {
        // “The card on which the referential ability is printed.” The ally and
        // identity share She-Hulk's title, but the ally's own ability damages
        // the ally rather than the identity.
        var runner = Runner("10013", """{ "dealDamage": { "cards": { "titled": "She-Hulk" }, "amount": 1 } }""");
        Card? ally = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01019a");
            ally = InPlay(board, "10013", DeckType.AlliesArea);
        }, runner, hero: "she_hulk");
        ResolveAction(game, ally!);
        Assert.Equal(1, ally!.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:referential-ability.step.2")]
    [Fact]
    public void AnIdentityAssociatedCardWinsASharedTitleReference()
    {
        // Identity cards and identity-specific cards are the second tier. The
        // She-Hulk upgrade therefore refers to its identity before the basic
        // ally that happens to share the title.
        var runner = Runner("01028", """{ "giveStatus": { "card": { "titled": "She-Hulk" }, "status": "tough" } }""");
        Card? source = null;
        Card? ally = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01019a");
            source = InPlay(board, "01028", DeckType.UpgradesArea);
            ally = InPlay(board, "10013", DeckType.AlliesArea);
        }, runner, hero: "she_hulk");
        ResolveAction(game, source!);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.False(Statuses.Has(world, ally!, Statuses.Tough));
    }

    [Rule("rr:referential-ability.step.2")]
    [Rule("rr:form-change-form.4")]
    [Fact]
    public void AnInactiveIdentityTitleDoesNotFallThroughToAnUnrelatedCard()
    {
        // The associated She-Hulk identity remains the highest referential
        // tier while Jennifer Walters is faceup. That inactive hero title is
        // not a valid target, and the reference cannot fall through to the
        // unrelated basic ally that shares it.
        var runner = Runner("01028", """{ "giveStatus": { "card": { "titled": "She-Hulk" }, "status": "tough" } }""");
        Card? source = null;
        Card? ally = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01019b");
            source = InPlay(board, "01028", DeckType.UpgradesArea);
            ally = InPlay(board, "10013", DeckType.AlliesArea);
        }, runner, hero: "she_hulk");
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.False(Statuses.Has(world, ally!, Statuses.Tough));
    }

    [Rule("rr:referential-ability.step.2")]
    [Fact]
    public void AnObligationIsAssociatedWithItsIdentity()
    {
        // Step 2 includes "the identity's obligation cards." Eviction Notice
        // therefore means the Spider-Man identity when its synthetic effect
        // names Spider-Man, not an unrelated ally with the same printed title.
        AssertRevealedIdentityAssociation(source: "01165", identity: "01001a", unrelated: "04045", hero: "spider_man");
    }

    [Rule("rr:referential-ability.step.2")]
    [Fact]
    public void ANemesisCardIsAssociatedWithItsIdentity()
    {
        // Step 2 includes "the identity's nemesis set." Sweeping Swoop's
        // `spider_man_nemesis` set therefore outranks the unrelated basic ally
        // when its synthetic effect names Spider-Man.
        AssertRevealedIdentityAssociation(source: "01168", identity: "01001a", unrelated: "04045", hero: "spider_man");
    }

    [Rule("rr:referential-ability.step.3")]
    [Fact]
    public void APlayerCardSharedTitleReferenceExcludesEncounterCards()
    {
        // At the final tier an ability on a player card refers to player cards.
        // Both player allies are affected; the encounter minion with the same
        // title is not.
        var runner = Runner("01006", """{ "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } }""");
        Card? source = null;
        Card? first = null;
        Card? second = null;
        Card? encounter = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            first = InPlay(board, "04045", DeckType.AlliesArea);
            second = InPlay(board, "13019", DeckType.AlliesArea);
            encounter = board.CreateCard("56180", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, runner, hero: "captain_marvel");
        ResolveAction(game, source!);
        Assert.Equal(1, first!.Damage);
        Assert.Equal(1, second!.Damage);
        Assert.Equal(0, encounter!.Damage);
    }

    [Rule("rr:target.2.3")]
    [Fact]
    public void DrawingRequiresACardCurrentlyInThePlayersDeck()
    {
        // A draw “always [has] a valid target so long as that player has at
        // least one card in their deck.” A discard pile that could replenish
        // the deck after it empties does not satisfy that initiation clause.
        var runner = Runner("01006", """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var discard = board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            foreach (var card in board.Seats[0].Deck.Cards.ToList())
            {
                World.MoveToTop(card, discard);
            }
        }, runner);
        Assert.NotEmpty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Fact]
    public void ChooseRequiresAtLeastOneSelectableTarget()
    {
        // “Choose a [game element]” means a target must be selected for the
        // ability to initiate. An empty minion query therefore removes the
        // action rather than presenting an unanswerable prompt.
        var runner = Runner("01006", """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "exhaust": "chosen" } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, "01006", DeckType.SupportsArea), runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:target.3")]
    [Fact]
    public void ChooseFiltersCardsThatItsNestedEffectCannotAffect()
    {
        // Med Team says to choose a friendly character and heal it. A full-
        // health character meets the noun selector but is not a valid target
        // for the nested heal, so the support cannot spend its cost merely to
        // present an ineffective choice.
        Card? medTeam = null;
        var(game, _) = Playing(board =>
        {
            medTeam = InPlay(board, "01080", DeckType.SupportsArea);
            medTeam.PlaceTokens("c_medical", 3);
        }, AuthoredCards.Runner());
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == medTeam!.ObjectId);
        Assert.True(medTeam!.Ready);
        Assert.Equal(3, medTeam.Tokens["c_medical"]);
    }

    [Rule("rr:target.5")]
    [Fact]
    public void ADelayedEffectDoesNotRequireItsFutureTargetAtInitiation()
    {
        // “The damaged character” is supplied only if the future damage
        // occurrence happens. Its absence now cannot invalidate the action
        // that creates the delayed effect.
        var runner = Runner("01006", """{ "delayUntil": { "condition": "WhenDamageDealt", "effect": { "giveStatus": { "card": "damaged", "status": "stunned" } } } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, "01006", DeckType.SupportsArea), runner);
        ResolveAction(game, source!);
        Assert.Contains(world.Effects.Active(), effect => effect.Source == EffectSource.DelayedEffect);
    }

    [Rule("rr:target.3.3")]
    [Fact]
    public void ACostDoesNotMakeItsOwnTargetValidForTheEffect()
    {
        // The cost could exhaust Helicarrier, after which the effect could
        // ready it. Target validity deliberately ignores that cost, so the
        // currently-ready support is not a valid ready target and the action
        // cannot begin by manufacturing one during payment.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01092", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              "cost": { "exhaust": "this" },
              "effect": { "ready": { "titled": "Helicarrier" } }
            } ] } ] }
            """));
        Card? source = null;
        Card? carrier = null;
        var(game, _) = Playing(board =>
        {
            source = carrier = InPlay(board, "01092", DeckType.SupportsArea);
        }, runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.True(carrier!.Ready);
    }

    [Rule("rr:target.3.6")]
    [Fact]
    public void DamagePreventedByToughStillHasAValidTarget()
    {
        // Damage that is dealt but prevented still affects its target. Tough
        // therefore does not remove the minion from eligibility; it prevents
        // the resolving damage and is discarded instead.
        var runner = Runner("01006", """{ "dealDamage": { "cards": { "query": "minions" }, "amount": 1 } }""");
        Card? source = null;
        Card? minion = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, minion, Statuses.Tough);
        }, runner);
        ResolveAction(game, source!);
        Assert.Equal(0, minion!.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }
}
