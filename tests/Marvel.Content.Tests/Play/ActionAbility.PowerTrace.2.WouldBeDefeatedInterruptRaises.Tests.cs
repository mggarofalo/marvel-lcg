using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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
public sealed class ActionAbilityPowerTraceWouldBeDefeatedInterruptRaisesTests
{
    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void WouldBeDefeatedInterruptRaisesBeforeALabelledPowerMutates()
    {
        // Step 6 resolves "would be defeated" interrupts after damage is
        // placed and before defeat. Biomechanical Upgrades heals its host and
        // discards itself, invalidating the imminent defeat under rr:would.1.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? upgrade = null;
        Card? villain = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            upgrade = board.CreateCard("01185", board.AreaOf(DeckType.UpgradesArea, guard.Area.PlayArea, guard.ObjectId));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("step-6 interrupt", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, upgrade!.Area.Host);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UnauthoredPrintedDefeatAbilityRaisesBeforeLabelledCost()
    {
        // Goblin Soldier prints a When Defeated ability that has no authored
        // behavior. The engine raises rather than guessing, and eligibility
        // must do so before either the cost or lethal damage mutates the board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Goblin Soldier" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? soldier = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            soldier = board.CreateCard("02023", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner));
        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompiledHealthProjectionUsesItsSnapshotAndTheProjectedThreat(bool each, bool replacePredicate)
    {
        string grant = each ? """{"grantEach":{"cards":{"query":"minions"},"keyword":"health","amount":3}}""" : """{"grant":{"card":"this","keyword":"health","amount":3}}""";
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},
                "effect":{"attack":{"target":{"query":"villain"},"effect":{"seq":[
                  {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}},
                  {"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":3}},
                  {"dealDamage":{"cards":{"query":"attackableEnemies"},"amount":1}},
                  {"moveDamage":{"from":{"query":"villain"},"to":{"titled":"Spider-Man"},"amount":1}},
                  {"if":{"test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                    "then":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
                    "else":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
                ]}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"timing":"Constant","subject":"this"},
                "effect":{"if":{
                  "test":{"atLeast":{"value":{"tokensOn":{"query":"mainScheme"}},"count":2}},
                  "then":{{{{{{grant}}}}}}
                }}
              }]}
            ]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[1].Effect.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var constant = parsed.Abilities[1] with
        {
            Effect = new AbilityNode("if", new AbilityValue.Map(fields))
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([parsed.Abilities[0], constant], parsed.Authored));
        if (replacePredicate)
        {
            fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["atLeast"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["value"] = new AbilityValue.Number(1), ["count"] = new AbilityValue.Number(1), }), });
        }

        World? world = null;
        Card? source = null;
        Card? guard = null;
        Card? scheme = null;
        // Engine-choice fixture: a synthetic health constant is explicitly
        // installed on a Core minion. Compilation freezes its predicate, while
        // the preview must remove its health bonus after the threat change.
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            board.Seats[0].IdentityCard.TakeDamage(9);
            Assert.Equal(3, Assert.Single(runner.Constant(board, guard)).Amount);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(6, Damage.Health(world!, CardCatalogData, guard!));
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ChangedThreatConditionCannotLeaveStaleConstantHealthInTheTrace()
    {
        // Constant abilities update whenever the game state changes. Infinite
        // Soldier has +3 hit points only while Gene Pool has at least 9 threat;
        // removing one threat makes the following 3 damage lethal.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            pool = board.CreateCard("45071", board.AreaOf(DeckType.SideSchemesArea));
            pool.PlaceTokens("k_threat", 9);
            soldier = board.CreateCard("45069", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }
}
