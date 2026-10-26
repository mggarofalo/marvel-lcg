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

// Synthetic abilities isolate completion priority; no content is admitted.
public sealed class MainSchemeCompletionOrderingTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    [Rule("rr:when-completed-abilities.1")]
    public void FirstPlayerOrdersCompletionWithForcedInterruptAndEachFinishesItsChildren(
        bool completionFirst, bool final)
    {
        // "the first player decides the order"; "each forced ability must resolve
        // as completely as possible before the next forced ability ... may initiate."
        var runner = Runner();
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        world.FirstPlayer = 1;
        var scheme = world.CreateCard("02004b", world.AreaOf(DeckType.MainSchemesArea));
        if (!final) world.CreateCard("01137a,01137b", world.AreaOf(DeckType.MainSchemesDeck));
        var events = new List<GameEvent>();
        Threat.Place(world, Cards, runner, scheme, 14, "synthetic", events);

        var ordering = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Order, ordering.Asking);
        Assert.Equal(1, ordering.Player);
        Assert.False(ordering.Cancellable);
        Assert.Equal(2, ordering.Affordances.Count);
        var firstType = completionFirst ? AbilityType.WhenCompleted : AbilityType.ForcedInterrupt;
        var first = Assert.Single(ordering.Affordances, option => option.Timing == firstType);
        Sequence.Answer(world, Cards, runner, ordering, Decision.Take(first.Id), events);

        var firstChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Option, firstChoice.Asking);
        Assert.Equal(15, scheme.Tokens["k_threat"]);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("c_progress"));
        AssertPending(world, scheme, events);
        Sequence.Answer(world, Cards, runner, firstChoice, Decision.Take(0), events);

        var secondChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Option, secondChoice.Asking);
        Assert.Equal(16, scheme.Tokens["k_threat"]);
        Assert.Equal(1, scheme.Tokens["c_progress"]);
        string firstObserved = completionFirst ? "c_completion" : "c_forced";
        string secondObserved = completionFirst ? "c_forced" : "c_completion";
        Assert.Equal(1, scheme.Tokens[firstObserved]);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault(secondObserved));
        AssertPending(world, scheme, events);
        Sequence.Answer(world, Cards, runner, secondChoice, Decision.Take(1), events);
        Sequence.Finish(world, Cards, runner, events);

        var observations = events.OfType<FieldSet>()
            .Where(change => change.Field == firstObserved || change.Field == secondObserved).ToArray();
        Assert.Equal(new[] { firstObserved, secondObserved }, observations.Select(change => change.Field));
        Assert.Equal(new long?[] { 1, 3 }, observations.Select(change => change.To));
        var completed = Assert.Single(events.OfType<FieldSet>(), change => change.Field == "is_completed");
        Assert.True(events.IndexOf(observations[1]) < events.IndexOf(completed));
        Assert.Equal(final ? Outcome.VillainWins : Outcome.Unfinished, world.Result);
        Assert.Equal(final ? DeckType.MainSchemesArea : DeckType.RemovedArea, scheme.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:when-completed-abilities.1")]
    public void CompletionTextOnAnotherCardDoesNotAnswerTheCompletingScheme()
    {
        // "When this scheme is completed..." restricts the printed timing even
        // when a synthetic definition omits an explicit subject predicate.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"02004b","abilities":[{
                "trigger":{"event":"WhenMainSchemeCompleted","timing":"WhenCompleted"},
                "effect":{"placeCounters":{"card":{"titled":"Criminal Enterprise"},"counter":"infamy","count":1}}}]},
              {"card":"02006a","abilities":[{
                "trigger":{"event":"WhenMainSchemeCompleted","timing":"WhenCompleted"},
                "effect":{"placeCounters":{"card":"this","counter":"infamy","count":100}}}]}
            ]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var scheme = world.CreateCard("02004b", world.AreaOf(DeckType.MainSchemesArea));
        var environment = world.CreateCard("02006a", world.AreaOf(DeckType.EnvironmentArea));
        var events = new List<GameEvent>();
        Threat.Place(world, Cards, runner, scheme, 7, "synthetic", events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, environment.Tokens["c_infamy"]);
        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    private static void AssertPending(World world, Card scheme, List<GameEvent> events)
    {
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.DoesNotContain(events, item => item is CardsMoved { Verb: "Advance" });
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02004b","abilities":[COMPLETION,FORCED]},
          {"card":"01137a","abilities":[]},{"card":"01137b","abilities":[]}]}
        """.Replace("COMPLETION", Ability("WhenCompleted", "completion"), StringComparison.Ordinal)
            .Replace("FORCED", Ability("ForcedInterrupt", "forced"), StringComparison.Ordinal)));

    private static string Ability(string timing, string observed) => """
        {"trigger":{"event":"WhenMainSchemeCompleted","timing":"TIMING","subject":"this"},
          "effect":{"seq":[
            {"placeThreat":{"scheme":"this","amount":1}},
            {"choose":{"options":[
              {"placeCounters":{"card":"this","counter":"progress","count":1}},
              {"placeCounters":{"card":"this","counter":"progress","count":2}}]}},
            {"placeCounters":{"card":"this","counter":"OBSERVED",
              "count":{"countersOn":{"card":"this","counter":"progress"}}}}
          ]}}
        """.Replace("TIMING", timing, StringComparison.Ordinal)
            .Replace("OBSERVED", observed, StringComparison.Ordinal);
}
