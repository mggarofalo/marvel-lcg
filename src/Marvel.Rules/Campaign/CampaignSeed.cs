using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Marvel.Rules.Campaign;

/// <summary>The versioned application choice deriving reproducible game seeds for campaign attempts.</summary>
public static class CampaignSeed
{
    /// <summary>Hashes a fixed ASCII domain and little-endian integers; no operational identity enters gameplay.</summary>
    public static uint ForAttempt(uint rootSeed, CampaignStage stage, int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attempt);
        byte[] domain = Encoding.ASCII.GetBytes("marvel-rors-attempt-v1");
        byte[] bytes = new byte[domain.Length + 12];
        domain.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(domain.Length), rootSeed);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(domain.Length + 4), (int)stage);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(domain.Length + 8), attempt);
        return BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(bytes));
    }
}
