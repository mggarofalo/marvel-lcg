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

/// <summary>A composed client, or why its engine connection could not be configured.</summary>
public sealed record LocalClientConnection(
    LocalGameClient? Client,
    ClientStartupError? Error)
{
    /// <summary>Whether the configured engine transport is available.</summary>
    public bool Succeeded => Client is not null && Error is null;
}
