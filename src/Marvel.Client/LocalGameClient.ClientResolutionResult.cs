using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>The client-owned label for the one local table.</summary>

/// <summary>A resolved decision, optionally paired with a recovered current view.</summary>
public sealed record ClientResolutionResult(
    EngineResponse? Response,
    ClientStartupError? Error,
    ClientMutationDisposition MutationDisposition = ClientMutationDisposition.Accepted,
    ClientSessionDisposition SessionDisposition = ClientSessionDisposition.Active)
{
    /// <summary>Whether the submitted decision was accepted.</summary>
    public bool Succeeded => MutationDisposition == ClientMutationDisposition.Accepted
        && Response is not null
        && Error is null;

    /// <summary>Whether an authoritative view is available for rendering.</summary>
    public bool HasAuthoritativeView => Response is not null;
}
