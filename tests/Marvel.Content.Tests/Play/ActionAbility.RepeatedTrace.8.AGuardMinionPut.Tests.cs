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
public sealed class ActionAbilityRepeatedTraceAGuardMinionPutTests
{
    [Rule("rr:guard.1")]
    [Fact]
    public void AGuardMinionPutIntoPlayImmediatelyProtectsTheVillain()
    {
        // Guard means "the engaged player cannot attack any villain."
        // Putting Hydra Mercenary into play engaged with the resolving hero
        // therefore removes Rhino from attackableEnemies before damage lands.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "if": {
                    "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
                    "then": { "putIntoPlay": {
                      "card": { "cardsIn": {
                        "areas": [ "encounterDiscardPile" ],
                        "title": "Hydra Mercenary"
                      } },
                      "where": "engagedWithYou"
                    } }
                  } },
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }
}
