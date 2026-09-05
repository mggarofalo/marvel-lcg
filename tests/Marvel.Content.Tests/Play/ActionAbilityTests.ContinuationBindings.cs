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
    [Rule("rr:then")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANestedDependentChoiceIsRejectedBeforeItsCost()
    {
        // The outer answer does not determine whether the complete predecessor
        // resolved: its nested choice and any later siblings still have to run.
        // Until that aggregate outcome is modelled, fail before paying a cost.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } }
              } },
              "then": { "draw": {
                "player": "chosenPlayer", "count": 1
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SiblingChoicesBeforeDependentTextAreRejectedBeforeTheirCost()
    {
        // The first answer cannot classify the complete predecessor while a
        // sibling choice and a later effect remain unresolved. Fail closed
        // before exhaustion rather than recording the first leaf's outcome.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "seq": [
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "heal": { "card": "you", "amount": 1 } }
              ] },
              "then": { "draw": { "player": "chosenPlayer", "count": 1 } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEmptyEachPlayerOutcomeInvalidatesAChosenPlayerDraw()
    {
        // Player 0 has a hero target and player 1 does not. If player 1's frame
        // resolves last, the outer draw has no chosen player; unlike a card
        // prompt, the order decision cannot filter out that reachable outcome.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "yourHero",
                "effect": { "seq": [] }
              } } } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:target.2")]
    [Fact]
    public void AMixedBindingPromptKeepsTheDrawablePlayerPath()
    {
        // The selector includes an identity and the villain. At least one path
        // supplies the player required by the later draw, so the action is
        // legal and the scenario-owned character is filtered from its prompt.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "characters" },
                "effect": { "seq": [] }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEarlyForEachPromptAccountsForTheLaterIteration()
    {
        // The first iteration may choose player 0 even though only player 1 can
        // satisfy the outer selector: the second iteration asks again and its
        // answer is the binding that reaches the continuation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": {
                "count": 2,
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "seq": [] }
                } }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEachTimePromptKeepsTheOuterFilterWhenLaterBodiesAreSkipped()
    {
        // The first discarded card matches and asks for a character; the next
        // card is not Kree, so no later prompt will replace that binding. The
        // first prompt must therefore apply the outer chosen-player draw and
        // exclude the scenario-owned villain before the cost is exposed.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """
            { "seq": [
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck", "count": 2
                } },
                "when": { "cardSet": {
                  "card": "that", "set": "kree_fanatic"
                } },
                "then": { "chooseCard": {
                  "from": { "query": "characters" },
                  "effect": { "seq": [] }
                } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            eventName: Steps.CardRevealed);
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }
        world.CreateCard(AuthoredCards.ImTough, deck);
        world.CreateCard("90001", deck);
        var source = world.CreateCard(
            AuthoredCards.AuntMay, world.AreaOf(DeckType.RevealingArea));
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = runner.WhenRevealed(world, source, 0).ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;

        Assert.Contains(
            prompt.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            prompt.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:activation.8")]
    [Fact]
    public void AChosenPlayerBindingSurvivesAnActivationContinuation()
    {
        // The selection is part of the unresolved ability when an activation
        // “resolves after the current … ability.” Resuming that same ability
        // must retain which identity the player selected.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "identities" },
              "effect": { "seq": [
                { "enemyAttacks": { "enemies": { "query": "villain" } } },
                { "draw": { "player": "chosenPlayer", "count": 1 } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(
            world, source!, 0, choice.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true,
            attack.ActivationId, Made: false));

        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ALabelledPowerRestoresTheOuterChosenBindingBeforeContinuing()
    {
        // The attack's villain target is not the identity selected by the
        // outer ability. The power uses its target while resolving, then the
        // unresolved outer sentence again refers to the selected player.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "seq": [
                  { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  { "draw": { "player": "chosenPlayer", "count": 1 } }
                ] }
              } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long damage = villain.Damage;
        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);

        runner.Chose(
            world, source!, 0, choice.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(
            world.Agenda.Outstanding,
            step => step.What == Steps.CharacterAttacks);
        runner.ResolveCardAttack(
            world, attack.CharacterAttack!, attack.OccurrenceOf(world, Cards), []);

        Assert.Equal(damage + 1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void EveryEachPlayerFrameContributesLaterBindingCandidates()
    {
        // The first player decides the frame order. The final frame can bind
        // either player's engaged minion, so the later attack must account for
        // Madame Hydra's “cannot take damage” prohibition before paying costs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                var legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEmptyFinalEachPlayerFrameRemainsAReachableBindingOutcome()
    {
        // Every player frame restores the binding from before “each player.”
        // If the player with no minion resolves last, no target is persisted
        // for the later attack and the cost must not be paid first.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void NestedChoiceCandidatesReplaceTheOuterChosenPlayerDuringValidation()
    {
        // Candidate validation must read the identity currently being offered,
        // not the hero chosen by the outer question. The alter-ego candidate
        // reaches an attack targeting that identity and is therefore illegal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "identities" },
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "chosenPlayer", "form": "hero"
                  } },
                  "then": { "draw": {
                    "player": "chosenPlayer", "count": 1
                  } },
                  "else": { "attack": {
                    "target": "chosen",
                    "effect": { "dealAttackDamage": {
                      "cards": "chosen", "amount": 1
                    } }
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:then")]
    [Fact]
    public void ChosenTargetCanMakeADependentContinuationReachable()
    {
        // "Then" resolves only after its predecessor resolves in full. Binding
        // the threatened scheme makes that predecessor reachable and mutable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "thwartableSchemes" }, "effect": { "then": { "effect": { "removeThreat": { "scheme": "chosen", "amount": 1 } }, "then": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        long threat = -1;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
                threat = scheme.Tokens.GetValueOrDefault("k_threat");
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(threat, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesMutationsFromEarlierFrames()
    {
        // The first player can remove the source before another player's frame
        // tests whether its title remains in play.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "discard": "this" }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void EachPlayerDrawDoesNotDestabilizeEveryPlayersHeroForm()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ACardTitleContainingChosenIsNotAChoiceBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "if": {
                "test": { "titleInPlay": "Kang's Chosen" },
                "then": { "attack": {
                  "target": "chosen",
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
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

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesDefeatFromEarlierFrames()
    {
        // Damage tokens equal to remaining hit points defeat a minion. A later
        // player's title test therefore sees a different in-play board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Hydra Mercenary" }, "then": { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 3 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightUnionsEveryPlayersActiveMutationPath()
    {
        // The first player chooses the order. A hero can discard the source
        // before an alter-ego's different branch tests whether it remains.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisDoesNotReadAnUnansweredChosenPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } },
                  "then": { "draw": { "player": "chosenPlayer", "count": 1 } },
                  "else": { "draw": { "player": "chosenPlayer", "count": 1 } }
                } }
              } },
              "else": { "draw": { "player": "you", "count": 1 } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form")]
    [Fact]
    public void RepeatedMutationAnalysisIncludesLabelledPowerEffects()
    {
        // The first player can order another hero first. That hero's attack
        // flips the first player before the first player's own frame resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "attack": {
                "target": { "query": "villain" },
                "effect": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisReachesAFixedPoint()
    {
        // One frame removes the title, making a form change reachable; that
        // form change makes the unsupported third-frame branch reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisIsBoundedByRemainingFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
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

    [Fact]
    public void RemovingThreatDoesNotChangeWhichTitlesAreInPlay()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LethalDamageCanMoveTheFirstPlayerBindingBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 99 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Fact]
    public void VillainDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void NonlethalIdentityDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void MovingLethalDamageCanMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "moveDamage": { "from": { "query": "villain" }, "to": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RemovingLastThreatCanDefeatATitleBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeThreatRemovalCanDefeatATitleBetweenFrames()
    {
        // All threat removed during one player's frame counts when deciding
        // whether a later player's title-dependent branch can become active.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "seq": [
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } },
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeDamageCanEliminateAPlayerBetweenFrames()
    {
        // Individually nonlethal damage instances combine before the next
        // player's frame and can move the first-player binding.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
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

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void OrderedMutationCanExposeLethalDamageWithinARepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "dealDamage": { "cards": "you", "amount": 2 } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
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

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageToFixedTargetsAccumulatesAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "query": "identities" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
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
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Theory]
    [InlineData("\"yourHero\"")]
    [InlineData("{ \"query\": \"charactersYouControl\" }")]
    [InlineData("{ \"withTrait\": { \"cards\": \"yourHero\", \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"withTrait\": { \"cards\": { \"query\": \"charactersYouControl\" }, \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"maxBy\": { \"of\": { \"query\": \"charactersYouControl\" }, \"by\": \"attack\" } }")]
    public void PlayerRelativeDamageTargetsApplyOnlyInTheirPlayersFrame(
        string targets)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": { "cards": {{targets}}, "amount": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MovedDamageCannotBeReusedAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageBeforeAMoveReplenishesItsRepeatedSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 2 } },
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "moveDamage": {
                    "from": { "titled": "Hydra Mercenary" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 2
                  } },
                  "else": { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 1
                  } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void DamageAfterAMoveIsAvailableOnlyToLaterRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageMovedAwayDoesNotAccumulateAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Spider-Man" },
                  "to": { "query": "villain" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageHealedAfterEachMoveDoesNotAccumulateAcrossFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(3);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DistinctPlayersCanSupplyDamageAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": "you",
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
                board.Seats[1].IdentityCard.TakeDamage(1);
                board.Seats[2].IdentityCard.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk", "iron_man"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[2].IdentityCard.Damage);
    }

    [Fact]
    public void PlayerRelativeDamageRebindsBetweenFixedTargetFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(7);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughPreventsLethalDamageInAnEarlierRepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

}
