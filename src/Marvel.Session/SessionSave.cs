using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>The complete, capability-free deterministic session authority.</summary>
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

    /// <summary>The single schema this runtime reads and writes.</summary>
    public const int CurrentSchema = 7;

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
            setup.Snapshot(),
            new InitialRecord(
                JournalValues.List(setupEvents.Select(JournalJson.Event)),
                game.State.Random.Generator.WordsConsumed,
                game.State.Digest().Canonical()),
            Revision: 0,
            Cursor: 0,
            EditFrontier: 0,
            game.Pending is null ? null : PromptRecord.From(game.Pending),
            []);
}
