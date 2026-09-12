using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptVocabulary
{
    internal static IReadOnlyList<TranscriptBinding> All() =>
    [
        .. GivenTranscriptVocabulary.Bindings(),
        .. WhenTranscriptVocabulary.Bindings(),
        .. ThenTranscriptVocabulary.Bindings(),
    ];
}

internal static class TranscriptBindingFactory
{
    internal static TranscriptBinding Bind(
        string name,
        TranscriptStepKind kind,
        string pattern,
        Action<TranscriptContext, TranscriptStep, Match> execute) =>
        new(name, kind, new Regex(
            $"\\A{pattern}\\z",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            CoreTranscriptRunner.PatternTimeout), execute);
}
