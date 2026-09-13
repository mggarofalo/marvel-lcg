using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A one-time capability that may become authenticated session access.</summary>
internal sealed record PendingInvitation(HostedSession Session, ViewScope Scope);
