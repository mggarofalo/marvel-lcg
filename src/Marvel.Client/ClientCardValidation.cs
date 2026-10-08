using Marvel.View;

namespace Marvel.Client;

/// <summary>Validates readable card facts before they enter reusable client state.</summary>
internal static class ClientCardValidation
{
    internal static bool Complete(CardFaceDescriptor face) =>
        face.Id is not null && face.Title is not null && face.Subtitle is not null
        && face.Fields is not null && CompletePrintedValues(face.PrintedValues)
        && CompleteEffectiveValues(face.EffectiveValues);

    private static bool CompletePrintedValues(IReadOnlyDictionary<string, CardPrintedValue>? values) =>
        values is not null && values.All(pair =>
            !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null
            && !string.IsNullOrWhiteSpace(pair.Value.Value)
            && pair.Value.ConsequentialDamage >= 0);

    private static bool CompleteEffectiveValues(IReadOnlyDictionary<string, CardEffectiveValue>? values) =>
        values is not null && values.All(pair =>
            !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null
            && !string.IsNullOrWhiteSpace(pair.Value.BaseKind)
            && pair.Value.Calculation is not null
            && pair.Value.Calculation.All(CompleteCalculation));

    private static bool CompleteCalculation(CardValueCalculation? calculation) =>
        calculation is not null && !string.IsNullOrWhiteSpace(calculation.Operation)
        && (calculation.Source is null || CompleteSource(calculation.Source));

    internal static bool CompleteSource(CardValueSourceDescriptor source) =>
        !string.IsNullOrWhiteSpace(source.FaceId) && source.Title is not null
        && source.RulesText is not null && source.RulesMarkup is not null
        && (!source.Historical || source.CardId is null);
}
