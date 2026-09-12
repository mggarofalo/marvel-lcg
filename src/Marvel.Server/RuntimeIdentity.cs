using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Non-secret runtime identities available before a game is opened.</summary>
public sealed record RuntimeIdentity(
    string ProductVersion,
    string Commit,
    string ReplayContract,
    string RngContract,
    string StateDigest,
    int Protocol,
    int SaveSchema,
    string CardsSha256,
    string SetupSha256,
    string AbilitiesSha256);
