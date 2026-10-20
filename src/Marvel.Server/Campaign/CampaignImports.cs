using System.Collections.Immutable;
using Marvel.Protocol;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>Verifies portable saves and publishes independent copies with fresh authority.</summary>
internal static class CampaignImports
{
    internal static EngineResponse Create(CampaignHost host, EngineRequest request)
    {
        var imported = CampaignTransferCodec.Import(request.Campaign!.Transfer
            ?? throw new ArgumentException("a campaign file is required"));
        imported = CampaignContentUpgrade.Verify(host.Factory, imported);
        Verify(host, imported);
        var save = Remap(imported);
        Verify(host, save);
        string capability = new CryptographicCapabilityIssuer().Issue();
        var authorities = ImmutableArray.CreateBuilder<CampaignAttemptAuthority>();
        var prepared = new List<(StoredSession Stored, Marvel.Rules.Play.Game Game)>();
        CampaignGameAccess? current = null;
        foreach (var attempt in save.Attempts)
        {
            string token = new CryptographicCapabilityIssuer().Issue();
            var game = SessionReplay.Verify(attempt, host.Factory.Compatibility,
                setup => CampaignTransactions.Open(host.Factory, setup));
            var scope = host.Games.visibility.Authorize(null, game.State.Players);
            var grants = SessionAuthorityRegistry.OpenAuthorities(token, scope, game.State.Players, []);
            var stored = new StoredSession(attempt, grants);
            host.Games.authority.ValidateForRestore(stored, game);
            prepared.Add((stored, game));
            authorities.Add(new(attempt.Session.StorageId, [.. grants]));
            if (attempt.Setup.Campaign!.Attempt == save.State.Attempt)
                current = new(attempt.Session.Label, token);
        }
        var response = CampaignHost.Success(request) with { Capability = capability, CampaignGame = current };
        var provisional = new StoredCampaign(save, SessionAuthorityRegistry.Verifier(capability),
            EntryReceipts.Protect(request, response), authorities.ToImmutable(), []);
        response = response with { Campaign = host.Describe(provisional) };
        var campaign = provisional with { Entry = EntryReceipts.Protect(request, response) };
        host.Publish(campaign);
        try
        {
            foreach (var item in prepared) host.Games.authority.PublishRestored(item.Stored, item.Game);
        }
        catch (Exception failure) { host.RequireRecovery(failure); }
        return response;
    }

    private static void Verify(CampaignHost host, CampaignSave save) =>
        CampaignReplay.Verify(save, host.Factory.CampaignDefinition!, host.Factory.Compatibility,
            setup => CampaignTransactions.Open(host.Factory, setup), host.Factory.InspectCampaign);

    private static CampaignSave Remap(CampaignSave original)
    {
        string id = EngineHostLifecycle.NewStorageId();
        var identities = original.Attempts.ToDictionary(attempt => attempt.Session.StorageId,
            attempt => new SessionIdentity(EngineHostLifecycle.NewStorageId(),
                id + ":" + attempt.Setup.Campaign!.Attempt, "active"), StringComparer.Ordinal);
        return original with
        {
            Identity = new(id, CampaignManagement.CopyName(original.Identity.Label), "active"),
            Attempts = [.. original.Attempts.Select(attempt => attempt with { Session = identities[attempt.Session.StorageId] })],
            Journal = [.. original.Journal.Select(entry => entry.Proof is { } proof
                ? entry with { Proof = proof with { Session = identities[proof.Session.StorageId] } } : entry)],
        };
    }
}
