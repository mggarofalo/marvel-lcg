using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Names an offered resource source using only the authorized snapshot.</summary>
public sealed record PaymentSourcePresentation(int Id, string Name, string Resources, bool DiscardsCard, string Reference)
{
    /// <summary>Describes the offered source; its availability and output remain engine-owned.</summary>
    public static PaymentSourcePresentation From(ResourceSource source, WorldDescriptor world)
    {
        foreach (AreaDescriptor area in world.Areas)
        {
            CardDescriptor? card = area.Cards.FirstOrDefault(candidate => candidate.Id == source.Effect);
            if (card?.Face is not { } face) continue;
            return new(source.Effect, face.Title, source.Generates,
                area.Zone == "HandsArea", face.RulesText);
        }
        return new(source.Effect, "Resource ability", source.Generates, false, string.Empty);
    }
}
