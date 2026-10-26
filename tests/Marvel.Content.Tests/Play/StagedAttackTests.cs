using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic body on the physical Melee event isolates the engine prerequisite.
public sealed class StagedAttackTests
{
    [Rule("rr:ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondEnemyIsChosenAfterFirstDamageSettles(bool guard)
    {
        // rr:ability.4: "resolve the ability one sentence at a time".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
                "attack":{"target":"chosen","effect":{"seq":[
                  {"dealAttackDamage":{"cards":"chosen","amount":3}},
                  {"attackAnotherEnemy":{"amount":3}}
                ]}}}}}
            }]}]}
            """));
        Card? source = null;
        Card? first = null;
        Card? other = null;
        Card? payment = null;
        Card? paymentTwo = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("05030", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            paymentTwo = board.CreateCard("01089", board.Seats[0].Hand);
            first = board.CreateCard(guard ? "01101" : "01130",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            other = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [payment!.ObjectId, paymentTwo!.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paying);
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == first!.ObjectId).Id));
        Assert.NotNull(game.Pending);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.AnchorId == first!.ObjectId);
        if (guard)
        {
            Assert.Equal(DeckType.EncounterDiscardPile, first!.Area.Type);
            Assert.Contains(game.Pending.Affordances, option => option.AnchorId == world.TheCardIn(DeckType.VillainArea)!.ObjectId);
        }
        else Assert.Equal(3, first!.Damage);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            option => option.AnchorId == other!.ObjectId).Id));
        Assert.Equal(3, other!.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }
}
