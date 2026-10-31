using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic constant on printed Stinger; the production ability book is unchanged.
public sealed class AllyLimitExclusionTests
{
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"12014","abilities":[{
          "trigger":{"timing":"Constant"},
          "effect":{"grant":{"card":"this","keyword":"ally_limit_excluded"}}
        }]}]}
        """));

    [Theory]
    [InlineData("Action", "this", 1)]
    [InlineData("Constant", "you", 1)]
    [InlineData("Constant", "this", 2)]
    public void AdmissionRejectsUnsupportedExclusionShapes(string timing, string target, int amount)
    {
        string book = $$$$"""
            {"cards":[{"card":"12014","abilities":[{
              "trigger":{"timing":"{{{{timing}}}}"},
              "effect":{"grant":{"card":"{{{{target}}}}","keyword":"ally_limit_excluded","amount":{{{{amount}}}}}}
            }]}]}
            """;
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
    }

    [Fact]
    public void AdmissionRejectsASelfExclusionInsideALargerConstantBody()
    {
        // The grant alone is a well-formed self grant of 1; only the program
        // check sees that it is not the ability's whole body.
        const string book = """
            {"cards":[{"card":"12014","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"seq":[{"grant":{"card":"this","keyword":"ally_limit_excluded"}}]}
            }]}]}
            """;
        var error = Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
        Assert.Contains("standalone constant self grant", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("12014", false)]
    [InlineData("21019", true)]
    [Rule("rr:ally-limit")]
    [Rule("rr:ability.8.2")]
    public void GamePlayResumesAtTheCorrectDecision(string face, bool choiceRequired)
    {
        // The excess choice precedes entry abilities; a constant starts on entry.
        var runner = Runner();
        Card ally = null!;
        Card payment = null!;
        var (game, world) = Playing(board =>
        {
            var area = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            board.CreateCard("01067", area);
            board.CreateCard("01068", area);
            board.CreateCard("01041", area);
            ally = board.CreateCard(face, board.Seats[0].Hand);
            payment = board.CreateCard("01003", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally.ObjectId);
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]) with
        { Targets = [world.Seats[0].IdentityCard.ObjectId] });
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(!choiceRequired, game.IsRootPrompt);
        if (choiceRequired)
        {
            Assert.Equal(Steps.ChooseAllyForLimit, game.Pending!.Trigger);
            var discard = Assert.Single(game.Pending.Affordances, item => item.AnchorId == ally.ObjectId);
            game.Resolve(Decision.Take(discard.Id));
            Assert.True(game.IsRootPrompt);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        }
        else Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    [Rule("rr:ability.8.2")]
    public void PutIntoPlayExcludesSelfAndLaterOrdinaryAllyRequiresChoice()
    {
        // The excess discard occurs before entry abilities; constants are active as soon as a card enters.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var first = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", pile);
        var events = new List<GameEvent>();
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, stinger, 0, "synthetic", events);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        var blade = world.CreateCard("21019", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], blade, [payment.ObjectId], []);
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
        Assert.Contains(prompt.Affordances, option => option.Id == blade.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.AlliesArea, stinger.Area.Type);
        Assert.Equal(DeckType.AlliesArea, blade.Area.Type);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void FallingLimitRepeatsAfterChoosingAnExcludedAlly()
    {
        // A player above their limit must discard allies until the count equals the limit.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", area);
        var first = world.CreateCard("01067", area);
        var second = world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        using var reduction = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: -2,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(stinger.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(first.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(second.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, stinger.Area.Type);
        Assert.Single(area.Cards);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    [Rule("rr:ability.8.2")]
    public void SyntheticBlankMakesTheAllyCountAndReentryRestoresTheConstant()
    {
        // The constant remains active while the card is in play.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", area);
        var first = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        // Synthetic lasting blank: the public blanking action supports minions only.
        using var blank = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, PrintedText.Blanked,
            Card: stinger.ObjectId, Affects: stinger.ObjectId)
        { TargetIncarnation = stinger.Incarnation });
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        blank.Dispose();
        Discard.Card(world, stinger, "synthetic", events);
        world.CreateCard("21019", area);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, stinger, 0, "synthetic", events);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.AlliesArea, stinger.Area.Type);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void ExpirationOfLimitIncreaseCountsOnlyOrdinaryAllies()
    {
        // The limit applies whenever the count exceeds the live modified limit.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", area);
        var increase = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: 1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        world.CreateCard("21019", area);
        increase.Dispose();
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(5, prompt.Affordances.Count);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void ControlTransferRechecksTheNewControllersCount()
    {
        // The limit belongs to the player who controls the allies.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var area0 = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var area1 = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        var stinger = world.CreateCard("12014", area0);
        var ordinary = world.CreateCard("21019", area0);
        world.CreateCard("01067", area1);
        world.CreateCard("01068", area1);
        world.CreateCard("01041", area1);
        CardPlay.TakeControl(world, world.Facts, stinger, 1);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        CardPlay.TakeControl(world, world.Facts, ordinary, 1);
        var choice = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        Assert.Equal(1, choice.Seat);
    }
}
