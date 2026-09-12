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
/// What the client can prove happened to one submitted mutation. These states
/// are a client contract chosen by this project; the game rules do not define transport recovery.
/// </summary>
public enum ClientMutationDisposition
{
    /// <summary>The request did not reach the game service.</summary>
    NotSent,

    /// <summary>The game service accepted the decision.</summary>
    Accepted,

    /// <summary>The game service refused the decision without applying it.</summary>
    Rejected,

    /// <summary>The client cannot prove whether the decision was applied.</summary>
    Uncertain,
}
