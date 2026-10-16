using System.Text.Json;
using Marvel.Core.Random;
using Marvel.Session;
using static Marvel.Sim.SimulationHarnessSupport;

namespace Marvel.Sim;

/// <summary>Validates research-record framing, schema and deterministic seed plans.</summary>
internal static class SimulationRecordContract
{
    internal const int RecordSchema = 5;

    internal static T Read<T>(string line, string expectedType, int schema = RecordSchema)
    {
        ValidateRecordSchema(schema);
        return JsonSerializer.Deserialize<T>(line, RecordJson.Options)
            ?? throw new JsonException($"{expectedType} record was null");
    }

    internal static void ValidateRecordSchema(int schema)
    {
        if (schema != RecordSchema)
            throw new SimulationUsageException($"record schema {schema} is not supported; expected {RecordSchema}");
    }

    internal static void ValidateHeaderSeeds(HeaderRecord header)
    {
        if (header.Seeds.Count == 0)
        {
            throw new ReplayDivergenceException("header contains no game seeds");
        }

        switch (header.SeedMode)
        {
            case "explicit": ValidateExplicitSeeds(header); break;
            case "consecutive": ValidateConsecutiveSeeds(header); break;
            case "random": ValidateRandomSeeds(header); break;
            default:
                throw new ReplayDivergenceException(
                    $"unknown seed mode '{header.SeedMode}'");
        }
    }

    private static void ValidateExplicitSeeds(HeaderRecord header)
    {
        if (header.SelectionSeed is not null)
            throw new ReplayDivergenceException("explicit seed mode records a selection seed");
    }

    private static void ValidateConsecutiveSeeds(HeaderRecord header)
    {
        bool overflows = (ulong)header.Seeds[0] + (ulong)header.Seeds.Count - 1 > uint.MaxValue;
        bool differs = header.Seeds.Where((seed, index) =>
            index > 0 && seed != header.Seeds[0] + (uint)index).Any();
        if (header.SelectionSeed is not null || overflows || differs)
            throw new ReplayDivergenceException("consecutive seed plan does not match its mode");
    }

    private static void ValidateRandomSeeds(HeaderRecord header)
    {
        if (header.SelectionSeed is not { } selectionSeed)
            throw new ReplayDivergenceException("random seed mode has no selection seed");
        var planner = new MersenneTwister(selectionSeed);
        var expected = Enumerable.Range(0, header.Seeds.Count)
            .Select(_ => planner.NextUInt32()).ToList();
        RequireSequence(expected, header.Seeds, "random seed plan");
    }

    internal static string RecordType(JsonElement record)
    {
        if (record.ValueKind != JsonValueKind.Object
            || !record.TryGetProperty("type", out var type)
            || type.ValueKind != JsonValueKind.String
            || type.GetString() is not { Length: > 0 } value)
        {
            throw new JsonException("record has no string 'type'");
        }

        return value;
    }
}
