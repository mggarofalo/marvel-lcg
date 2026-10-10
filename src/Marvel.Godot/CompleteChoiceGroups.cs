using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Organizes explicit offers by their authorized physical source.</summary>
internal static class CompleteChoiceGroups
{
    internal static readonly string[] Headings =
        ["Current decision", "Identity and allies", "Upgrades and supports", "Hand"];

    internal static int Category(AffordancePresentation offer, bool turn, string? zone)
    {
        if (!turn) return 0;
        if (offer.PlaysCard) return 3;
        return zone switch
        {
            "HeroArea" or "AlliesArea" => 1,
            "UpgradesArea" or "SupportsArea" => 2,
            "HandsArea" => 3,
            _ => 0,
        };
    }

    internal static int Category(AffordancePresentation offer, DecisionPanel panel)
    {
        int? id = offer.CardAnchorId;
        string? zone = id is null ? null : panel.world!.Areas.FirstOrDefault(area =>
            area.Cards.Any(card => card.Id == id && card.Face is not null))?.Zone;
        return Category(offer, panel.composer!.Prompt.Asking == Question.TurnOption, zone);
    }

    internal static string SourceKey(AffordancePresentation offer) =>
        offer.CardAnchorId is { } id ? $"card-{id}" : $"offer-{offer.Id}";
}
