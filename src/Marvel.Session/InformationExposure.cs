using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>One bounded reason that committed history became unsafe to erase.</summary>
public sealed record InformationExposure(
    [property: JsonRequired] string Reason,
    [property: JsonRequired] IReadOnlyList<int> Seats);
