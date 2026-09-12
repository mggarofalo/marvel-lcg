using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record PolicyMetrics(
    int CardsPlayed,
    int PlayerAttacks,
    int Payments,
    int ResourceAbilitiesUsed);
