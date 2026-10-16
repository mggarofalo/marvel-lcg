using System.Security.Cryptography;
using System.Text;
using Marvel.Protocol;

namespace Marvel.Server;

/// <summary>Protects issued entry authority with a key that only the retriable client request supplies.</summary>
internal static class EntryReceipts
{
    public static bool Valid(EntryClaim? claim) => claim is { Id.Length: > 0 and <= 256, Secret.Length: 64 }
        && claim.Secret.All(char.IsAsciiHexDigit);

    public static bool Valid(StoredEntryReceipt? receipt)
    {
        if (receipt is null) return true;
        if (!Digest(receipt.Verifier) || !Digest(receipt.Fingerprint)) return false;
        try
        {
            return Convert.FromBase64String(receipt.Nonce).Length == 12
                && Convert.FromBase64String(receipt.Tag).Length == 16
                && Convert.FromBase64String(receipt.Ciphertext).Length is > 0 and <= 65536;
        }
        catch (Exception failure) when (failure is FormatException or ArgumentNullException) { return false; }
    }

    private static bool Digest(string? value) => value is { Length: 64 }
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    public static string Verifier(EntryClaim claim) => Hash(Encoding.UTF8.GetBytes(claim.Id + ":" + claim.Secret));

    public static string Fingerprint(EngineRequest request) =>
        Hash(EngineJson.Write(request with { RequestId = string.Empty, Entry = null }));

    public static StoredEntryReceipt Protect(EngineRequest request, EngineResponse response)
    {
        EntryClaim claim = request.Entry!;
        byte[] plaintext = EngineJson.Write(response with { Prompt = null, World = null, Events = [], History = null });
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipher = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        string fingerprint = Fingerprint(request);
        using var aes = new AesGcm(Convert.FromHexString(claim.Secret), tag.Length);
        aes.Encrypt(nonce, plaintext, cipher, tag, Encoding.UTF8.GetBytes(fingerprint));
        CryptographicOperations.ZeroMemory(plaintext);
        return new(Verifier(claim), fingerprint, Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipher), Convert.ToBase64String(tag));
    }

    public static EngineResponse Recover(EngineRequest request, StoredEntryReceipt receipt)
    {
        byte[] cipher = Convert.FromBase64String(receipt.Ciphertext);
        byte[] plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(Convert.FromHexString(request.Entry!.Secret), 16);
        aes.Decrypt(Convert.FromBase64String(receipt.Nonce), cipher, Convert.FromBase64String(receipt.Tag),
            plaintext, Encoding.UTF8.GetBytes(receipt.Fingerprint));
        try { return EngineJson.ReadResponse(plaintext); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}
