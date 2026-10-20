using System.Collections.Immutable;
using Marvel.Rules.Campaign;

namespace Marvel.Session;

/// <summary>A complete campaign authority, including every retained game attempt.</summary>
public sealed record CampaignSave(
    string Format,
    int Schema,
    SessionCompatibility Compatibility,
    SessionIdentity Identity,
    CampaignConfiguration Configuration,
    long Revision,
    CampaignState State,
    ImmutableArray<CampaignJournalEntry> Journal,
    ImmutableArray<SessionSave> Attempts)
{
    /// <summary>The explicit campaign envelope identity, distinct from a single session.</summary>
    public const string FormatName = "marvel-campaign";

    /// <summary>The supported campaign envelope version.</summary>
    public const int CurrentSchema = 1;

    /// <summary>Creates a campaign with no attempts or accepted choices.</summary>
    public static CampaignSave Create(
        SessionCompatibility compatibility, SessionIdentity identity,
        CampaignConfiguration configuration, CampaignDefinition definition) => new(
            FormatName, CurrentSchema, compatibility, identity, configuration, 0,
            CampaignRules.Create(definition, configuration.Players, configuration.Expert, configuration.Seed), [], []);
}
