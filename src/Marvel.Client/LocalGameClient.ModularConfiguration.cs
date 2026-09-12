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

/// <summary>How the player wants to fill the scenario's modular-set selection.</summary>
public enum ModularConfiguration
{
    /// <summary>Use the scenario's authored recommendation.</summary>
    Recommended,

    /// <summary>Deliberately include no modular set.</summary>
    None,

    /// <summary>Use one or more explicitly selected authored modular sets.</summary>
    Selected,
}
