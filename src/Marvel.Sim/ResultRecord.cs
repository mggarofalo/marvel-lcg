using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record ResultRecord(
    string Type,
    int Game,
    uint Seed,
    string Outcome,
    int Round,
    int Decisions,
    PolicyMetrics Metrics,
    string TerminalDigest);
