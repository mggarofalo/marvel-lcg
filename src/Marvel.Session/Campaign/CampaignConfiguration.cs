using System.Collections.Immutable;
using Marvel.Rules.Campaign;

namespace Marvel.Session;

/// <summary>Admitted immutable inputs from which the complete campaign is replayed.</summary>
public sealed record CampaignConfiguration(
    string Product, bool Expert, uint Seed, ImmutableArray<CampaignRosterEntry> Players);
