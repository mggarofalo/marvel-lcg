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

/// <summary>The result of discovering the authored setup surface.</summary>
public sealed record ClientSetupResult(
    SetupChoices? Choices,
    ClientStartupError? Error)
{
    /// <summary>Whether complete choices were returned.</summary>
    public bool Succeeded => Choices is not null && Error is null;
}
