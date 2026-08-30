using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:for-each.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ForEachDamageIsFullyTracedBeforeARepeatedFrameCanMutate()
    {
        // The first frame's two points are one combined for-each instance and
        // eliminate Spider-Man at two remaining hit points. That changes the
        // first player before the next frame and exposes the unsupported
        // branch. Initiation must trace both points and refuse before the
        // exhaust cost or damage can mutate the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 }
              } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MutableForEachAmountsFailClosedDuringRepeatedTrace()
    {
        // Each chosen instance would read damageOn again: three damage first,
        // then six, not the same three copied twice. The trace cannot yet
        // evaluate that expression against its intermediate board, so it must
        // refuse before an earlier frame can eliminate the first player and
        // expose the unsupported branch for the next one.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": { "chooseCard": {
                "from": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "titled": "Spider-Man" },
                  "amount": { "damageOn": { "titled": "Spider-Man" } }
                } }
              } } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(3);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("between traced iterations", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(3, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MultiTargetForEachIsNotOfferedBeforeItsCost()
    {
        // Without “choose,” for-each applies to one target. Two matching
        // minions make this authored selector unsupported. The exact-one
        // boundary is an initiation check so the action cannot first exhaust
        // its source and only then discover the problem.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01121", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.1")]
    [Fact]
    public void AStableVillainTargetSurvivesAnExhaustCost()
    {
        // Exhausting this support cannot change which single card occupies the
        // villain area. The conservative mutation boundary therefore keeps
        // this supported target shape available.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "villain" }, "amount": 1 }
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AForEachTargetCannotAppearAfterPaymentOrAnEarlierStep()
    {
        // The action starts with one minion, but the preceding step would put
        // a second into play. Because the no-choice target is not yet a
        // persisted binding, initiation refuses this changing-cardinality
        // shape before either exhausting Aunt May or moving Sandman.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                } },
                "where": "engagedWithYou"
              } },
              { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? sandman = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                sandman = board.CreateCard(
                    "01102", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, sandman!.Area.Type);
        Assert.Equal(0, world!.Cards[source.ObjectId].Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeForEachCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "forEach": { "count": -1, "effect": { "draw": { "player": "you", "count": 1 } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeEachTimeCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachTime": {
              "effect": { "discardTop": { "from": "encounterDeck", "count": -1 } },
              "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MutableEachTimeCountAfterAnEarlierEffectIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "heal": { "cards": "you", "amount": 1 } },
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck",
                  "count": { "add": [ -1, { "damageOn": "you" } ] }
                } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "draw": { "player": "you", "count": 1 } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("each-time count after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void DynamicNegativeForEachCountIsRejectedBeforeALabelledPowerCost()
    {
        // A changing count still has a definite value at initiation. It must
        // be validated before suspension analysis can schedule the attack and
        // pay its cost; mutability only determines whether zero can prune the
        // body.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "add": [ -1, { "damageOn": "you" } ] },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void BindingDependentForEachCountCannotPruneItsBodyBeforePayment()
    {
        // Before chooseCard binds `chosen`, the count appears to be zero. The
        // chosen card makes it one, so the unsupported labelled continuation
        // must be found before the exhaust cost rather than hidden as a
        // zero-count body during initiation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "minions" },
              "effect": { "forEach": {
                "count": { "count": "chosen" },
                "effect": { "seq": [
                  { "choose": { "options": [
                    { "draw": { "player": "you", "count": 1 } },
                    { "seq": [] }
                  ] } },
                  { "attack": {
                    "target": { "query": "villain" },
                    "effect": { "enemyAttacks": {
                      "enemies": { "query": "villain" }
                    } }
                  } }
                ] }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("for-each count after state may change", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void CurrentDynamicZeroHasNoChoiceForOtherwisePreflight()
    {
        // No payment, prior step, or binding can change damageOn before this
        // predecessor executes. Its current zero count makes the nested choice
        // unreachable, so otherwise may resolve the draw.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "otherwise": {
              "effect": { "forEach": {
                "count": { "damageOn": "you" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } },
              "otherwise": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveStablePowerBranchDoesNotValidateItsForEachCount()
    {
        // Form cannot change between offering and paying this action. Only the
        // hero branch can execute, so an alter-ego-only count must not reject
        // the labelled attack from an unreachable branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                "else": { "forEach": {
                  "count": { "add": [ -1, { "damageOn": "you" } ] },
                  "effect": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:form-change-form.2")]
    [Fact]
    public void EarlierPowerStepCanExposeASuspendingBranch()
    {
        // The first step changes the fact tested by the second. Suspension
        // preflight must therefore inspect both reachable branches and refuse
        // the enemy activation before the labelled attack is scheduled.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alterEgo" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void PowerTargetBindingCannotHideAForEachContinuation()
    {
        // The villain becomes `chosen` when the labelled attack is scheduled.
        // Its existing damage makes this count one, so the nested threat
        // continuation must be refused while the action is still only offered.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "damageOn": "chosen" },
                "effect": { "placeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PowerAmountBranchIsRefusedBeforeLegalPracticeDiscards()
    {
        // The selected card count binds powerAmount. Every branch that binding
        // can open must be checked before Legal Practice discards a hand card.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "if": {
                  "test": { "atLeast": {
                    "value": { "powerAmount": "cardsDiscarded" },
                    "count": 1
                  } },
                  "then": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } },
                  "else": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        World? world = null;
        Card? source = null;
        int handBefore = -1;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01151", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                handBefore = board.Seats[0].Hand.Cards.Count;
            },
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(handBefore, world!.Seats[0].Hand.Cards.Count);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void HarmlessEarlierPowerStepDoesNotSwitchAFormBranch()
    {
        // Drawing changes state but cannot change form. The hero-only branch
        // therefore remains the only executable branch of this labelled power.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveEarlierBranchDoesNotChangePowerForm()
    {
        // The first condition's alter-ego branch cannot execute while the hero
        // branch merely draws. Its unreachable form change must not make the
        // later hero-only condition appear switchable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "draw": { "player": "you", "count": 1 } },
                  "else": { "changeForm": {
                    "player": "you", "to": "hero"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ZeroForEachBodyDoesNotChangePowerForm()
    {
        // A zero-count form change never executes. It cannot make the later
        // form condition switch or expose its suspending alter-ego branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 0,
                  "effect": { "changeForm": {
                    "player": "you", "to": "alterEgo"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpFormChangeDoesNotSwitchALaterPowerBranch()
    {
        // Changing to the form already showing does nothing. The later hero
        // condition therefore cannot switch to its suspending alter-ego branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AnotherPlayersFormChangeDoesNotSwitchYourPowerBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alterEgo"
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void StableFirstPlayerNoOpDoesNotSwitchALaterPowerBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "changeForm": {
                  "player": "firstPlayer", "to": "hero"
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalPowerDamageCanRebindAFormTestedFirstPlayer()
    {
        // Eliminating the hero holding the first-player token moves that
        // selector to the alter-ego player. The newly reachable activation is
        // refused before the lethal damage mutates the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 99 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DeterministicFormRestorationKeepsTheLaterPowerBranchStable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alterEgo" } },
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ChosenPlayerRestorationKeepsEveryOtherFormOutcomeReachable()
    {
        // A choice must be made among “eligible game elements.” Restoring the
        // chosen identity to hero form changes only that identity; another
        // identity that a prior option changed can remain in alter-ego form.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alterEgo"
                } },
                { "changeForm": { "player": "you", "to": "alterEgo" } }
              ] } },
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "changeForm": {
                  "player": "chosenPlayer", "to": "hero"
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
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
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void EveryLegalEachPlayerOrderContributesFormReachability()
    {
        // “The first player decides the order” for an each-player effect. If
        // player one resolves first, both identities can change form; the
        // later target check must include that legal ordering.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "changeForm": {
                  "player": "you", "to": "alterEgo"
                } },
                "else": { "seq": [] }
              } } } },
              { "if": {
                "test": { "inForm": {
                  "player": "you", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
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
        var (_, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                board.Seats[1].IdentityCard.TurnTo("01010a");
                source = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 1), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void CumulativePowerDamageCanRebindTheFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalDamageToAnotherPlayerDoesNotRebindFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Carol Danvers" }, "amount": 99
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DamagePlacedEarlierCanBeMovedToEliminateFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.2")]
    [Rule("rr:tough.2")]
    [Fact]
    public void CombinedForEachDamageUsesOneToughInPowerPreflight()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 2,
                  "effect": { "dealDamage": { "cards": "you", "amount": 1 } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NonToughStatusDoesNotEnterPowerDamageSimulation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void MutablePowerAmountAfterDamageFailsBeforeMutation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": {
                  "cards": "you", "amount": { "damageOn": "you" }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(4);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("mutable power amount", refused.Message);
        Assert.Equal(4, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:and")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UnsupportedPowerAndIsRejectedWithoutReplayingDamage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "and": [
                  { "dealDamage": { "cards": "you", "amount": 1 } },
                  { "draw": { "player": "you", "count": 1 } }
                ] }
              ] }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:and")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void SingletonPowerAndIsSimulatedOnce()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "and": [
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ToughOnMovedDamageSourcePreventsPhantomInventory()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(
                    board, board.TheCardIn(DeckType.VillainArea)!, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpMoveDoesNotMakeALaterAmountLookMutable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" },
                  "amount": { "damageOn": "you" }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(2);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void BranchMergeKeepsLiveDamageWhenAnotherBranchHeals()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "titleInPlay": "Nonexistent" },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void BranchMergeDoesNotInventToughOnTheUntakenPath()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "titleInPlay": "Aunt May" },
                  "then": { "giveStatus": {
                    "card": "you", "status": "tough"
                  } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Rule("rr:heal")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void GuaranteedBranchDoesNotKeepItsPreBranchDamageState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:and")]
    [Rule("rr:heal")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void SingletonAndDoesNotKeepItsPreChildDamageState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "and": [
                  { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                ] },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UncertainOtherwisePreservesThePathWithoutItsFallback()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "otherwise": {
                  "effect": { "draw": { "player": "you", "count": 1 } },
                  "otherwise": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:exhausted")]
    [Rule("rr:otherwise")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ExhaustedPredecessorMakesItsOtherwiseFallbackGuaranteed()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "exhaust": "this" },
                { "otherwise": {
                  "effect": { "exhaust": "this" },
                  "otherwise": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ForcedReplacementCanLeaveALabelledPowerMoveSourceEmpty()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RemovedExhaustTargetCannotSatisfyThen()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "then": {
                  "effect": { "exhaust": { "titled": "Hydra Mercenary" } },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AlternativeDamageProtectionPathsRemainCorrelated()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "titleInPlay": "Nonexistent" },
                  "then": { "seq": [
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 5
                    } },
                    { "giveStatus": {
                      "card": { "query": "villain" }, "status": "tough"
                    } }
                  ] },
                  "else": { "seq": [] }
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cannot.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ProhibitedLabelledMoveLeavesDamageForALaterMove()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DependentOutcomeStaysCorrelatedWithItsConditionalState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "if": {
                    "test": { "titleInPlay": "Nonexistent" },
                    "then": { "giveStatus": {
                      "card": { "query": "villain" }, "status": "tough"
                    } },
                    "else": { "seq": [] }
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cannot.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedProhibitionMakesLaterLabelledDamageLegal()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Legions of Hydra" } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:heal")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpHealCannotSatisfyThenAfterAnEarlierStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedDiscardOfAnAlreadyDiscardedCardResolvesNone()
    {
        // The second discard cannot affect Helicarrier after the first one has
        // moved it out of play. It therefore does not satisfy "then," and the
        // dependent villain damage never becomes available to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Helicarrier" } },
                { "then": {
                  "effect": { "discard": { "titled": "Helicarrier" } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        Card? helicarrier = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                helicarrier = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, helicarrier!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpFormChangeCannotSatisfyThenAfterAnEarlierStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "changeForm": { "player": "you", "to": "hero" } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

}
