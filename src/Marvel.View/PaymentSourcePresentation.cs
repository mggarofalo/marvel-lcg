using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Names an offered resource source using only the authorized snapshot.</summary>
public sealed record PaymentSourcePresentation(int Id, string Name, string Resources, bool DiscardsCard, string Reference)
{
    /// <summary>Authorized display markup preserves the printed resource symbols.</summary>
    public string ReferenceMarkup { get; init; } = string.Empty;

    /// <summary>The exact offered resource ability, when supplied by the engine.</summary>
    public string? AbilityName { get; init; }

    /// <summary>Authorized non-resource commitments of selecting this source.</summary>
    public string? CostDescription { get; init; }

    /// <summary>Describes the offered source; its availability and output remain engine-owned.</summary>
    public static PaymentSourcePresentation From(ResourceSource source, WorldDescriptor world)
    {
        foreach (AreaDescriptor area in world.Areas)
        {
            CardDescriptor? card = area.Cards.FirstOrDefault(candidate => candidate.Id == source.Effect);
            if (card?.Face is not { } face) continue;
            return new(source.Effect, AffordanceSourcePresentation.Name(area, card, face.Title), source.Generates,
                area.Zone == "HandsArea", face.RulesText)
            {
                ReferenceMarkup = face.RulesMarkup,
                AbilityName = source.AbilityName,
                CostDescription = source.CostDescription,
            };
        }
        return new(source.Effect, "Resource ability", source.Generates, false, string.Empty);
    }
}
