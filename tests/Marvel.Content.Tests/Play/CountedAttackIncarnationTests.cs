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
    private static readonly string[] Supports = ["01092", "01007", "01093", "01091", "01006"];
    [Rule("rr:leaves-play.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:attack-player-ability-type.5.1")]
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void ReenteredRecipientKeepsOriginalAttackFactsWithoutDamageOrRetaliation(
        bool minionFirst, bool onlyMinion, bool duringDamage)
    {
        // A departing card "is considered to be a new copy of the card".
        // The attacker "is considered to have attacked each of those enemies";
        // only an attacked enemy "still in play" can retaliate. This synthetic
        // interrupt uses engine decisions to discard and reenter that recipient.
        string trigger = duringDamage
            ? "{\"event\":\"WhenDamageWouldBeDealt\",\"timing\":\"ForcedInterrupt\",\"target\":\"minion\"}"
            : "{\"event\":\"WhenAttackInitiated\",\"timing\":\"ForcedInterrupt\",\"actor\":\"hero\"}";
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01003","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"query":"POOL"},"count":2,"effect":{
                  "dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}
                }}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":TRIGGER,
                "effect":{"choose":{"options":[
                  {"seq":[
                    {"discard":{"titled":"Whiplash"}},
                    {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Whiplash"}},
                                   "where":"engagedWithYou"}}
                  ]},
                  {"draw":{"player":"firstPlayer","count":1}}
                ]}}
              }]},
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
                "effect":{"placeCounters":{"card":"you","counter":"attacked","count":1}}
              }]},
              {"card":"01093","abilities":[{
                "trigger":{"event":"WhenAttackEnds","alsoHappened":"WhenDamageDealt","timing":"ForcedResponse","target":"enemy"},
                "effect":{"placeCounters":{"card":"you","counter":"damaged","count":1}}
              }]},
              {"card":"01091","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"whole","count":1}}
              }]},
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
                "effect":{"giveStatus":{"card":"trigger.target","status":"stunned"}}
              }]}
            ]}
            """.Replace("TRIGGER", trigger, StringComparison.Ordinal)
                .Replace("POOL", onlyMinion ? "minions" : "attackableEnemies", StringComparison.Ordinal)));
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01003", board.Seats[0].Hand);
            minion = board.CreateCard("01172", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            foreach (string face in Supports)
                InPlay(board, face);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int incarnation = minion!.Incarnation;
        int[] targets = onlyMinion ? [minion.ObjectId]
            : minionFirst ? [minion.ObjectId, villain.ObjectId] : [villain.ObjectId, minion.ObjectId];
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, targets, []));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(targets, occurrence.Attacks.Select(recipient => recipient.Card));
        var original = Assert.Single(occurrence.Attacks, recipient => recipient.Card == minion.ObjectId);
        Assert.Equal(incarnation, original.Incarnation);
        Assert.Equal(0, original.DamageTaken);
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.True(minion.Incarnation > incarnation);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Stunned));
        int damaged = onlyMinion ? 0 : 1;
        Assert.Equal(damaged, villain.Damage);
        Assert.Equal(!onlyMinion, Statuses.Has(world, villain, Statuses.Stunned));
        var hero = world.Seats[0].IdentityCard;
        Assert.Equal(0, hero.Damage);
        Assert.Equal(targets.Length, hero.Tokens.GetValueOrDefault("c_attacked"));
        Assert.Equal(damaged, hero.Tokens.GetValueOrDefault("c_damaged"));
        Assert.Equal(1, hero.Tokens.GetValueOrDefault("c_whole"));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }
}
