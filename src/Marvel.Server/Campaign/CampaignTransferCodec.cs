using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>A portable, bounded save envelope without server or client credentials.</summary>
internal static class CampaignTransferCodec
{
    // Product format choice: compressed transfers fit the existing 4 MiB socket frame.
    internal const int MaximumCharacters = 3 * 1024 * 1024;

    internal static string Export(CampaignSave save)
    {
        byte[] bytes = CampaignSaveJson.Write(save);
        using var output = new MemoryStream();
        using (var compressed = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            compressed.Write(bytes);
        string result = "marvel-campaign-transfer:1\n" + Convert.ToHexString(SHA256.HashData(bytes))
            + "\n" + Convert.ToBase64String(output.ToArray());
        if (result.Length > MaximumCharacters)
            throw new ArgumentException("campaign export exceeds the supported transfer size");
        return result;
    }

    internal static CampaignSave Import(string value)
    {
        if (value.Length > MaximumCharacters) throw new ArgumentException("campaign transfer is oversized");
        string[] parts = value.Split('\n', 3);
        if (parts.Length != 3 || parts[0] != "marvel-campaign-transfer:1" || parts[1].Length != 64)
            throw new ArgumentException("campaign transfer format is unsupported");
        using var input = new MemoryStream(Convert.FromBase64String(parts[2]));
        using var compressed = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        byte[] buffer = new byte[65536];
        int read;
        while ((read = compressed.Read(buffer)) != 0)
        {
            if (output.Length + read > CampaignSaveJson.MaximumBytes)
                throw new ArgumentException("expanded campaign transfer is oversized");
            output.Write(buffer, 0, read);
        }
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != parts[1])
            throw new ArgumentException("campaign transfer checksum failed");
        return CampaignSaveJson.Read(bytes);
    }
}
