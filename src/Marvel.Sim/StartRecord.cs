using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record StartRecord(
    string Type,
    int Game,
    uint Seed,
    uint PolicySeed,
    IReadOnlyList<uint> SeatPolicySeeds,
    string InitialDigest,
    IReadOnlyList<JsonElement> SetupEvents);
