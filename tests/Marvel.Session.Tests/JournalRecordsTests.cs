using System.Text.Json;
using System.Text.Json.Nodes;
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
        var recorded = DurableDecision.From(1, original, Decision.Take(11));
        var replayed = Prompt(
            new Affordance(110, "Action", 7, 1, "Choose"),
            new Affordance(111, "Action", 7, 1, "Choose"));

        Assert.Equal(111, recorded.Resolve(replayed).Affordance);
        Assert.Equal(1, DurableDecision.SimulationActor(original, Decision.Take(11)));
        Assert.Throws<ReplayDivergenceException>(() =>
            (recorded with { Actor = 0 }).Resolve(replayed));
        string json = JsonSerializer.Serialize(recorded, JournalJson.Options);
        Assert.DoesNotContain("affordance", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IllegalDuplicateDoesNotShiftTheRecordedLegalOccurrence()
    {
        var original = Prompt(
            new Affordance(10, "Action", 7, 0, "Choose", Illegal: "blocked"),
            new Affordance(11, "Action", 7, 0, "Choose"));
        var recorded = DurableDecision.From(0, original, Decision.Take(11));
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

        var recorded = DurableDecision.From(1, asked, input);
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
        var asked = Prompt(new Affordance(1, "Play", 7, 0, "Choose"));
        var recorded = DurableDecision.From(0, asked, Decision.Take(1));

        Assert.Throws<ReplayDivergenceException>(() =>
            recorded.Resolve(asked with { Player = 1 }));
        Assert.Throws<ReplayDivergenceException>(() =>
            recorded.Resolve(Prompt(new Affordance(2, "Play", 8, 0, "Choose"))));
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
    public void DeclineRequiresThePromptPlayerAndACancellablePrompt()
    {
        var mandatory = Prompt(new Affordance(1, "Action", 7, 0, "Required"));
        var decline = DurableDecision.From(0, mandatory, Decision.Decline);

        Assert.Throws<ReplayDivergenceException>(() => decline.Resolve(mandatory));
        Assert.Throws<ReplayDivergenceException>(() =>
            (decline with { Actor = 1 }).Resolve(mandatory with { Cancellable = true }));
        Assert.True(decline.Resolve(mandatory with { Cancellable = true }).IsDecline);
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

    [Fact]
    public void ReplayRecordsOmitPresentationOnlyEventSubjects()
    {
        var happened = new FieldSet(7, "health", 1, 0)
        {
            Subjects = new Dictionary<int, string> { [7] = "Drone" },
        };

        JsonElement recorded = JournalJson.Event(happened);

        Assert.False(recorded.TryGetProperty("subjects", out _));
        JournalReplay.RequireEvents([recorded], [happened], "events");
    }

    [Fact]
    public void CanonicalPromptRecordsContainEachIndependentCostAndTargetFieldOnce()
    {
        var target = new TargetRequest(
            [20, 10],
            1,
            2,
            Groups: [[20], [10]],
            MustIncludeTraits: ["t_hero"],
            Rule: "named",
            IsSearch: true,
            AllowRepeated: true,
            MaximumOccurrences: new Dictionary<int, int> { [20] = 2 })
        {
            Details = new Dictionary<int, string> { [20] = "two damage" },
        };
        CostOption[] costs =
        [
            new(0, "0", Sources: null),
            new(0, "0", Sources: []),
            new(
                20,
                "X",
                Rule: ["mental"],
                OrCost: "2",
                OrRule: ["any"],
                Sources: [new ResourceSource(8, "BY")],
                Variables: [new VariableRequest("X", 1, 3)],
                Components: [new ResourceCost("1", ["energy"], Printed: true)],
                DeclarationSensitive: true),
        ];
        PromptRecord record = PromptRecord.From(Prompt(new Affordance(
            4, "Action", 3, 1, "Pay", target, costs)));

        string json = JsonSerializer.Serialize(record, JournalJson.Options);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement affordance = document.RootElement.GetProperty("affordances")[0];
        JsonElement recordedTarget = affordance.GetProperty("targets");
        JsonElement recordedCosts = affordance.GetProperty("costs");

        Assert.True(recordedTarget.TryGetProperty("groups", out _));
        Assert.False(recordedTarget.TryGetProperty("is_grouped", out _));
        Assert.Equal(JsonValueKind.Null, recordedCosts[0].GetProperty("sources").ValueKind);
        Assert.Empty(recordedCosts[1].GetProperty("sources").EnumerateArray());
        Assert.Equal(8, recordedCosts[2].GetProperty("sources")[0].GetProperty("effect").GetInt32());
        Assert.Equal(3, Count(json, "\"sources\""));
        Assert.DoesNotContain("has_alternative", json, StringComparison.Ordinal);
        Assert.DoesNotContain("generators", json, StringComparison.Ordinal);
        Assert.DoesNotContain("variable_requests", json, StringComparison.Ordinal);
        Assert.DoesNotContain("resource_costs", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaTwoPromptReaderVerifiesAliasesBeforeCanonicalizing()
    {
        PromptRecord current = PromptRecord.From(Prompt(new Affordance(
            4,
            "Action",
            3,
            1,
            "Pay",
            new TargetRequest([20], 1, 1, Groups: [[20]]),
            [new CostOption(
                20,
                "1",
                Sources: [new ResourceSource(8, "M")],
                Components: [new ResourceCost("1", ["mental"], Printed: true)])])));
        JsonObject legacy = SchemaTwoPrompt(current);

        PromptRecord parsed = SchemaTwoPromptJson.Read(
            JsonSerializer.SerializeToElement(legacy, JournalJson.Options));

        Assert.Equal(
            JsonSerializer.Serialize(current, JournalJson.Options),
            JsonSerializer.Serialize(parsed, JournalJson.Options));
        legacy["affordances"]![0]!["costs"]![0]!["generators"] = new JsonArray();
        Assert.Throws<JsonException>(() => SchemaTwoPromptJson.Read(
            JsonSerializer.SerializeToElement(legacy, JournalJson.Options)));
    }

    [Fact]
    public void ReplayPinsIndependentTargetCostAndUnchosenAffordanceFields()
    {
        var asked = Prompt(
            new Affordance(
                4,
                "Action",
                3,
                1,
                "Pay",
                new TargetRequest([20], 1, 1, Groups: [[20]]),
                [new CostOption(
                    20,
                    "X",
                    Rule: ["mental"],
                    OrCost: "2",
                    OrRule: ["any"],
                    Sources: [new ResourceSource(8, "M")],
                    Variables: [new VariableRequest("X", 1, 3)],
                    Components: [new ResourceCost("1", ["energy"], Printed: true)],
                    DeclarationSensitive: true)]),
            new Affordance(5, "Action", 9, 1, "Unchosen", Illegal: "blocked"));
        PromptRecord recorded = PromptRecord.From(asked);
        AffordanceRecord first = recorded.Affordances[0];
        TargetRequestRecord target = first.Targets!;
        CostOptionRecord cost = first.Costs[0];
        PromptRecord[] changed =
        [
            recorded with
            {
                Affordances = [first with { Targets = target with { Groups = [[20, 21]] } },
                    recorded.Affordances[1]],
            },
            recorded with
            {
                Affordances = [first with
                {
                    Costs = [cost with
                    {
                        Sources = [new ResourceSourceRecord(8, "E")],
                    }],
                }, recorded.Affordances[1]],
            },
            recorded with
            {
                Affordances = [first with
                {
                    Costs = [cost with
                    {
                        Variables = [new VariableRequestRecord("X", 1, 4)],
                    }],
                }, recorded.Affordances[1]],
            },
            recorded with
            {
                Affordances = [first with
                {
                    Costs = [cost with
                    {
                        Components = [new ResourceCostComponentRecord(
                            "1", ["mental"], Printed: true)],
                    }],
                }, recorded.Affordances[1]],
            },
            recorded with
            {
                Affordances = [first, recorded.Affordances[1] with { Illegal = null }],
            },
        ];

        foreach (PromptRecord mutation in changed)
        {
            Assert.Throws<ReplayDivergenceException>(() =>
                JournalReplay.RequirePrompt(mutation, asked, "prompt"));
        }
    }

    internal static JsonObject SchemaTwoPrompt(PromptRecord prompt)
    {
        JsonObject root = Assert.IsType<JsonObject>(JsonSerializer.SerializeToNode(
            prompt, JournalJson.Options));
        foreach (JsonNode? affordanceNode in root["affordances"]!.AsArray())
        {
            JsonObject affordance = affordanceNode!.AsObject();
            if (affordance["targets"] is JsonObject target)
            {
                target["is_grouped"] = target["groups"] is JsonArray { Count: > 0 };
            }

            foreach (JsonNode? costNode in affordance["costs"]!.AsArray())
            {
                JsonObject cost = costNode!.AsObject();
                cost["has_alternative"] = cost["or_cost"]!.GetValue<string>().Length > 0;
                cost["generators"] = cost["sources"]?.DeepClone() ?? new JsonArray();
                cost["variable_requests"] = cost["variables"]?.DeepClone() ?? new JsonArray();
                cost["resource_costs"] = cost["components"]?.DeepClone()
                    ?? new JsonArray(new JsonObject
                    {
                        ["cost"] = cost["cost"]!.GetValue<string>(),
                        ["rule"] = cost["rule"]?.DeepClone(),
                        ["printed"] = false,
                    });
            }
        }

        return root;
    }

    private static int Count(string value, string needle) =>
        value.Split(needle, StringSplitOptions.None).Length - 1;

    private static Prompt Prompt(params Affordance[] affordances) => new(
        0,
        Question.TurnOption,
        TimingPriority.Untimed,
        string.Empty,
        "Prompt",
        false,
        affordances);
}
