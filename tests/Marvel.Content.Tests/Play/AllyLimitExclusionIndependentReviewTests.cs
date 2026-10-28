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

// Synthetic abilities and prepared boards; these do not admit Stinger as playable content.
public sealed class AllyLimitExclusionIndependentReviewTests
{
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"12014","abilities":[{"trigger":{"timing":"Constant"},
            "effect":{"grant":{"card":"this","keyword":"ally_limit_excluded"}}}]},
          {"card":"06012","abilities":[{"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
            "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]}
        ]}
        """));

    [Fact]
    [Rule("rr:ally-limit")]
    public void RestoredLimitDoesNotLeaveAMandatoryDiscard()
    {
        // "until they have a number of allies in play equal to their ally limit."
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("12014", area);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var reduction = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: -1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        reduction.Dispose();
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(4, area.Cards.Count);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void RestoredLimitSkipsStaleChoiceAndContinuesEntryResponse()
    {
        // Discard applies only while the player controls more than the limit.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", events);
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        using var increase = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: 1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.NotEqual(Steps.ChooseAllyForLimit, prompt.Trigger);
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == ally.ObjectId);
        Assert.False(ally.Tokens.ContainsKey("c_test"));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(1, ally.Tokens["c_test"]);
        Assert.Equal(4, area.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void RestoredThenReducedLimitStillRequiresTheQueuedChoice()
    {
        // The live count, rather than whether the plan was once obsolete, decides.
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var reduction = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: -1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        reduction.Dispose();
        using var second = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: -1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Equal(3, prompt.Affordances.Count);
    }

    [Fact]
    public void RepeatedQueriesPreserveStateAndTheNextLimitChoice()
    {
        var runner = Runner();
        var probed = ThorActualFixture.Board("06001a");
        var control = ThorActualFixture.Board("06001a");
        foreach (var world in new[] { probed, control })
        {
            world.Abilities = runner;
            var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            world.CreateCard("12014", area);
            world.CreateCard("01067", area);
            world.CreateCard("01068", area);
            world.CreateCard("01041", area);
            world.CreateCard("21019", world.Seats[0].Hand);
        }
        string before = probed.Digest().Canonical();
        var random = probed.Random.Generator.GetState();
        var words = probed.Random.Generator.WordsConsumed;
        var pending = probed.Agenda.Outstanding.ToArray();
        var registered = probed.Effects.Registered.ToArray();
        for (int i = 0; i < 3; i++)
        {
            Assert.Contains(probed.Effects.Active(), effect => effect.Kind == AllyLimitExclusion.Modifier);
            CardPayment.CostOf(probed, probed.Facts, probed.Seats[0], probed.Seats[0].Hand.Cards[^1]);
        }
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(random.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(words, probed.Random.Generator.WordsConsumed);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        Assert.Equal(registered, probed.Effects.Registered);
        foreach (var world in new[] { probed, control })
            CardPlay.PutAllyIntoPlay(world, world.Facts, runner, world.Seats[0].Hand.Cards[^1], 0, "synthetic", []);
        var actual = Sequence.Work(probed, probed.Facts, runner, [])!;
        var expected = Sequence.Work(control, control.Facts, runner, [])!;
        Assert.Equal(Steps.ChooseAllyForLimit, actual.Trigger);
        Assert.Equal(expected.Trigger, actual.Trigger);
        Assert.Equal(expected.Affordances.Select(option => option.Id), actual.Affordances.Select(option => option.Id));
        Sequence.Answer(probed, probed.Facts, runner, actual, Decision.Take(actual.Affordances[^1].Id), []);
        Sequence.Answer(control, control.Facts, runner, expected, Decision.Take(expected.Affordances[^1].Id), []);
        Assert.Null(Sequence.Work(probed, probed.Facts, runner, []));
        Assert.Null(Sequence.Work(control, control.Facts, runner, []));
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, probed.Random.Generator.WordsConsumed);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void SyntheticExpirationDuringSuspendedEntrySettlesBeforeResponse()
    {
        // "The player must discard down to their ally limit before resolving any abilities that resolve when the ally enters play."
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", area);
        var first = world.CreateCard("01067", area);
        var second = world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        world.CreateCard("21019", area);
        var increase = world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "ally_limit", Amount: 1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", events);
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
        increase.Dispose(); // Synthetic interposition, not a claimed reachable timing point.
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
        Assert.False(ally.Tokens.ContainsKey("c_test"));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(first.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.False(ally.Tokens.ContainsKey("c_test"));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(second.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(1, ally.Tokens["c_test"]);
    }
}
