using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>One frozen schema 2 decision and its derived replay facts.</summary>
internal sealed record SchemaTwoJournalStep(
    [property: JsonRequired] JsonElement Prompt,
    [property: JsonRequired] JsonElement Decision,
    [property: JsonRequired] IReadOnlyList<JsonElement> Events,
    [property: JsonRequired] long RngWords,
    [property: JsonRequired] string StateFingerprint,
    [property: JsonRequired] EngineResultRecord? Result);
