using System.Text.Json;
using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
namespace Marvel.Desktop;

/// <summary>Persists campaign access separately from public deck files and deterministic server saves.</summary>
public sealed class CampaignConnectionStore : IDisposable
{
    private readonly FileCampaignAccessStore files;
    /// <summary>Opens the private per-user campaign connection directory.</summary>
    public CampaignConnectionStore(string root) => files = new(root);

    /// <summary>Lists recoverable local connections, preserving unreadable files without replacing them.</summary>
    public IReadOnlyList<CampaignConnection> Load()
    {
        var result = new List<CampaignConnection>();
        foreach (string id in files.Ids())
        {
            try { result.Add(Read(id)); }
            catch (Exception failure) when (failure is IOException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException) { }
        }
        return result;
    }

    /// <summary>Flushes the exact pending request before it may be sent to the service.</summary>
    public void Save(CampaignConnection connection)
    {
        using var bytes = new MemoryStream();
        using (var json = new Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("schema", 1);
            json.WriteString("local_id", connection.LocalId);
            json.WriteString("endpoint", connection.Endpoint);
            json.WriteString("label", connection.Label);
            json.WriteString("campaign_id", connection.CampaignId);
            json.WriteString("capability", connection.Capability);
            json.WriteString("game_id", connection.Game?.GameId);
            json.WriteString("game_capability", connection.Game?.Capability);
            json.WritePropertyName("pending");
            if (connection.Pending is null) json.WriteNullValue();
            else json.WriteRawValue(EngineJson.Write(connection.Pending));
            json.WritePropertyName("snapshot");
            if (connection.Snapshot is null) json.WriteNullValue();
            else json.WriteRawValue(EngineJson.Write(new EngineResponse(EngineProtocol.Version, "cache",
                connection.CampaignId!, null, null, []) { Campaign = connection.Snapshot }));
            if (connection.Refused) json.WriteBoolean("refused", true);
            json.WriteEndObject();
        }
        files.Write(connection.LocalId, bytes.ToArray());
    }
    /// <summary>Removes access only after authoritative permanent deletion is confirmed.</summary>
    public void Delete(string id) => files.Delete(id);

    private CampaignConnection Read(string id)
    {
        using var document = JsonDocument.Parse(files.Read(id));
        var json = document.RootElement;
        if (json.GetProperty("schema").GetInt32() != 1 || json.GetProperty("local_id").GetString() != id)
            throw new JsonException("unsupported campaign connection");
        string? game = json.GetProperty("game_id").GetString();
        string? secret = json.GetProperty("game_capability").GetString();
        var pending = json.GetProperty("pending");
        return new(id, json.GetProperty("endpoint").GetString()!, json.GetProperty("label").GetString()!,
            json.GetProperty("campaign_id").GetString(), json.GetProperty("capability").GetString(),
            game is null || secret is null ? null : new(game, secret),
            pending.ValueKind == JsonValueKind.Null ? null : EngineJson.ReadRequest(System.Text.Encoding.UTF8.GetBytes(pending.GetRawText())))
        {
            Snapshot = SnapshotOf(json),
            Refused = json.TryGetProperty("refused", out var refused) && refused.GetBoolean(),
        };
    }

    private static CampaignDescriptor? SnapshotOf(JsonElement json) =>
        json.TryGetProperty("snapshot", out var snapshot) && snapshot.ValueKind != JsonValueKind.Null
            ? EngineJson.ReadResponse(System.Text.Encoding.UTF8.GetBytes(snapshot.GetRawText())).Campaign : null;

    /// <summary>Releases the local writer lease.</summary>
    public void Dispose() => files.Dispose();
}
