using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>One atomic campaign aggregate, with protected entry authority and game capabilities.</summary>
public sealed record StoredCampaign([property: JsonRequired] CampaignSave Save,
    [property: JsonRequired] string OwnerVerifier, [property: JsonRequired] StoredEntryReceipt Entry,
    [property: JsonRequired] ImmutableArray<CampaignAttemptAuthority> Authorities,
    [property: JsonRequired] ImmutableArray<CampaignReceipt> Receipts)
{
    /// <summary>Operational archive status; absent in older archives means active.</summary>
    public bool Archived { get; init; }
}
