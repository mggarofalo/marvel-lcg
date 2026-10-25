using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousCardAttackTests
{
    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Fact]
    public void FirstDefeatChoiceObservesEveryRecipientsDamageAlreadyPlaced()
    {
        // FAQ03006: damage is dealt simultaneously. RRdamage7 checks defeat
        // after placement; When Defeated resolves before the card leaves play.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01004","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"titled":"Hydra Soldier"},"effect":{
                  "dealAttackDamage":{"cards":{"query":"minions"},"amount":4,"simultaneous":1}
                }}}
              }]},
              {"card":"01182","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
                "effect":{"choose":{"options":[
                  {"placeCounters":{"card":{"titled":"Spider-Man"},"counter":"seen","count":{"damageOn":{"titled":"Sandman"}}}},
                  {"draw":{"player":"firstPlayer","count":1}}
                ]}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? payment = null;
        Card? soldier = null;
        Card? sandman = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            soldier = board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            sandman = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [payment!.ObjectId];
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(4, soldier!.Damage);
        Assert.Equal(4, sandman!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, sandman.Area.Type);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(4, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_seen"));
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, sandman.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualAttackPublishesEachRecipientAndOnlyOneWholeAttack(bool tough)
    {
        int expectedDamage = tough ? 0 : 1;
        int expectedResponses = tough ? 4 : 5;
        // RR2.1: "a single attack"; RR5: "attacked each of those enemies".
        // Vendored ruling:f32c53356db718a6 gives one Pitchback and per-enemy Jarnbjorn.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01004","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"query":"villain"},"effect":{
                  "dealAttackDamage":{"cards":{"query":"enemies"},"amount":1,"simultaneous":1}
                }}}
              }]},
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
                "effect":{"placeCounters":{"card":"trigger.target","counter":"recipient","count":1}}
              }]},
              {"card":"01093","abilities":[{
                "trigger":{"event":"WhenAttackEnds","alsoHappened":"WhenDamageDealt","timing":"ForcedResponse","target":"enemy"},
                "effect":{"placeCounters":{"card":"trigger.target","counter":"damaged","count":1}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"whole","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? payment = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (tough) Statuses.Give(board, minion, Statuses.Tough);
            InPlay(board, "01007");
            InPlay(board, "01092");
            InPlay(board, "01093");
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [payment!.ObjectId];
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(expectedResponses, game.Pending.Affordances.Count);
        Assert.Equal(2, world.Agenda.Occurrence!.Attacks.Count);
        Assert.Equal(expectedDamage,
            Assert.Single(world.Agenda.Occurrence.Attacks, item => item.Card == minion!.ObjectId).DamageTaken);
        for (int remaining = expectedResponses; remaining >= 2; remaining--)
        {
            Assert.Equal(Question.Order, game.Pending!.Asking);
            Assert.Equal(remaining, game.Pending.Affordances.Count);
            game.Resolve(Decision.Take(game.Pending.Affordances[1].Id));
        }
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(1, villain.Damage);
        Assert.Equal(expectedDamage, minion!.Damage);
        Assert.Equal(1, villain.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, minion.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, villain.Tokens.GetValueOrDefault("c_damaged"));
        Assert.Equal(expectedDamage, minion.Tokens.GetValueOrDefault("c_damaged"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_whole"));
    }
}
