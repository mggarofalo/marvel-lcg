using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the bounded two-line identity shown for one authorized hand card.</summary>
internal static class HandCardFaceRendering
{
    internal static VBoxContainer Create(BoardCardPresentation card, InterfaceScale scale)
    {
        var content = new VBoxContainer
        {
            Name = "CardFace",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var identity = new HBoxContainer
        {
            Name = "HandIdentity",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        Label title = Text(card.Title, GodotThemeVariations.CardTitle, "Title");
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        title.TooltipText = card.Title;
        identity.AddChild(title);
        if (card.Cost is not null)
        {
            identity.AddChild(Text(
                $"COST {card.Cost}", GodotThemeVariations.CardLiveValue, "HandCost"));
        }
        content.AddChild(identity);

        var details = new HBoxContainer
        {
            Name = "HandDetails",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        string kind = string.IsNullOrWhiteSpace(card.Classification)
            ? card.Kind
            : $"{card.Kind} / {card.Classification.ToUpperInvariant()}";
        Label kindLabel = Text(kind, GodotThemeVariations.Eyebrow, "Kind");
        kindLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        kindLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        details.AddChild(kindLabel);
        BoardFieldPresentation? resource = card.PrintedStats
            .FirstOrDefault(value => value.Name == "RES");
        if (resource is not null)
        {
            details.AddChild(CardFaceRendering.ResourceValue(resource, scale));
        }
        content.AddChild(details);
        return content;
    }

    private static Label Text(string text, string variation, string name) => new()
    {
        Name = name,
        Text = text,
        ThemeTypeVariation = variation,
    };
}
