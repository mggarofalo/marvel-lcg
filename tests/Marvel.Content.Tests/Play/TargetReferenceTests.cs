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

/// <summary>Target validity and printed-title references.</summary>
public sealed class TargetReferenceTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:qualifiers")]
    [Fact]
    public void AQualifierAppliesToEveryApplicableTermInACombinedSelector()
    {
        // “If ability text includes a qualifier followed by multiple terms,
        // the qualifier applies to each item in the list, if applicable.” The
        // SHIELD qualifier filters both “upgrade” and “support”; it is not
        // consumed by the first term.
        var runner = Runner(
            "01006",
            """{ "exhaust": { "withTrait": { "cards": { "query": "upgradesAndSupportsYouControl" }, "trait": "S.H.I.E.L.D" } } }""");
        Card? source = null;
        Card? support = null;
        Card? upgrade = null;
        Card? unqualified = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                support = InPlay(board, "01092", DeckType.SupportsArea);
                upgrade = InPlay(board, "27182a", DeckType.UpgradesArea);
                unqualified = InPlay(board, "01007", DeckType.UpgradesArea);
            },
            runner);

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
        var runner = Runner(
            "10013",
            """{ "dealDamage": { "cards": { "titled": "She-Hulk" }, "amount": 1 } }""");
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                ally = InPlay(board, "10013", DeckType.AlliesArea);
            },
            runner,
            hero: "she_hulk");

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
        var runner = Runner(
            "01028",
            """{ "giveStatus": { "card": { "titled": "She-Hulk" }, "status": "tough" } }""");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                source = InPlay(board, "01028", DeckType.UpgradesArea);
                ally = InPlay(board, "10013", DeckType.AlliesArea);
            },
            runner,
            hero: "she_hulk");

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
        var runner = Runner(
            "01028",
            """{ "giveStatus": { "card": { "titled": "She-Hulk" }, "status": "tough" } }""");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019b");
                source = InPlay(board, "01028", DeckType.UpgradesArea);
                ally = InPlay(board, "10013", DeckType.AlliesArea);
            },
            runner,
            hero: "she_hulk");

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        Assert.False(Statuses.Has(world, ally!, Statuses.Tough));
    }

    [Rule("rr:referential-ability.step.2")]
    [Fact]
    public void AnObligationIsAssociatedWithItsIdentity()
    {
        // Step 2 includes "the identity's obligation cards." Eviction Notice
        // therefore means the Spider-Man identity when its synthetic effect
        // names Spider-Man, not an unrelated ally with the same printed title.
        AssertRevealedIdentityAssociation(
            source: "01165",
            identity: "01001a",
            unrelated: "04045",
            hero: "spider_man");
    }

    [Rule("rr:referential-ability.step.2")]
    [Fact]
    public void ANemesisCardIsAssociatedWithItsIdentity()
    {
        // Step 2 includes "the identity's nemesis set." Sweeping Swoop's
        // `spider_man_nemesis` set therefore outranks the unrelated basic ally
        // when its synthetic effect names Spider-Man.
        AssertRevealedIdentityAssociation(
            source: "01168",
            identity: "01001a",
            unrelated: "04045",
            hero: "spider_man");
    }

    [Rule("rr:referential-ability.step.3")]
    [Fact]
    public void APlayerCardSharedTitleReferenceExcludesEncounterCards()
    {
        // At the final tier an ability on a player card refers to player cards.
        // Both player allies are affected; the encounter minion with the same
        // title is not.
        var runner = Runner(
            "01006",
            """{ "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } }""");
        Card? source = null;
        Card? first = null;
        Card? second = null;
        Card? encounter = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                first = InPlay(board, "04045", DeckType.AlliesArea);
                second = InPlay(board, "13019", DeckType.AlliesArea);
                encounter = board.CreateCard(
                    "56180",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            runner,
            hero: "captain_marvel");

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
        var runner = Runner(
            "01006",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var discard = board.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
                foreach (var card in board.Seats[0].Deck.Cards.ToList())
                {
                    World.MoveToTop(card, discard);
                }
            },
            runner);

        Assert.NotEmpty(world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Fact]
    public void ChooseRequiresAtLeastOneSelectableTarget()
    {
        // “Choose a [game element]” means a target must be selected for the
        // ability to initiate. An empty minion query therefore removes the
        // action rather than presenting an unanswerable prompt.
        var runner = Runner(
            "01006",
            """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "exhaust": "chosen" } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, "01006", DeckType.SupportsArea),
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
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
        var (game, _) = Playing(
            board =>
            {
                medTeam = InPlay(board, "01080", DeckType.SupportsArea);
                medTeam.PlaceTokens("c_medical", 3);
            },
            AuthoredCards.Runner());

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == medTeam!.ObjectId);
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
        var runner = Runner(
            "01006",
            """{ "delayUntil": { "condition": "WhenDamageDealt", "effect": { "giveStatus": { "card": "damaged", "status": "stunned" } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, "01006", DeckType.SupportsArea),
            runner);

        ResolveAction(game, source!);

        Assert.Contains(
            world.Effects.Active(),
            effect => effect.Source == EffectSource.DelayedEffect);
    }

    [Rule("rr:target.3.3")]
    [Fact]
    public void ACostDoesNotMakeItsOwnTargetValidForTheEffect()
    {
        // The cost could exhaust Helicarrier, after which the effect could
        // ready it. Target validity deliberately ignores that cost, so the
        // currently-ready support is not a valid ready target and the action
        // cannot begin by manufacturing one during payment.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01092", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              "cost": { "exhaust": "this" },
              "effect": { "ready": { "titled": "Helicarrier" } }
            } ] } ] }
            """));
        Card? source = null;
        Card? carrier = null;
        var (game, _) = Playing(
            board =>
            {
                source = carrier = InPlay(board, "01092", DeckType.SupportsArea);
            },
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
        Assert.True(carrier!.Ready);
    }

    [Rule("rr:target.3.6")]
    [Fact]
    public void DamagePreventedByToughStillHasAValidTarget()
    {
        // Damage that is dealt but prevented still affects its target. Tough
        // therefore does not remove the minion from eligibility; it prevents
        // the resolving damage and is discarded instead.
        var runner = Runner(
            "01006",
            """{ "dealDamage": { "cards": { "query": "minions" }, "amount": 1 } }""");
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, minion, Statuses.Tough);
            },
            runner);

        ResolveAction(game, source!);

        Assert.Equal(0, minion!.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:target.3.4")]
    [Fact]
    public void OneEffectivePartMakesATargetValidForAMultiEffectAbility()
    {
        // “If an ability or game function has multiple effects on its target,
        // the target is valid if at least one of those effects can affect the
        // target.” Killmonger cannot take this upgrade's damage, but he can be
        // exhausted, so the action initiates and only the exhaust changes him.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01046", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "seq": [
                  { "dealDamage": { "cards": { "titled": "Killmonger" }, "amount": 1 } },
                  { "exhaust": { "titled": "Killmonger" } }
                ] }
              } ] },
              { "card": "01157", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventDamageFrom": { "card": "this", "sourceKind": "upgrade", "sourceTrait": "BLACK_PANTHER" } }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "01046", DeckType.UpgradesArea);
                target = board.CreateCard(
                    "01157",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            runner);

        ResolveAction(game, source!);

        Assert.Equal(0, target!.Damage);
        Assert.False(target.Ready);
    }

    [Rule("rr:target.3")]
    [Rule("rr:status-cards.1")]
    [Fact]
    public void AStatusAtItsLimitIsNotAValidTargetForTheSameStatus()
    {
        // A target is valid only if some part of the ability can affect it,
        // and a character cannot hold a second Tough card. The already-tough
        // villain therefore cannot make this status-only action initiable.
        var runner = Runner(
            "01006",
            """{ "giveStatus": { "card": { "query": "villain" }, "status": "tough" } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                Statuses.Give(
                    board, board.TheCardIn(DeckType.VillainArea)!, Statuses.Tough);
            },
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        Assert.Equal(
            1,
            Statuses.Count(
                world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Tough));
    }

    [Rule("rr:target.3.5")]
    [Rule("rr:ready.1")]
    [Fact]
    public void ACardThatCannotReadyIsSkippedAndDoesNotMakeTheAbilityLegal()
    {
        // A target is invalid when the ability would make it perform a game
        // function another ability prohibits. Offering and execution ask the
        // same CanReady question.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "ready": { "titled": "Helicarrier" } }
              } ] },
              { "card": "01092", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventReady": "this" }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                target = InPlay(board, "01092", DeckType.SupportsArea);
                target.Exhaust();
            },
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        Assert.False(target!.Ready);
    }

    [Rule("rr:target.3.7")]
    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void AGroupEffectUsesItsValidTargetAndSkipsOneThatCannotTakeDamage()
    {
        // The Black Panther upgrade cannot damage Killmonger, but the villain
        // remains a valid target. The ability initiates and resolves only
        // against that valid element.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01046", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "dealDamage": { "cards": { "query": "enemies" }, "amount": 1 } }
              } ] },
              { "card": "01157", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventDamageFrom": { "card": "this", "sourceKind": "upgrade", "sourceTrait": "BLACK_PANTHER" } }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? killmonger = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01046", DeckType.UpgradesArea);
                killmonger = board.CreateCard(
                    "01157",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        ResolveAction(game, source!);

        Assert.Equal(1, villain.Damage);
        Assert.Equal(0, killmonger!.Damage);
    }

    [Rule("rr:target.2")]
    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void ALabelledPowerSkipsAnEmptyStatusTargetGroup()
    {
        // An ability with multiple targets can initiate with at least one
        // valid target, and it does not resolve against invalid group members.
        // The villain remains a valid attack target while the empty minion
        // group contributes no status target and no simulator-only failure.
        var runner = Runner(
            "01006",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "giveStatus": {
                  "card": { "query": "minionsEngagedWithYou" },
                  "status": "stunned"
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, "01006", DeckType.SupportsArea),
            runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        ResolveAction(game, source!);

        Assert.Equal(1, villain.Damage);
        Assert.Empty(world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void ALabelledThwartHonorsItsExplicitCrisisException()
    {
        // Crisis normally says player cards cannot remove threat from the main
        // scheme. This exact effect says it ignores Crisis, so the explicit
        // exception wins and makes the declared thwart target valid.
        var runner = Runner(
            "01006",
            """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;

        ResolveAction(game, source!);

        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void ALabelledThwartBindsItsTargetBeforeCheckingACrisisException()
    {
        // The power target becomes `chosen` before the nested effect resolves.
        // Offer-time legality uses that same binding, so the explicit Crisis
        // exception is visible on both sides of the decision.
        var runner = Runner(
            "01006",
            """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "removeThreat": {
                "scheme": "chosen", "amount": 1,
                "ignoresCrisis": "true"
              } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;

        ResolveAction(game, source!);

        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:then")]
    [Fact]
    public void ALabelledThwartFindsACrisisExceptionInsideADependency()
    {
        // A `then` predecessor necessarily executes. Its explicit Crisis
        // exception therefore makes the declared target valid before the
        // dependent draw is considered.
        var runner = Runner(
            "01006",
            """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "then": {
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } },
                "then": { "draw": { "player": "you", "count": 1 } }
              } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;

        ResolveAction(game, source!);

        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void APrePaymentCrisisExceptionSurvivesACostChangingItsBranch()
    {
        // Target validity ignores the cost. Before payment the explicit
        // Crisis exception makes the main scheme a valid thwart target; after
        // discarding its source, the other branch draws instead. Scheduling
        // preserves the established target mode rather than failing after the
        // board has already mutated.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01092", "abilities": [ {
              "trigger": {
                "event": "WhenActionTriggered", "timing": "Action",
                "subject": "game"
              },
              "cost": { "discard": "this" },
              "effect": { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "if": {
                  "test": { "titleInPlay": "Helicarrier" },
                  "then": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } }
              } }
            } ] } ] }
            """));
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01092", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        int held = world.Seats[0].Hand.Cards.Count;

        ResolveAction(game, source!);

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AValidatedCrisisExceptionSurvivesAChoiceContinuation()
    {
        // The target decision belongs to initiation. A preceding choice
        // suspends and reconstructs the cast, so its continuation metadata
        // carries the validated power address rather than rechecking Crisis
        // after the chosen option has already changed the board.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "choose": { "options": [
                { "draw": { "player": "you", "count": 1 } },
                { "draw": { "player": "you", "count": 2 } }
              ] } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        int held = world.Seats[0].Hand.Cards.Count;

        ResolveAction(game, source!);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));

        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AValidatedCrisisExceptionSurvivesAnActivationContinuation()
    {
        // Enemy activation suspends this sequence and resumes it in a fresh
        // cast. The continuation carries the previously validated thwart
        // address, so completing the activation cannot expose a late Crisis
        // refusal before the following power is scheduled.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "enemyAttacks": { "enemies": { "query": "villain" } } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);
        var activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(
            activation.Subject, activation.Seat, Attacking: true,
            activation.ActivationId, Made: false));

        Assert.NotNull(world.CharacterThwart);
        Assert.Equal(
            world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId,
            world.CharacterThwart!.Scheme);
    }

    [Rule("rr:target.3.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void AReachableBranchWithAnIllegalThwartTargetRefusesBeforeMutation()
    {
        // The form change can select either branch after initiation. Only the
        // currently active branch ignores Crisis, so the ordinary alternate
        // branch makes the whole sequence unsafe to begin. Refusal occurs
        // while the identity is still in hero form.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alter-ego" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AStepThatCannotChangeFormDoesNotOpenTheOtherFormBranch()
    {
        // Drawing changes the board but cannot change identity form. The
        // alter-ego branch is therefore unreachable, so its ordinary thwart
        // target under Crisis cannot hide the valid hero branch.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "draw": { "player": "you", "count": 1 } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        ResolveAction(game, source!);

        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:target.3.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void AChoiceWrappedFormChangeOpensTheReachableFormBranch()
    {
        // Either option may be selected after initiation. Because one can
        // change to alter-ego form, the later ordinary thwart branch is
        // reachable and its Crisis-prohibited target must be refused before
        // the choice can suspend or mutate the identity.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "choose": { "options": [
                { "changeForm": { "player": "you", "to": "alter-ego" } },
                { "seq": [] }
              ] } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AnInactiveFormChangeDoesNotOpenTheLaterFormBranch()
    {
        // The first form predicate is stable and selects its draw branch. Its
        // inactive change-form branch cannot make the later alter-ego branch
        // reachable, so only the Crisis-ignoring hero thwart is required.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "changeForm": {
                  "player": "you", "to": "hero"
                } }
              } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        ResolveAction(game, source!);

        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ANoOpFormChangeDoesNotOpenTheLaterFormBranch()
    {
        // Changing to the form already shown is a no-op. It cannot make the
        // later alter-ego branch reachable or let that branch's prohibited
        // target hide the valid hero thwart.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "changeForm": { "player": "you", "to": "hero" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        ResolveAction(game, source!);

        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ADeterministicFormRestorationClosesTheEarlierFormBranch()
    {
        // A form change “changes from their current form to their other form.”
        // The second change therefore returns the identity to hero form before
        // the thwart. The earlier alter-ego state cannot make the prohibited
        // branch reachable when the target is checked at initiation.
        var runner = Runner(
            "01006",
            """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alter-ego" } },
              { "changeForm": { "player": "you", "to": "hero" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                source = InPlay(board, "01006", DeckType.SupportsArea);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            runner);

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:target.6")]
    [Fact]
    public void ASearchNeedsAnAreaRatherThanAMatchingCard()
    {
        // “An ability with a search effect requires only a searchable game
        // area.” No card matches the invented id, but the encounter deck is a
        // searchable area, so the action remains initiable.
        var runner = Runner(
            "01006",
            """{ "search": { "in": [ { "encounterDeck": 1 } ], "for": "missing-card" } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, "01006", DeckType.SupportsArea),
            runner);

        Resolution resolved = ResolveAction(game, source!);

        Assert.Contains(
            resolved.Information,
            signal => signal.Kind == InformationKind.Search);
    }

    [Fact]
    public void AShortCircuitedConcealedQueryDoesNotRecordASearch()
    {
        var runner = Runner(
            "01006",
            """
            { "if": {
              "test": { "and": [
                { "exists": { "query": "minions" } },
                { "exists": { "cardsIn": { "area": "yourDeck", "kind": "Upgrade" } } }
              ] },
              "then": { "placeCounters": { "card": "this", "counter": "test", "count": 1 } },
              "else": { "placeCounters": { "card": "this", "counter": "test", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, "01006", DeckType.SupportsArea),
            runner);

        Resolution resolved = ResolveAction(game, source!);

        Assert.DoesNotContain(
            resolved.Information,
            signal => signal.Kind == InformationKind.Search);
    }

    private static AbilityRunner Runner(string card, string effect) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              "effect": {{effect}}
            } ] } ] }
            """));

    private static void AssertRevealedIdentityAssociation(
        string source,
        string identity,
        string unrelated,
        string hero)
    {
        string title = Cards.Title(identity);
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{source}}", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
              "effect": { "giveStatus": { "card": { "titled": "{{title}}" }, "status": "tough" } }
            } ] } ] }
            """));
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero]), Cards),
            [Setup.Hero(hero).Name],
            12345);
        world.Seats[0].IdentityCard.TurnTo(identity);
        var abilitySource = world.CreateCard(
            source,
            world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0)));
        var sameTitledAlly = InPlay(world, unrelated, DeckType.AlliesArea);

        runner.WhenRevealed(world, abilitySource, 0);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.False(Statuses.Has(world, sameTitledAlly, Statuses.Tough));
    }

    private static Card InPlay(World world, string card, DeckType area) =>
        world.CreateCard(
            card,
            world.AreaOf(area, PlayArea.Of(0), cardOwner: 0));

    private static Resolution ResolveAction(Game game, Card source)
    {
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source.ObjectId);
        return game.Resolve(Decision.Take(action.Id));
    }

    private static (Game Game, World World) Playing(
        Action<World> prepare,
        AbilityRunner runner,
        string hero = "spider_man")
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero]), Cards),
            [Setup.Hero(hero).Name],
            12345);
        prepare(world);
        var game = Game.Begin(world, Cards, runner);
        while (game.Pending is { } pending
            && pending.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }
        return (game, world);
    }
}
