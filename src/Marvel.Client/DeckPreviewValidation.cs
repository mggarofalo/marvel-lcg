using Marvel.View;

namespace Marvel.Client;

/// <summary>Admits complete printed previews without live targets, counters or state.</summary>
internal static class DeckPreviewValidation
{
    internal static bool Complete(BoardCardPresentation? face, string id)
    {
        return face is { TargetId: null, Concealed: false, Persistent: null, Fields.Count: 0,
            EffectiveValues.Count: 0, Counters.Count: 0, Statuses.Count: 0 }
            && face.FaceId == id && Text(face) && Printed(face);
    }

    private static bool Text(BoardCardPresentation face) => face is
    {
        Title: not null, Subtitle: not null, Kind: not null, Back: not null,
        Classification: not null, RulesText: not null, RulesMarkup: not null,
    };

    private static bool Printed(BoardCardPresentation face) => face.Traits is not null && face.Traits.All(item => item is not null)
        && face.Keywords is not null && face.Keywords.All(item => item is not null)
        && face.PrintedStats is not null && face.PrintedStats.All(item => item is not null)
        && face.PrintedMarks is not null && face.PrintedMarks.All(item => item is not null);
}
