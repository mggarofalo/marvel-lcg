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
    int Game,
    uint Seed,
    int Step,
    string Exception,
    string Message,
    PromptRecord? Prompt,
    DecisionSelector? Decision,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    string? LastGoodDigest,
    string? PostFailureDigest,
    IReadOnlyList<JsonElement> RecentEvents,
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
    bool Cancellable)
{
    public static PromptRecord From(Prompt prompt) => new(
        prompt.Player,
        prompt.Asking.ToString(),
        prompt.When.ToString(),
        prompt.Trigger,
        prompt.Label,
        prompt.Cancellable);
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

    public Decision Resolve(Prompt prompt, IReadOnlyList<int> targets, IReadOnlyList<int> resources)
    {
        if (Decline)
        {
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

        return Decision.Take(exact[Occurrence].Id, targets, resources);
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
