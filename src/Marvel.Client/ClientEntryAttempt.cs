using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>Retains the exact entry request and secret needed to recover an uncertain response.</summary>
public sealed class ClientEntryAttempt
{
    internal ClientEntryAttempt(EngineRequest request) => Request = EngineJson.Snapshot(request);
    internal EngineRequest Request { get; }
    /// <summary>The opaque destination game label.</summary>
    public string GameId => Request.GameId;
    /// <summary>Whether this attempt redeems a seat invitation.</summary>
    public bool IsAttachment => Request.Operation == EngineProtocol.Attach;
    /// <summary>The idempotent operation identity, safe to display in diagnostics.</summary>
    public string OperationId => Request.Entry!.Id;
}
