using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Marvel.Session;

/// <summary>Strict bounded canonical JSON for campaign archives and replay equality checks.</summary>
public static class CampaignSaveJson
{
    /// <summary>The supported archive byte bound; excess history is refused, never truncated.</summary>
    public const int MaximumBytes = 256 * 1024 * 1024;
    private static JsonSerializerOptions Options => CreateOptions();

    /// <summary>Serializes one canonical value; choices of property names are this application's format.</summary>
    public static string Canonical<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Writes a complete structurally checked archive.</summary>
    public static byte[] Write(CampaignSave save)
    {
        Validate(save);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(save, Options);
        if (bytes.Length > MaximumBytes) throw new SessionSaveException("campaign archive exceeds its supported size");
        return bytes;
    }

    /// <summary>Rejects duplicate or unknown fields, invalid encodings and unsupported envelope shapes.</summary>
    public static CampaignSave Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes) throw new SessionSaveException("campaign archive exceeds its supported size");
        try
        {
            StrictJsonMembers.Validate(bytes);
            var save = JsonSerializer.Deserialize<CampaignSave>(bytes, Options)
                ?? throw new SessionSaveException("campaign archive is empty");
            Validate(save);
            return save;
        }
        catch (Exception failure) when (failure is JsonException or ArgumentException)
        {
            throw new SessionSaveException("campaign archive is malformed", failure);
        }
    }

    /// <summary>Reads a strictly bounded enclosing campaign document using the canonical contract.</summary>
    public static T ReadDocument<T>(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes) throw new SessionSaveException("campaign document exceeds size limit");
        try
        {
            StrictJsonMembers.Validate(bytes);
            return JsonSerializer.Deserialize<T>(bytes, Options)
                ?? throw new SessionSaveException("campaign document is empty");
        }
        catch (JsonException failure) { throw new SessionSaveException("campaign document is malformed", failure); }
    }

    private static void Validate(CampaignSave save)
    {
        if (save.Format != CampaignSave.FormatName || save.Schema != CampaignSave.CurrentSchema)
            throw new SessionSaveException("campaign save version is unsupported");
        if (save.Compatibility is null || save.Identity is null || save.Configuration is null || save.State is null)
            throw new SessionSaveException("campaign save lacks required records");
        ValidateDimensions(save);
        ValidateState(save);
        ValidateHistory(save);
    }

    private static void ValidateHistory(CampaignSave save)
    {
        if (save.Journal.Any(entry => entry is null) || save.Attempts.Any(attempt => attempt is null))
            throw new SessionSaveException("campaign history contains an empty record");
        if (save.Configuration.Players.Any(player => player is null || player.Deck is null))
            throw new SessionSaveException("campaign roster contains an empty record");
        foreach (var attempt in save.Attempts) SessionSaveJson.Validate(attempt);
        if (save.Attempts.Select(attempt => attempt.Session.StorageId).Distinct(StringComparer.Ordinal).Count() != save.Attempts.Length
            || save.Attempts.Select(attempt => attempt.Setup.Campaign?.Attempt).Distinct().Count() != save.Attempts.Length)
            throw new SessionSaveException("campaign history contains a duplicate attempt");
    }

    private static void ValidateDimensions(CampaignSave save)
    {
        if (save.Revision < 0 || save.Journal.IsDefault || save.Attempts.IsDefault
            || save.Configuration.Players.IsDefault)
            throw new SessionSaveException("campaign save dimensions are invalid");
        if (save.Journal.Length > 100000 || save.Attempts.Length > 1000)
            throw new SessionSaveException("campaign history exceeds supported bounds");
    }

    private static void ValidateState(CampaignSave save)
    {
        var state = save.State;
        if (state.Players.IsDefaultOrEmpty || state.Players.Length > 4 || state.RewardSeats.IsDefault
            || state.RemovedCardKeys.IsDefault || state.BannedAllyFaces.IsDefault || state.ExperimentalWeapons.IsDefault)
            throw new SessionSaveException("campaign state dimensions are invalid");
        foreach (var player in state.Players) ValidatePlayer(player);
    }

    private static void ValidatePlayer(Marvel.Rules.Campaign.CampaignPlayerState player)
    {
            if (player is null || player.Deck is null || player.Cards.IsDefault || player.AvailableObligations.IsDefault)
                throw new SessionSaveException("campaign player is malformed");
    }

    private static DefaultJsonTypeInfoResolver RequiredCampaignMembers()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type.Namespace == "Marvel.Rules.Campaign" || info.Type.Name.StartsWith("Campaign", StringComparison.Ordinal))
                foreach (var property in info.Properties) property.IsRequired = true;
        });
        return resolver;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 128,
            TypeInfoResolver = RequiredCampaignMembers(),
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

