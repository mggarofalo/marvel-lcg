using System.Text.Json;

namespace Marvel.Session;

/// <summary>Rejects duplicate JSON member names without relying on APIs newer than the target runtime.</summary>
public static class StrictJsonMembers
{
    /// <summary>Checks every object independently, including nested records and array elements.</summary>
    public static void Validate(ReadOnlySpan<byte> bytes)
    {
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 128 });
        var objects = new Stack<HashSet<string>>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    objects.Push(new HashSet<string>(StringComparer.Ordinal));
                    break;
                case JsonTokenType.EndObject:
                    objects.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    if (!objects.Peek().Add(reader.GetString()!))
                        throw new JsonException("duplicate JSON member");
                    break;
            }
        }
    }
}
