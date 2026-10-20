using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CountedAttackIncarnationTests
{
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void ReenteredRecipientRaisesAtUnimplementedCompletionBoundaryBeforeDamage()
    {
        // A departing card "is considered to be a new copy of the card".
        // Synthetic legal interrupt transcript isolates the unadmitted capability
        // boundary. The interpreter must not mistake the reentered copy for the
        // chosen enemy. Completing absent-recipient attack facts remains required.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01003","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"query":"attackableEnemies"},"count":2,"effect":{
                  "dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}
                }}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","actor":"hero"},
                "effect":{"choose":{"options":[
                  {"seq":[
                    {"discard":{"titled":"Sandman"}},
                    {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Sandman"}},
                                   "where":"engagedWithYou"}}
                  ]},
                  {"draw":{"player":"firstPlayer","count":1}}
                ]}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01003", board.Seats[0].Hand);
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int incarnation = minion!.Incarnation;
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id,
            [villain.ObjectId, minion.ObjectId], []));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id)));
        Assert.Contains("changed incarnation before damage", error.Message);
        Assert.True(minion.Incarnation > incarnation);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, villain.Damage);
    }
}
