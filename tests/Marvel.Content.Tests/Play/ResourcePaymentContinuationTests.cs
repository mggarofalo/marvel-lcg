using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ResourcePaymentContinuationTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void EachGeneratorFinishesItsCostBeforeTheNextGeneratorAndPurchasedEffect()
    {
        // "Pay the cost(s)." Synthetic abilities exercise two successive
        // suspensions without importing expansion content into the Core runtime.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"spend":"YY"},{"exhaust":"this"}]},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},
                "effect":{"generate":"Y"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        Card[] generators = [];
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            generators = Enumerable.Range(0, 2).Select(_ => board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0))).ToArray();
            foreach (var generator in generators) generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        int[] paying = [.. generators.Select(card => card.ObjectId)];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(4, generators[0].Damage);
        Assert.Equal(2, generators[1].Damage);
        Assert.True(source!.Ready);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, generators[0].Area.Type);
        Assert.Equal(4, generators[1].Damage);
        Assert.True(source.Ready);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.All(generators, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.False(source.Ready);
        Assert.Equal(1, source.Tokens["c_test"]);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
    }
}
