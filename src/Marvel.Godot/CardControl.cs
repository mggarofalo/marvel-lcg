using Godot;

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
        InterfaceScale scale = InterfaceScale.Standard)
    {
        ArgumentNullException.ThrowIfNull(card);
        CardLayoutMetrics layout = VisualSystem.Card(size, scale);
        var control = new CardControl
        {
            Name = "ProceduralCard",
            TargetId = card.TargetId,
            CustomMinimumSize = new Vector2(layout.Width, layout.MinimumHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = card.Title,
            baseVariation = card.Concealed
                ? GodotThemeVariations.ConcealedCard
                : GodotThemeVariations.BoardCard,
        };
        control.ThemeTypeVariation = control.baseVariation;
        control.AddChild(card.Concealed
            ? Back(card)
            : Face(card, layout));
        return control;
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

    private static VBoxContainer Face(BoardCardPresentation card, CardLayoutMetrics layout)
    {
        var content = Stack();
        content.Name = "CardFace";
        content.AddChild(Label(card.Kind, GodotThemeVariations.Eyebrow, "Kind"));
        content.AddChild(Label(
            card.Title,
            GodotThemeVariations.CardTitle,
            "Title",
            wrap: true,
            maximumLines: layout.TitleLines));

        if (layout.ShowSubtitle && !string.IsNullOrWhiteSpace(card.Subtitle))
        {
            content.AddChild(Label(
                card.Subtitle, GodotThemeVariations.MutedText, "Subtitle", wrap: true));
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
        if (card.Cost is not null)
        {
            printed.Add(new BoardFieldPresentation("COST", card.Cost));
        }

        if (layout.ShowPrintedStats)
        {
            printed.AddRange(card.PrintedStats);
        }

        if (printed.Count > 0)
        {
            content.AddChild(ValueStrip(
                "PRINTED", printed, GodotThemeVariations.CardPrintedValue, "PrintedValues"));
        }

        if (card.Keywords.Count > 0)
        {
            content.AddChild(Label(
                string.Join("  ·  ", card.Keywords),
                GodotThemeVariations.StatusText,
                "Keywords",
                wrap: true));
        }

        if (!string.IsNullOrWhiteSpace(card.RulesText))
        {
            content.AddChild(Label(
                card.RulesText,
                GodotThemeVariations.CardRules,
                "RulesText",
                wrap: true,
                maximumLines: layout.RulesLines));
        }

        var live = card.Fields.ToList();
        if (card.Damage > 0)
        {
            live.Add(new BoardFieldPresentation("DAMAGE", card.Damage.ToString()));
        }

        live.AddRange(card.Counters);
        if (live.Count > 0)
        {
            content.AddChild(ValueStrip(
                "CURRENT", live, GodotThemeVariations.CardLiveValue, "LiveValues"));
        }

        content.AddChild(Label(
            card.Status, GodotThemeVariations.CardState, "StateStrip", wrap: true));
        return content;
    }

    private static VBoxContainer Stack() => new()
    {
        ThemeTypeVariation = GodotThemeVariations.TightStack,
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
        var flow = new HFlowContainer();
        foreach (BoardFieldPresentation value in values)
        {
            flow.AddChild(Label(
                $"{value.Name}  {value.Value}", variation, $"{name}{value.Name}", wrap: true));
        }

        section.AddChild(flow);
        return section;
    }

    private static Label Label(
        string text,
        string variation,
        string name,
        bool wrap = false,
        int maximumLines = -1) => new()
    {
        Name = name,
        Text = text,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        MaxLinesVisible = maximumLines,
        ClipText = maximumLines > 0,
        ThemeTypeVariation = variation,
    };
}
