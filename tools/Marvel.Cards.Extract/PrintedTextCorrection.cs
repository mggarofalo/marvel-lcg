using System.Text.Json;

namespace Marvel.Cards.Extract;

/// <summary>Reads an explicitly sourced replacement for upstream printed text.</summary>
public static class PrintedTextCorrection
{
    /// <summary>Null means that the correction does not replace printed text.</summary>
    public static string? Read(JsonElement entry)
    {
        if (!entry.TryGetProperty("text", out var text))
        {
            return null;
        }

        // The supplement format requires a source for a replacement, including
        // an empty replacement. This is an acquisition policy, not a game rule.
        if (text.ValueKind != JsonValueKind.String
            || !entry.TryGetProperty("authority", out var authority)
            || authority.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(authority.GetString()))
        {
            throw new InvalidDataException(
                "a printed-text correction requires string text and a nonempty authority");
        }

        return text.GetString()!;
    }
}
