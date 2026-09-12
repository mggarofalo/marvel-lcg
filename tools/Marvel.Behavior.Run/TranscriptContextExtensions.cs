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

internal static class TranscriptContextExtensions
{
    public static CanonicalCoreScene SceneRequired(
        this TranscriptContext context, TranscriptStep step) =>
        context.Scene ?? throw new TranscriptException(
            $"{step.Location}: a canonical Core scene must be dealt first");
}
