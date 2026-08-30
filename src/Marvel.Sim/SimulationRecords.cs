using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Sim;

internal static class RecordJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    public static void Write(TextWriter writer, object record)
    {
        writer.WriteLine(JsonSerializer.Serialize(record, record.GetType(), Options));
        writer.Flush();
    }

    public static JsonElement Event(GameEvent happened) =>
        JsonSerializer.SerializeToElement<GameEvent>(happened, EventJson.Options);
}

internal sealed record HeaderRecord(
    string Type,
    int Schema,
    string Scenario,
    string Difficulty,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    string Policy,
    int PolicyVersion,
    string PolicyVisibility,
    uint PolicySeed,
    int DecisionLimit,
    string SeedMode,
    uint? SelectionSeed,
    IReadOnlyList<uint> Seeds);

internal sealed record StartRecord(
    string Type,
    int Game,
    uint Seed,
    uint PolicySeed,
    IReadOnlyList<uint> SeatPolicySeeds,
    string InitialDigest,
    IReadOnlyList<JsonElement> SetupEvents);

internal sealed record StepRecord(
    string Type,
    int Game,
    int Step,
    PromptRecord Prompt,
    DecisionSelector Decision,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<JsonElement> Events,
    string Digest);

internal sealed record ResultRecord(
    string Type,
    int Game,
    uint Seed,
    string Outcome,
    int Round,
    int Decisions,
    PolicyMetrics Metrics,
    string TerminalDigest);

internal sealed record FailureRecord(
    string Type,
    string Category,
    int Game,
    uint Seed,
    int Step,
    int Round,
    PolicyMetrics Metrics,
    string Exception,
    string Message,
    PromptRecord? Prompt,
    DecisionSelector? Decision,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    IReadOnlyDictionary<string, long> Values,
    string? LastGoodDigest,
    string? PostFailureDigest,
    IReadOnlyList<StepRecord> RecentSteps,
    string Reproduce);

internal sealed record SummaryRecord(
    string Type,
    int Games,
    int PlayersWin,
    int VillainWins,
    int PlayersLose,
    int Failures,
    int Decisions,
    int Rounds,
    int CardsPlayed,
    int PlayerAttacks,
    int Payments,
    int ResourceAbilitiesUsed,
    IReadOnlyDictionary<string, int> FailureSignatures);

internal sealed record PromptRecord(
    int Player,
    string Asking,
    string When,
    string Trigger,
    string Label,
    bool Cancellable,
    IReadOnlyList<AffordanceRecord> Affordances)
{
    public static PromptRecord From(Prompt prompt) => new(
        prompt.Player,
        prompt.Asking.ToString(),
        prompt.When.ToString(),
        prompt.Trigger,
        prompt.Label,
        prompt.Cancellable,
        [.. prompt.Affordances.Select(AffordanceRecord.From)]);
}

internal sealed record AffordanceRecord(
    string Verb,
    int AnchorId,
    int AnchorPlayer,
    string Label,
    JsonElement? Targets,
    IReadOnlyList<JsonElement> Costs,
    string? Illegal)
{
    public static AffordanceRecord From(Affordance affordance) => new(
        affordance.Verb,
        affordance.AnchorId,
        affordance.AnchorPlayer,
        affordance.Label,
        affordance.Targets is null
            ? null
            : JsonSerializer.SerializeToElement(affordance.Targets, RecordJson.Options),
        [.. affordance.CostOptions.Select(cost =>
            JsonSerializer.SerializeToElement(cost, RecordJson.Options))],
        affordance.Illegal);
}

