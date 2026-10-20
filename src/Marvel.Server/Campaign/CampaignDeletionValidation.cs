using Marvel.Session;
namespace Marvel.Server;

/// <summary>Rejects malformed durable revocations before they affect campaign access.</summary>
internal static class CampaignDeletionValidation
{
    internal static void Validate(CampaignDeletion deletion)
    {
        if (!Header(deletion))
            throw new SessionSaveException("campaign revocation header is invalid");
        if (deletion.GameIds is null || deletion.GameEntryVerifiers is null
            || deletion.GameIds.Any(id => id is not { Length: > 0 and <= 256 })
            || deletion.GameEntryVerifiers.Any(verifier => !Hex(verifier, 64)))
            throw new SessionSaveException("campaign revocation authorities are invalid");
    }

    private static bool Header(CampaignDeletion deletion) =>
        !(deletion.Schema != 1 || !Hex(deletion.StorageId, 32)
            || !Hex(deletion.OwnerVerifier, 64) || !Hex(deletion.Fingerprint, 64)
            || !Hex(deletion.EntryVerifier, 64)
            || deletion.RequestId is not { Length: > 0 and <= 256 });

    private static bool Hex(string? value, int length) =>
        value?.Length == length && value.All(char.IsAsciiHexDigit);
}
