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

/// <summary>
/// A newly opened or attached session, its render-safe view, and any invitations
/// that the entry screen must hand off before discarding them.
/// </summary>
public sealed record ClientEntryResult(
    ClientSession? Session,
    EngineResponse? Response,
    IReadOnlyList<SeatInvitation> Invitations,
    ClientStartupError? Error)
{
    /// <summary>Whether a complete authorized game view was returned.</summary>
    public bool Succeeded => Session is not null && Response is not null && Error is null;
}
