using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal static class RecordJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    public static void Write(TextWriter writer, object record)
    {
        writer.WriteLine(JsonSerializer.Serialize(record, record.GetType(), Options));
        writer.Flush();
    }
}
