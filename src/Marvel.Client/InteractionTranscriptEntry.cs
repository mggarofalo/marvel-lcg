using Marvel.Rules.Play;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Client;

/// <summary>One submitted decision or authorized engine response.</summary>
public sealed record InteractionTranscriptEntry(
    string Kind,
    long Revision,
    Decision? Decision = null,
    InteractionTranscriptResponse? Response = null,
    string? Disposition = null);
