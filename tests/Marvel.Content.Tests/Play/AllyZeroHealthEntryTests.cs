using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual printed zero HP, with no authored card ability or pack admission.
public sealed class AllyZeroHealthEntryTests
{
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""{"cards":[]}"""));

    [Theory]
    [InlineData("play")]
    [InlineData("put")]
    [InlineData("paid put")]
    [Rule("rr:hit-points.3")]
    public void ZeroHealthAllyUnderLimitIsDefeatedOnEntry(string entry)
    {
        // "An ally or minion with zero or fewer remaining hit points is defeated".
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        if (entry == "play") CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally, [], events);
        else if (entry == "paid put")
        {
            var payment = CardPlay.Spend(world, world.Facts, runner, [world.Seats[0].Hand], [],
                0, "", -1, 0, events, payingFor: ally);
            AllyCardEntry.PutPaid(world, world.Facts, runner, EntryPayment.Capture(ally, 0, payment), "synthetic", events);
        }
        else CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ability.8.2")]
    public void LiveConstantHealthAllowsEntryAndItsOwnResponse()
    {
        // A constant ability becomes active as soon as its card enters play.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12011","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"grant":{"card":"this","keyword":"health","amount":1}}},
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"response","count":1}}}
            ]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_response"));
    }

    [Fact]
    public void TeamTrainingKeepsZeroPrintedHealthAllyInPlayWithoutCounters()
    {
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var training = world.CreateCard("04016", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, world.Facts, training, [], abilities: runner);
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(1, DamagePlacement.Health(world, world.Facts, ally));
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_pym"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StagedCounterDeterminesLiveHealthBeforeLimit(bool take)
    {
        // Synthetic interpretation of a counter-based constant and entry interrupt.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12011","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"grant":{"card":"this","keyword":"health",
                 "amount":{"countersOn":{"card":"this","counter":"pym"}}}}},
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Interrupt","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"pym","count":1}}}
            ]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var old = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt,
            take ? Decision.Take(offer.Id) : Decision.Decline, []);
        if (!take)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, world.Facts, runner, []));
            Assert.Contains("zero-health defeat and ally-limit", error.Message);
            return;
        }
        var limit = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Steps.ChooseAllyForLimit, limit.Trigger);
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_pym"));
        Sequence.Answer(world, world.Facts, runner, limit, Decision.Take(old.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
    }

    [Fact]
    public void EntryResponseCannotRescueAnAlreadyDefeatedAlly()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12011","abilities":[
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"response","count":1}}}
            ]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_response"));
    }

    [Fact]
    public void EntryDefeatIsRecordedOnTheEntryOccurrence()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Contains(response.Affordances, item => item.AnchorId == observer.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
    }

    [Fact]
    public void DefeatInterruptFinishesBeforeEntryResponse()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]},{"card":"12011","abilities":[{
              "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
              "effect":{"placeCounters":{"card":"this","counter":"response","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Decline, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_response"));
    }

    [Fact]
    public void ParentActionContinuesAfterEntryDefeat()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[
                {"putIntoPlay":{"card":{"cardsIn":{"area":"yourHand","title":"Ant-Man"}},"where":"yourPlayArea"}},
                {"placeCounters":{"card":"you","counter":"suffix","count":1}}
              ]}
            }]}]}
            """));
        Card? ally = null;
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("12011", board.Seats[0].Hand);
            source = board.CreateCard("01006", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.DiscardPile, ally!.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_suffix"));
    }

    [Fact]
    public void ZeroHealthAllyAtTheLimitIsDefeatedWithoutLimitChoice()
    {
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(2, area.Cards.Count);
    }

    [Fact]
    public void SimultaneousZeroHealthAndExcessAllyLimitFailsClosed()
    {
        // MARVEL-885 tracks the unresolved ordering of these framework obligations.
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        Assert.Throws<RulesNotImplementedException>(() =>
        {
            CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
            Sequence.Work(world, world.Facts, runner, []);
        });
    }
}
