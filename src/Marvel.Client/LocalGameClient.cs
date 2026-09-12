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
public static class LocalGameSession
{
    /// <summary>The opaque game id sent through either transport.</summary>
    public const string GameId = "local-core-game";
}
