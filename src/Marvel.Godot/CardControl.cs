using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A reusable procedural rendering of one visibility-safe card descriptor.</summary>
public sealed partial class CardControl : PanelContainer
{
    private string baseVariation = GodotThemeVariations.BoardCard;
    private bool highlighted;
    private bool presented;

    private CardControl()
    {
    }

    /// <summary>The engine handle used by prompt highlighting, when visible.</summary>
    public int? TargetId { get; private set; }

    /// <summary>Builds a card without consulting content or inferring hidden face data.</summary>
    public static CardControl Create(
        BoardCardPresentation card,
        CardDisplaySize size = CardDisplaySize.Board,
        InterfaceScale scale = InterfaceScale.Standard,
        ICardArtProvider? art = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        CardLayoutMetrics layout = VisualSystem.Card(size, scale);
        var control = new CardControl
        {
            Name = "ProceduralCard",
            TargetId = card.TargetId,
            CustomMinimumSize = new Vector2(
                layout.Width,
                card.Concealed ? layout.Width * 0.72f : EstimatedFaceHeight(card, layout, size)),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = card.Title,
            baseVariation = card.Concealed
                ? GodotThemeVariations.ConcealedCard
                : GodotThemeVariations.BoardCard,
        };
        control.ThemeTypeVariation = control.baseVariation;
        Control body = card.Concealed
            ? Back(card)
            : Face(card, layout, art, size);
        // Wrapped labels report a one-pixel minimum width before their parent
        // has laid them out. Give the procedural body the known card width so
        // Godot can calculate every name and rules-text line instead of
        // collapsing them during the first container pass.
        body.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 22),
            Math.Max(1, control.CustomMinimumSize.Y - 18));
        control.AddChild(body);
        return control;
    }

    private static float EstimatedFaceHeight(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        CardDisplaySize size)
    {
        if (size == CardDisplaySize.Hand)
        {
            return layout.MinimumHeight;
        }

        int rulesLines = string.IsNullOrWhiteSpace(card.RulesText)
            || size != CardDisplaySize.Full
            ? 0
            : card.RulesText.Split('\n').Sum(line =>
                Math.Max(1, (int)Math.Ceiling(line.Length / 24.0)));
        int valueRows = size == CardDisplaySize.Hand
            ? 0
            : card.PrintedStats.Count
                + card.Fields.Count
                + card.Counters.Count
                + (card.Cost is null ? 0 : 1);
        int textRows = 3
            + (string.IsNullOrWhiteSpace(card.Subtitle) ? 0 : 1)
            + (card.Traits.Count == 0 ? 0 : 1)
            + card.Keywords.Count
            + rulesLines
            + valueRows;
        float scale = layout.Width / 250.0f;
        float artHeight = size == CardDisplaySize.Full && card.FaceId is { Length: > 0 }
            ? layout.Width * 0.34f
            : 0;
        return Math.Max(layout.MinimumHeight, textRows * 24 * scale + artHeight + 64 * scale);
    }

    /// <summary>Applies or clears the prompt-anchor focus treatment.</summary>
    public void SetHighlighted(bool value)
    {
        highlighted = value;
        RefreshTreatment();
    }

    /// <summary>Applies or clears a transient event cue independently of prompt focus.</summary>
    public void SetPresented(bool value)
    {
        presented = value;
        RefreshTreatment();
    }

    private void RefreshTreatment() =>
        ThemeTypeVariation = highlighted || presented
            ? GodotThemeVariations.FocusedCard
            : baseVariation;

    private static VBoxContainer Back(BoardCardPresentation card)
    {
        var content = Stack();
        content.Name = "CardBack";
        content.AddChild(Label("CARD BACK", GodotThemeVariations.Eyebrow, "BackKind"));
        content.AddChild(Label(
            string.IsNullOrWhiteSpace(card.Back) ? "CONCEALED" : card.Back,
            GodotThemeVariations.CardTitle,
            "BackIdentity",
            wrap: true));
        content.AddChild(Label(
            card.Count == 1 ? "Identity hidden" : $"{card.Count} cards · order hidden",
            GodotThemeVariations.MutedText,
            "BackCount",
            wrap: true));
        return content;
    }

    private static VBoxContainer Face(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        ICardArtProvider? art,
        CardDisplaySize size)
    {
        var content = Stack();
        content.Name = "CardFace";
        if (size != CardDisplaySize.Hand)
        {
            content.AddChild(Label(card.Kind, GodotThemeVariations.Eyebrow, "Kind"));
        }
        if (!string.IsNullOrWhiteSpace(card.Classification))
        {
            content.AddChild(Label(
                card.Classification.ToUpperInvariant(),
                GodotThemeVariations.Eyebrow,
                "Classification"));
        }
        if (card.Status is "READY" or "EXHAUSTED")
        {
            Label ready = Label(
                card.Status, GodotThemeVariations.CardState, "ReadyIndicator");
            ready.HorizontalAlignment = HorizontalAlignment.Right;
            content.AddChild(ready);
        }
        content.AddChild(Label(
            card.Title,
            GodotThemeVariations.CardTitle,
            "Title",
            wrap: true));

        if (layout.ShowSubtitle && !string.IsNullOrWhiteSpace(card.Subtitle))
        {
            content.AddChild(Label(
                card.Subtitle, GodotThemeVariations.MutedText, "Subtitle", wrap: true));
        }

        Texture2D? illustration = size == CardDisplaySize.Full
            && card.FaceId is { Length: > 0 } faceId
            ? art?.Find(faceId)
            : null;
        if (illustration is not null)
        {
            content.AddChild(new TextureRect
            {
                Name = "Illustration",
                Texture = illustration,
                CustomMinimumSize = new Vector2(0, layout.Width * 0.34f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }

        if (layout.ShowTraits && card.Traits.Count > 0)
        {
            content.AddChild(Label(
                string.Join("  ·  ", card.Traits),
                GodotThemeVariations.Caption,
                "Traits",
                wrap: true));
        }

        var printed = new List<BoardFieldPresentation>();
        if (size != CardDisplaySize.Hand && card.Cost is not null)
        {
            printed.Add(new BoardFieldPresentation("COST", card.Cost));
        }

        bool hasCurrentHealth = card.Fields.Any(field => field.Name == "HEALTH");
        if (layout.ShowPrintedStats)
        {
            printed.AddRange(size == CardDisplaySize.Board
                ? card.PrintedStats.Where(stat => stat.Name is
                    "REC" or "THW" or "ATK" or "DEF" or "SCH" or "HP"
                    or "Stage" or "StartingThreat" or "TargetThreat")
                    .Where(stat => stat.Name != "HP" || !hasCurrentHealth)
                : card.PrintedStats.Where(stat => stat.Name != "HP" || !hasCurrentHealth));
        }

        if (printed.Count > 0)
        {
            content.AddChild(ValueStrip(
                "PRINTED", printed, GodotThemeVariations.CardPrintedValue, "PrintedValues"));
        }

        if (size == CardDisplaySize.Full && card.Keywords.Count > 0)
        {
            content.AddChild(Label(
                string.Join("  ·  ", card.Keywords),
                GodotThemeVariations.StatusText,
                "Keywords",
                wrap: true));
        }

        if (size == CardDisplaySize.Full && !string.IsNullOrWhiteSpace(card.RulesText))
        {
            content.AddChild(Label(
                card.RulesText,
                GodotThemeVariations.CardRules,
                "RulesText",
                wrap: true));
        }

        List<BoardFieldPresentation> live = size == CardDisplaySize.Hand
            ? []
            : size == CardDisplaySize.Board
                ? card.Fields.Where(field => field.Name is "HEALTH" or "THREAT").ToList()
                : card.Fields.ToList();
        if (card.Damage > 0 && !hasCurrentHealth)
        {
            live.Add(new BoardFieldPresentation("DAMAGE", card.Damage.ToString()));
        }

        live.AddRange(card.Counters);
        if (live.Count > 0)
        {
            content.AddChild(ValueStrip(
                "CURRENT", live, GodotThemeVariations.CardLiveValue, "LiveValues"));
        }

        if (!string.IsNullOrWhiteSpace(card.Status)
            && card.Status is not ("READY" or "EXHAUSTED")
            && size != CardDisplaySize.Hand)
        {
            content.AddChild(Label(
                card.Status, GodotThemeVariations.CardState, "StateStrip", wrap: true));
        }
        return content;
    }

    private static VBoxContainer Stack() => new()
    {
        ThemeTypeVariation = GodotThemeVariations.TightStack,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsVertical = SizeFlags.ExpandFill,
    };

    private static VBoxContainer ValueStrip(
        string heading,
        IReadOnlyList<BoardFieldPresentation> values,
        string variation,
        string name)
    {
        var section = new VBoxContainer
        {
            Name = name,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        section.AddChild(Label(heading, GodotThemeVariations.Eyebrow, $"{name}Heading"));
        var valuesList = new VBoxContainer
        {
            Name = $"{name}Values",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        foreach (BoardFieldPresentation value in values)
        {
            Label field = Label(
                $"{value.Name}  {value.Value}", variation, $"{name}{value.Name}", wrap: true);
            field.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            valuesList.AddChild(field);
        }

        section.AddChild(valuesList);
        return section;
    }

    private static Label Label(
        string text,
        string variation,
        string name,
        bool wrap = false,
        int maximumLines = -1)
    {
        int estimatedLines = wrap
            ? text.Split('\n').Sum(line =>
                Math.Max(1, (int)Math.Ceiling(line.Length / 24.0)))
            : 1;
        return new Label
        {
            Name = name,
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MaxLinesVisible = maximumLines,
            ClipText = maximumLines > 0,
            ThemeTypeVariation = variation,
            CustomMinimumSize = new Vector2(0, wrap ? estimatedLines * 30 : 0),
        };
    }
}
