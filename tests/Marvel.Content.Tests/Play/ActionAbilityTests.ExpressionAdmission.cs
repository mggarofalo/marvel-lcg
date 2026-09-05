using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Fact]
    public void AnEmptyFutureBindingStillAdmitsItsReachedNumericRead()
    {
        // The selected path skips the area read. The explicit decline reaches
        // it, so the empty candidate must participate in admission as well.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"choose":{"options":[
                {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[]}}},
                {"seq":[]}
              ]}},
              {"discard":{"titled":"Shocker"}},
              {"placeCounters":{"card":"this","counter":"test","count":{
                "if":{"test":{"exists":"chosen"},"then":1,
                  "else":{"damageOn":{"cardsIn":{"area":"encounterDiscardPile","title":"Shocker"}}}}
              }}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner);
        world.CreateCard(AuthoredCards.Shocker, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(AuthoredCards.Shocker, world.AreaOf(DeckType.EncounterDiscardPile));
        string before = world.Digest().Canonical();

        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));

        Assert.Contains("singular area query", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(source.Ready);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericPreflightPreservesAnExplicitlyEmptyBinding(bool selectIdentity)
    {
        // Absence is a valid input to "exists chosen", not a missing player
        // error. Both the selected and declined paths must remain available.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"choose":{"options":[
                {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[]}}},
                {"seq":[]}
              ]}},
              {"placeCounters":{"card":"this","counter":"test","count":{
                "if":{"test":{"exists":"chosen"},"then":1,"else":2}
              }}},
              {"heal":{"card":"you","amount":1}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());

        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(2, game.Pending!.Affordances.Count);
        game.Resolve(Decision.Take(selectIdentity ? 0 : 1));
        if (selectIdentity) game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));

        Assert.Equal(selectIdentity ? 1 : 2, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(source.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void NumericPreflightWaitsForAPlayerSelectedByAnEarlierChoice()
    {
        // An unbound preview is not the numeric expression's live input.
        // The earlier answer supplies the player used by this count.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[]}}},
              {"placeCounters":{"card":"this","counter":"test","count":{
                "if":{"test":{"inForm":{"player":"chosenPlayer","form":"alter-ego"}},"then":1,"else":2}
              }}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[1].IdentityCard.ObjectId));

        Assert.Equal(1, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.False(source.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericBranchesAdmitOnlyTheSingleCardReadsTheyReach(bool readsDiscard)
    {
        // The engine refuses a singular read whose candidates can change
        // before resolution. An unvisited numeric branch requests no read
        // and therefore must not invoke that admission policy.
        const string read = """{"damageOn":{"cardsIn":{"area":"encounterDiscardPile","title":"Shocker"}}}""";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"seq":[
              {"discard":{"titled":"Shocker"}},
              {"placeCounters":{"card":"this","counter":"test","count":{
                "if":{"test":{"exists":{"query":"villain"}},
                  "then":{{{{{{(readsDiscard ? read : "1")}}}}}},
                  "else":{{{{{{(readsDiscard ? "1" : read)}}}}}}}
              }}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner);
        var minion = world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var discarded = world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EncounterDiscardPile));
        string before = world.Digest().Canonical();

        if (readsDiscard)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("singular area query", error.Message);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.True(source.Ready);
            Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        }
        else
        {
            var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
            Assert.Equal(before, world.Digest().Canonical());
            runner.Act(world, action, [], []);
            Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
            Assert.False(source.Ready);
            Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        }
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
    }
}
