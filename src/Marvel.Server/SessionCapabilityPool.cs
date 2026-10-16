using Marvel.Protocol;

namespace Marvel.Server;

/// <summary>Serializes token issuance and reserves values before any concurrent publication.</summary>
internal sealed class SessionCapabilityPool(ISessionCapabilityIssuer issuer)
{
    private readonly object gate = new();
    private readonly HashSet<string> issued = new(StringComparer.Ordinal);

    public string Issue(Func<string, bool> exists, HashSet<string>? reserved)
    {
        lock (gate)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                string capability = issuer.Issue();
                string verifier = SessionAuthorityRegistry.Verifier(capability);
                if (capability.Length is > 0 and <= EngineProtocol.MaximumIdentifierLength
                    && !exists(verifier) && !(reserved?.Contains(capability) ?? false) && issued.Add(verifier))
                    return capability;
            }
        }
        throw new InvalidOperationException("could not issue a unique session capability");
    }
}
