using System.Security.Cryptography;

namespace Marvel.Protocol;

/// <summary>A client-retained secret proving ownership of one retriable entry operation.</summary>
public sealed record EntryClaim(string Id, string Secret)
{
    /// <summary>Creates transport authority independently of deterministic game randomness.</summary>
    public static EntryClaim Create() => new(Guid.NewGuid().ToString("N"),
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    /// <summary>Omits the recovery secret from diagnostics.</summary>
    public override string ToString() => $"EntryClaim {{ Id = {Id} }}";
}
