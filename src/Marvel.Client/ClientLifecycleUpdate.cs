using Marvel.Server;

namespace Marvel.Client;

/// <summary>A visibility-safe completed operation and its explicit draft instruction.</summary>
public sealed record ClientLifecycleUpdate(
    EngineResponse? Response,
    GameProgressPresentation Progress,
    ClientDraftDisposition Draft,
    ClientStartupError? Error = null,
    bool MutationAccepted = false,
    ClientMutationDisposition? MutationDisposition = null);
