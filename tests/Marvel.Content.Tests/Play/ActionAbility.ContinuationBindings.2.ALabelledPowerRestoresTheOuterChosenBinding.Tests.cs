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
public sealed class ActionAbilityContinuationBindingsALabelledPowerRestoresTheOuterChosenBindingTests
{
    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ALabelledPowerRestoresTheOuterChosenBindingBeforeContinuing()
    {
        // The attack's villain target is not the identity selected by the
        // outer ability. The power uses its target while resolving, then the
        // unresolved outer sentence again refers to the selected player.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long damage = villain.Damage;
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(world, source!, 0, choice.Index, Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.CharacterAttacks);
        runner.ResolveCardAttack(world, attack.CharacterAttack!, attack.OccurrenceOf(world, CardCatalogData), []);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01181", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            var legions = board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
            legions.PlaceTokens("k_threat", 1);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:then")]
    [Fact]
    public void ChosenTargetCanMakeADependentContinuationReachable()
    {
        // "Then" resolves only after its predecessor resolves in full. Binding
        // the threatened scheme makes that predecessor reachable and mutable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "chooseCard": { "from": { "query": "thwartableSchemes" }, "effect": { "then": { "effect": { "removeThreat": { "scheme": "chosen", "amount": 1 } }, "then": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        long threat = -1;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            threat = scheme.Tokens.GetValueOrDefault("k_threat");
        }, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "discard": "this" }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void EachPlayerDrawDoesNotDestabilizeEveryPlayersHeroForm()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ACardTitleContainingChosenIsNotAChoiceBinding()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesDefeatFromEarlierFrames()
    {
        // Damage tokens equal to remaining hit points defeat a minion. A later
        // player's title test therefore sees a different in-play board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Hydra Mercenary" }, "then": { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 3 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? minion = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }
}
