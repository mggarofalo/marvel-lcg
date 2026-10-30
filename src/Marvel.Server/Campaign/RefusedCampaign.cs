using Marvel.Protocol;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>The authority a refused campaign archive still answers to, and why it was refused.</summary>
internal sealed record RefusedCampaign(
    string OwnerVerifier, string[] GameIds, string[] GameEntryVerifiers, string Category)
{
    internal static RefusedCampaign Of(StoredCampaign campaign, SessionCompatibilityException failure) => new(
        campaign.OwnerVerifier,
        [.. campaign.Save.Attempts.Select(attempt => attempt.Session.Label)],
        [.. campaign.Authorities.SelectMany(attempt => attempt.Authorities)
            .Select(authority => authority.Entry?.Verifier).OfType<string>().Distinct(StringComparer.Ordinal)],
        failure.Category);

    internal bool Holds(EngineRequest request) =>
        GameIds.Contains(request.GameId, StringComparer.Ordinal)
        || EntryReceipts.Valid(request.Entry)
            && GameEntryVerifiers.Contains(EntryReceipts.Verifier(request.Entry!), StringComparer.Ordinal);
}
