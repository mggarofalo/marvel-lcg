using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;

using static Marvel.Sim.SimulationRunHarness;
using static Marvel.Sim.SimulationReplayHarness;
using static Marvel.Sim.SimulationReportReader;
using static Marvel.Sim.SimulationHarnessSupport;
namespace Marvel.Sim;

internal static class SimulationReportReader
{
    public static SimulationSummary Report(string path)
    {
        return new SimulationReportState().Read(Path.GetFullPath(path));
    }
}

internal sealed class SimulationReportState
{
        private SummaryRecord? found;
        private HeaderRecord? header;
        private bool sawAny;
        private int games;
        private int playerWins;
        private int villainWins;
        private int playerLosses;
        private int failures;
        private int decisions;
        private int rounds;
        private int cardsPlayed;
        private int playerAttacks;
        private int payments;
        private int resourceAbilities;
        private readonly Dictionary<string, int> signatures = new(StringComparer.Ordinal);

    internal SimulationSummary Read(string path)
    {
        foreach (string line in SimulationRecordFiles.ReadLines(path))
        {
            Process(line);
            sawAny = true;
        }
        return Finish();
    }

    private void Process(string line)
    {
        if (found is not null)
            throw new ReplayDivergenceException("a record appeared after the terminal summary");
        using var document = JsonDocument.Parse(line);
        string type = RecordType(document.RootElement);
        switch (type)
        {
            case "header": ReadHeader(line, type); break;
            case "start": break;
            case "step": decisions++; break;
            case "result": AddResult(Read<ResultRecord>(line, type)); break;
            case "failure": AddFailureRecord(line, type); break;
            case "summary": found = Read<SummaryRecord>(line, type); break;
            default: throw new SimulationUsageException($"unknown record type '{type}'");
        }
    }

    private void ReadHeader(string line, string type)
    {
        if (sawAny) throw new ReplayDivergenceException("header is not the first record");
        header = Read<HeaderRecord>(line, type);
        ValidateRecordSchema(header.Schema);
    }

    private void AddResult(ResultRecord result)
    {
        RequireEqual(games, result.Game, "result game index");
        games++;
        rounds += result.Round;
        AddMetrics(result.Metrics, ref cardsPlayed, ref playerAttacks,
            ref payments, ref resourceAbilities);
        if (result.Outcome == nameof(Outcome.PlayersWin)) playerWins++;
        else if (result.Outcome == nameof(Outcome.VillainWins)) villainWins++;
        else if (result.Outcome == nameof(Outcome.PlayersLose)) playerLosses++;
        else throw new ReplayDivergenceException($"unknown result outcome '{result.Outcome}'");
    }

    private void AddFailureRecord(string line, string type)
    {
        int schema = header?.Schema
            ?? throw new ReplayDivergenceException("record has no leading header");
        var failure = Read<FailureRecord>(line, type, schema);
        RequireEqual(games, failure.Game, "failure game index");
        games++;
        rounds += failure.Round;
        AddMetrics(failure.Metrics, ref cardsPlayed, ref playerAttacks,
            ref payments, ref resourceAbilities);
        AddFailure(failure, signatures, ref failures);
    }

    private SimulationSummary Finish()
    {
        if (found is null)
        {
            throw new SimulationUsageException("record has no summary");
        }
        if (header is null)
        {
            throw new ReplayDivergenceException("record has no header");
        }
        RequireEqual(header.Seeds.Count, games, "recorded game count");

        var rebuilt = new SummaryRecord(
            "summary", games, playerWins, villainWins, playerLosses, failures,
            decisions, rounds, cardsPlayed, playerAttacks, payments,
            resourceAbilities, signatures);
        RequireEqual(
            JsonSerializer.Serialize(rebuilt, RecordJson.Options),
            JsonSerializer.Serialize(found, RecordJson.Options),
            "aggregate summary");
        return new SimulationSummary(
            rebuilt.Games, rebuilt.PlayersWin, rebuilt.VillainWins,
            rebuilt.PlayersLose, rebuilt.Failures, rebuilt.Decisions,
            rebuilt.Rounds, rebuilt.CardsPlayed, rebuilt.PlayerAttacks,
            rebuilt.Payments, rebuilt.ResourceAbilitiesUsed,
            rebuilt.FailureSignatures);
    }
}
