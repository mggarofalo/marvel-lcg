using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>An authenticated capability and its authorized view of one session.</summary>
internal sealed record SessionAccess(HostedSession Session, ViewScope Scope, bool Owner);
