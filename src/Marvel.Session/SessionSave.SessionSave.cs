using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>Schema 3's complete, capability-free deterministic session authority.</summary>
public sealed record SessionSave(
    [property: JsonRequired] string Format,
    [property: JsonRequired] int Schema,
    [property: JsonRequired] SessionCompatibility Compatibility,
    [property: JsonRequired] SessionIdentity Session,
    [property: JsonRequired] SessionSetup Setup,
    [property: JsonRequired] InitialRecord Initial,
    [property: JsonRequired] long Revision,
    [property: JsonRequired] int Cursor,
    [property: JsonRequired] int EditFrontier,
    [property: JsonRequired] PromptRecord? CurrentPrompt,
    [property: JsonRequired] IReadOnlyList<JournalUnit> Units)
{
    /// <summary>The required schema family marker.</summary>
    public const string FormatName = "marvel-session";

    /// <summary>The schema this runtime writes; schema 2 is read only for migration.</summary>
    public const int CurrentSchema = 3;

    /// <summary>Creates the zero-decision authority for a freshly dealt game.</summary>
    public static SessionSave Open(
        SessionCompatibility compatibility,
        string storageId,
        string label,
        SessionSetup setup,
        Game game,
        IReadOnlyList<GameEvent> setupEvents) =>
        new(
            FormatName,
            CurrentSchema,
            compatibility,
            new SessionIdentity(storageId, label, "active"),
            setup,
            new InitialRecord(
                [.. setupEvents.Select(JournalJson.Event)],
                game.State.Random.Generator.WordsConsumed,
                game.State.Digest().Canonical()),
            Revision: 0,
            Cursor: 0,
            EditFrontier: 0,
            game.Pending is null ? null : PromptRecord.From(game.Pending),
            []);
}
