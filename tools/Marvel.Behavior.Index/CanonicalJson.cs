using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marvel.Behavior.Index;

/// <summary>RFC 8785 JSON Canonicalization Scheme for authority fingerprints.</summary>
internal static class CanonicalJson
{
    /// <summary>Hashes one JSON subtree as the behavioral contract specifies.</summary>
    public static string Hash(JsonElement value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Serialize(value));
        return "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    /// <summary>Serializes one JSON value to JCS bytes, returned as their UTF-16 text.</summary>
    public static string Serialize(JsonElement value)
    {
        var builder = new StringBuilder();
        Append(value, builder);
        return builder.ToString();
    }

    private static void Append(JsonElement value, StringBuilder builder)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                bool firstProperty = true;
                foreach (var property in value.EnumerateObject()
                    .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    if (!firstProperty)
                    {
                        builder.Append(',');
                    }

                    firstProperty = false;
                    AppendString(property.Name, builder);
                    builder.Append(':');
                    Append(property.Value, builder);
                }

                builder.Append('}');
                break;

            case JsonValueKind.Array:
                builder.Append('[');
                bool firstItem = true;
                foreach (var item in value.EnumerateArray())
                {
                    if (!firstItem)
                    {
                        builder.Append(',');
                    }

                    firstItem = false;
                    Append(item, builder);
                }

                builder.Append(']');
                break;

            case JsonValueKind.String:
                AppendString(value.GetString()!, builder);
                break;

            case JsonValueKind.Number:
                AppendNumber(value, builder);
                break;

            case JsonValueKind.True:
                builder.Append("true");
                break;

            case JsonValueKind.False:
                builder.Append("false");
                break;

            case JsonValueKind.Null:
                builder.Append("null");
                break;

            default:
                throw new InvalidDataException(
                    $"JCS cannot serialize JSON kind {value.ValueKind}");
        }
    }

    private static void AppendNumber(JsonElement value, StringBuilder builder)
    {
        // Every number in the current authority subtrees is an integer. Failing
        // closed on a later decimal is intentional: substituting .NET's number
        // spelling for ECMAScript's JCS spelling would silently change a wire
        // format. Add the full RFC number algorithm with a pinned vector when
        // an authority first needs it.
        if (!value.TryGetInt64(out long integer))
        {
            throw new InvalidDataException(
                $"JCS authority fingerprint does not yet support number {value.GetRawText()}");
        }

        builder.Append(integer.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendString(string value, StringBuilder builder)
    {
        ValidateUtf16(value);
        builder.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    if (character <= 0x1f)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private static void ValidateUtf16(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new InvalidDataException("JCS input contains an unpaired high surrogate");
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw new InvalidDataException("JCS input contains an unpaired low surrogate");
            }
        }
    }
}
