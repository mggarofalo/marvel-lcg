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

/// <summary>Whether the bearer session can still be used.</summary>
public enum ClientSessionDisposition
{
    /// <summary>The session remains available for requests.</summary>
    Active,

    /// <summary>The service has established that the session is unavailable.</summary>
    Unavailable,
}
