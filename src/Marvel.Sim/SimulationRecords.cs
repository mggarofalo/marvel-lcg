using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

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
    IReadOnlyList<ResourceAllocation> Allocations,
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
    IReadOnlyList<ResourceAllocation> Allocations,
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
