using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class JournalRecordsTests
{
    [Fact]
    public void StableSelectorResolvesAChangedLiveHandleByOrderedOccurrence()
    {
        var original = Prompt(
            new Affordance(10, "Action", 7, 1, "Choose"),
            new Affordance(11, "Action", 7, 1, "Choose"));
        var recorded = DurableDecision.From(original, Decision.Take(11));
        var replayed = Prompt(
            new Affordance(110, "Action", 7, 1, "Choose"),
            new Affordance(111, "Action", 7, 1, "Choose"));

        Assert.Equal(111, recorded.Resolve(replayed).Affordance);
        string json = JsonSerializer.Serialize(recorded, JournalJson.Options);
        Assert.DoesNotContain("affordance", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IllegalDuplicateDoesNotShiftTheRecordedLegalOccurrence()
    {
        var original = Prompt(
            new Affordance(10, "Action", 7, 0, "Choose", Illegal: "blocked"),
            new Affordance(11, "Action", 7, 0, "Choose"));
        var recorded = DurableDecision.From(original, Decision.Take(11));
        var replayed = Prompt(new Affordance(111, "Action", 7, 0, "Choose"));

        Assert.Equal(0, recorded.Selector.Occurrence);
        Assert.Equal(111, recorded.Resolve(replayed).Affordance);
    }

    [Fact]
    public void DurableDecisionKeepsActorAndEveryAnswerInDomainOrder()
    {
        var cost = new CostOption(
            0,
            "2",
            Sources: [new ResourceSource(8, "BY")]);
        var asked = Prompt(new Affordance(
            4,
            "Action",
            3,
            1,
            "Pay",
            new TargetRequest([20, 10], 1, 2),
            [cost])) with { Player = 1 };
        var input = Decision.Take(
            4,
            [20, 10],
            [8],
            new Dictionary<string, long>(StringComparer.Ordinal),
            [new ResourceAllocation(8, 0, "BY")]);

        var recorded = DurableDecision.From(asked, input);
        var resolved = recorded.Resolve(asked);

        Assert.Equal(1, recorded.Actor);
        Assert.Equal([20, 10], resolved.Targets);
        Assert.Equal([8], resolved.Spent);
        Assert.Equal([new ResourceAllocation(8, 0, "BY")], resolved.Allocated);
        Assert.Throws<ReplayDivergenceException>(() =>
            (recorded with
            {
                Allocations = [new ResourceAllocation(8, 0, "BR")],
            }).Resolve(asked));
    }

    [Fact]
    public void ActorSelectorAndAnswerAreRejectedBeforeTheyBecomeAnEngineDecision()
    {
        var asked = Prompt(new Affordance(1, "Action", 7, 0, "Choose"));
        var recorded = DurableDecision.From(asked, Decision.Take(1));

        Assert.Throws<ReplayDivergenceException>(() =>
            recorded.Resolve(asked with { Player = 1 }));
        Assert.Throws<ReplayDivergenceException>(() =>
            recorded.Resolve(Prompt(new Affordance(2, "Action", 8, 0, "Choose"))));
        Assert.Throws<ReplayDivergenceException>(() =>
            new DurableDecision(
                0,
                new DecisionSelector(true, null, null, null, null, 0),
                [],
                [],
                new Dictionary<string, long>(StringComparer.Ordinal),
                [new ResourceAllocation(1, 0, "M")])
            .Resolve(asked));
    }

    [Fact]
    public void ReplayVerificationPinsPromptEventOrderAndStateFingerprint()
    {
        var asked = Prompt(new Affordance(1, "Action", 7, 0, "Choose"));
        var prompt = PromptRecord.From(asked);
        var events = new GameEvent[]
        {
            new FieldSet(7, "health", 3, 2),
            new FieldSet(8, "damage", 0, 1),
        };
        var recordedEvents = events.Select(JournalJson.Event).ToList();

        JournalReplay.RequirePrompt(prompt, asked, "prompt");
        JournalReplay.RequireEvents(recordedEvents, events, "events");
        JournalReplay.RequireFingerprint("abc", "abc", "digest");

        Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequirePrompt(prompt, asked with { Cancellable = true }, "prompt"));
        Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireEvents(recordedEvents, [events[1], events[0]], "events"));
        Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireFingerprint("abc", "abd", "digest"));
    }

    private static Prompt Prompt(params Affordance[] affordances) => new(
        0,
        Question.TurnOption,
        TimingPriority.Untimed,
        string.Empty,
        "Prompt",
        false,
        affordances);
}
