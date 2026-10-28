using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackExcessIncarnationControlTests
{
    [Fact]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:triggering-condition.1")]
    public void LaterNonattackDefeatOfReenteredCardCannotSatisfyOriginalAttack()
    {
        // rr:leaves-play.1: a card returning is a new copy. The interrupt
        // defeats that new copy with nonattack damage, not the original recipient.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01003","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"query":"minions"},"count":1,"effect":{
                  "dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":5,"simultaneous":1}
                }}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","actor":"hero"},
                "effect":{"seq":[{"discard":{"titled":"Whiplash"}},
                  {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Whiplash"}},"where":"engagedWithYou"}},
                  {"dealDamage":{"cards":{"titled":"Whiplash"},"amount":20}}]}
              }]},
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
                "effect":{"placeCounters":{"card":"you","counter":"attack_completed","count":1}}
              }]},
              {"card":"12030","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
                "when":{"wasDefeated":"trigger.target"},
                "effect":{"heal":{"card":"you","amount":{"attackExcessDamage":"trigger.target"}}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? enemy = null;
        Card? response = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01003", board.Seats[0].Hand);
            response = board.CreateCard("12030", board.Seats[0].Hand);
            enemy = board.CreateCard("01172", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(4);
            InPlay(board, "01092");
            InPlay(board, "01007");
        }, hero: true, abilities: runner);
        int incarnation = enemy!.Incarnation;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId).Id));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, [enemy.ObjectId], []));
        Assert.True(enemy.Incarnation > incarnation);
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_attack_completed"));
        Assert.True(game.Pending is null || game.Pending.Affordances.All(option => option.AnchorId != response!.ObjectId));
        Assert.Equal(DeckType.HandsArea, response!.Area.Type);
    }
}
