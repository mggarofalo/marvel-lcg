using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Issues capabilities from operating-system entropy outside gameplay.</summary>
internal sealed class CryptographicCapabilityIssuer : ISessionCapabilityIssuer
{
    public string Issue() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
