using System.Text.RegularExpressions;
using Marvel.Rules.Events;

namespace Marvel.Behavior.Run;

// Departed copies have no live quantities. Assert the emitted change instead.
internal static class CoreTranscriptHistoricalState
{
    internal static IReadOnlyList<TranscriptBinding> Bindings() =>
    [
        Bind("historical-field-change", TranscriptStepKind.Then,
            "card (?<face>\\d+[a-z]?) copy (?<copy>\\d+) had field \"(?<field>[A-Za-z0-9_]+)\" changed from (?<from>\\d+) to (?<to>\\d+)",
            FieldChanged),
    ];

    private static void FieldChanged(TranscriptContext context, TranscriptStep step, Match match)
    {
        var card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!context.Events.OfType<FieldSet>().Any(change => change.Card == card.ObjectId
                && change.Field == match.Groups["field"].Value
                && change.From == Number(match, "from", step)
                && change.To == Number(match, "to", step)))
            throw new TranscriptAssertionException($"{step.Location}: the required historical field change was not emitted");
    }
}
