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

/// <summary>The result of reading the current authoritative session view.</summary>
public sealed record ClientSynchronizationResult(
    EngineResponse? Response,
    ClientStartupError? Error,
    ClientSessionDisposition SessionDisposition)
{
    /// <summary>Whether a complete current view was returned.</summary>
    public bool Succeeded => Response is not null && Error is null;

    /// <summary>Whether an authoritative view is available for rendering.</summary>
    public bool HasAuthoritativeView => Response is not null;
}