internal sealed record DecisionSelector(
    bool Decline,
    int? AnchorId,
    int? AnchorPlayer,
    string? Verb,
    string? Label,
    int Occurrence)
{
    public static DecisionSelector From(Prompt prompt, Decision decision)
    {
        if (decision.IsDecline)
        {
            return new DecisionSelector(true, null, null, null, null, 0);
        }

        var selected = prompt.Affordances.Single(option => option.Id == decision.Affordance);
        var exact = prompt.Affordances.Where(option =>
            option.AnchorId == selected.AnchorId
            && option.AnchorPlayer == selected.AnchorPlayer
            && string.Equals(option.Verb, selected.Verb, StringComparison.Ordinal)
            && string.Equals(option.Label, selected.Label, StringComparison.Ordinal));
        int occurrence = exact.TakeWhile(option => option.Id != selected.Id).Count();
        return new DecisionSelector(
            false,
            selected.AnchorId,
            selected.AnchorPlayer,
            selected.Verb,
            selected.Label,
            occurrence);
    }

    public Decision Resolve(
        Prompt prompt, IReadOnlyList<int> targets, IReadOnlyList<int> resources,
        IReadOnlyDictionary<string, long> values)
    {
        if (Decline)
        {
            if (targets.Count > 0 || resources.Count > 0)
            {
                throw new ReplayDivergenceException(
                    $"decline at prompt '{prompt.Label}' records targets or resources");
            }

            return Decision.Decline;
        }

        var exact = prompt.Affordances.Where(option =>
                option.IsLegal
                && option.AnchorId == AnchorId
                && option.AnchorPlayer == AnchorPlayer
                && string.Equals(option.Verb, Verb, StringComparison.Ordinal)
                && string.Equals(option.Label, Label, StringComparison.Ordinal))
            .ToList();
        if (Occurrence < 0 || Occurrence >= exact.Count)
        {
            throw new ReplayDivergenceException(
                $"prompt '{prompt.Label}' has {exact.Count} matching affordance(s), "
                + $"not recorded occurrence {Occurrence}");
        }

        var selected = exact[Occurrence];
        if (selected.Targets is null ? targets.Count > 0 : !selected.Targets.Allows(targets))
        {
            throw new ReplayDivergenceException(
                $"recorded targets are not allowed by '{selected.Label}'");
        }

        if (!PaymentIsOffered(selected, resources))
        {
            throw new ReplayDivergenceException(
                $"recorded resources do not pay an offered cost for '{selected.Label}'");
        }

        var requested = selected.CostOptions
            .SelectMany(cost => cost.VariableRequests)
            .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
        if (values.Count != requested.Count
            || values.Any(entry => !requested.TryGetValue(entry.Key, out var variable)
                || !variable.Allows(entry.Value)))
        {
            throw new ReplayDivergenceException(
                $"recorded variables are not allowed by '{selected.Label}'");
        }

        return Decision.Take(selected.Id, targets, resources, values);
    }

    private static bool PaymentIsOffered(
        Affordance selected, IReadOnlyList<int> resources)
    {
        if (resources.Distinct().Count() != resources.Count)
        {
            return false;
        }

        if (selected.CostOptions.Count == 0)
        {
            return resources.Count == 0;
        }

        return selected.CostOptions.Any(cost =>
        {
            var sources = new List<ResourceSource>(resources.Count);
            foreach (int resource in resources)
            {
                var matches = cost.Generators
                    .Where(source => source.Effect == resource)
                    .ToList();
                if (matches.Count != 1)
                {
                    return false;
                }

                sources.Add(matches[0]);
            }

            string generated = string.Concat(sources.Select(source => source.Generates));
            bool primary = long.TryParse(
                    cost.Cost,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out long amount)
                && Resources.Pays(generated, amount, string.Concat(cost.Rule ?? []));
            bool alternative = cost.HasAlternative
                && long.TryParse(
                    cost.OrCost,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out long other)
                && Resources.Pays(generated, other, string.Concat(cost.OrRule ?? []));
            return primary || alternative;
        });
    }
}

internal sealed record PolicyMetrics(
    int CardsPlayed,
    int PlayerAttacks,
    int Payments,
    int ResourceAbilitiesUsed);

internal sealed record SimulationSummary(
    int Games,
    int PlayersWin,
    int VillainWins,
    int PlayersLose,
    int Failures,
    int Decisions,
    int Rounds,
    int CardsPlayed,
    int PlayerAttacks,
    int Payments,
    int ResourceAbilitiesUsed,
    IReadOnlyDictionary<string, int> FailureSignatures)
{
    public int ExitCode => Failures == 0 ? 0 : 1;

    public string Human(string? path = null)
    {
        string destination = path is null ? string.Empty : $" Records: {path}.";
        return $"{Games} game(s): {PlayersWin} player win(s), {VillainWins} villain win(s), "
            + $"{PlayersLose} player-loss ending(s), {Failures} failure(s), "
            + $"{Decisions} decision(s), {Rounds} total round(s), {CardsPlayed} card(s) played, "
            + $"{PlayerAttacks} player attack(s), {Payments} paid cost(s).{destination}";
    }
}

internal sealed record ReplaySummary(int Games, int Steps);
