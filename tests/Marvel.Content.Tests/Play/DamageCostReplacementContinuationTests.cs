using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DamageCostReplacementContinuationTests
{
    [Theory]
    [InlineData("dealDamage", false)]
    [InlineData("dealDamage", true)]
    [InlineData("takeDamage", false)]
    [InlineData("takeDamage", true)]
    [Rule("rr:cost.11")]
    [Rule("rr:cost.12")]
    public void DamageCostWaitsForReplacementChoiceBeforeAdvancing(string kind, bool prevent)
    {
        // "If dealing damage is a cost, that cost is considered paid even if
        // some or all of that damage is prevented." Taking damage instead
        // requires that "all of that damage was taken." Synthetic replacement
        // text puts a real player choice between cost initiation and placement.
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"{{{{kind}}}}":{"cards":"you","amount":1}},{"exhaust":"this"}]},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
                "effect":{"choose":{"options":[
                  {"preventDamage":"trigger.target"},
                  {"draw":{"player":"you","count":1}}
                ]}}
              }]}
            ]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            InPlay(board, "01092");
        }, abilities: runner);
        Card hero = world.Seats[0].IdentityCard;
        long damageBefore = hero.Damage;
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(damageBefore, hero.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.True(source!.Ready);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));
        Decision choice = Decision.Take(game.Pending.Affordances[prevent ? 0 : 1].Id);
        if (kind == "takeDamage" && prevent)
        {
            game.Resolve(choice);
            Assert.Equal(Question.TurnOption, game.Pending!.Asking);
            Assert.False(world.Agenda.IsBusy);
            Assert.True(source.Ready);
            Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));
        }
        else
        {
            game.Resolve(choice);
            Assert.Equal(Question.TurnOption, game.Pending!.Asking);
            Assert.False(source.Ready);
            Assert.Equal(1, source.Tokens["c_test"]);
            Assert.False(world.Agenda.IsBusy);
        }
        Assert.Equal(damageBefore + (prevent ? 0 : 1), hero.Damage);
        Assert.Equal(held + (prevent ? 0 : 1), world.Seats[0].Hand.Cards.Count);
    }

    [Theory]
    [InlineData("dealDamage")]
    [InlineData("takeDamage")]
    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    public void ReplacementChoiceSettlesCombinedCostsBeforeDefeatInterrupt(string kind)
    {
        // Multiple costs "must be paid simultaneously." The replacement choice
        // fixes the damage payment; exhaustion must then be visible at the
        // would-defeat interrupt, while "Pay the cost(s)" still precedes draw.
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"exhaust":"this"},{"{{{{kind}}}}":{"cards":"this","amount":2}}]},
                "effect":{"draw":{"player":"you","count":2}}
              }]},
              {"card":"01092","abilities":[
                {"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","subject":"game"},
                 "effect":{"choose":{"options":[
                   {"draw":{"player":"you","count":1}},
                   {"preventDamage":"trigger.target"}
                 ]}}},
                {"trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                 "effect":{"draw":{"player":"you","count":1}}}
              ]}
            ]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(2, source!.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.False(source.Ready);
        Assert.Equal(4, source.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(held + 3, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }


    [Fact]
    [Rule("rr:cost.5")]
    [Rule("rr:damage.step.6")]
    public void SimultaneousDamageComponentsOpenOneDefeatInterruptForTheRecipient()
    {
        // Costs "must be paid simultaneously." Their combined placement reaches
        // one point where this character "would be defeated," not two defeats.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[
                  {"takeDamage":{"cards":"this","amount":1}},
                  {"takeDamage":{"cards":"this","amount":1}},
                  {"exhaust":"this"}
                ]},
                "effect":{"draw":{"player":"you","count":2}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.False(source!.Ready);
        Assert.Equal(4, source.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

}
