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
        if (size == CardDisplaySize.Full
            && VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme)
        {
            layout = layout with
            {
                Width = layout.MinimumHeight,
                MinimumHeight = layout.Width,
            };
        }
        string variation = card.Concealed
            ? GodotThemeVariations.ConcealedCard
            : size == CardDisplaySize.Full
                ? VisualSystem.CardFrame(card.Kind).ThemeVariation
                : GodotThemeVariations.BoardCard;
        var control = new CardControl
        {
            Name = "ProceduralCard",
            TargetId = card.TargetId,
            CustomMinimumSize = new Vector2(
                layout.Width,
                card.Concealed
                    ? layout.Width * 0.72f
                    : size == CardDisplaySize.Full
                        ? layout.MinimumHeight
                        : EstimatedCompactHeight(card, layout, size)),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = card.Title,
            FocusMode = card.Concealed ? FocusModeEnum.None : FocusModeEnum.All,
            MouseDefaultCursorShape = card.Concealed
                ? CursorShape.Arrow
                : CursorShape.PointingHand,
            baseVariation = variation,
            ThemeTypeVariation = variation,
        };
        Control body = card.Concealed
            ? Back(card)
            : size == CardDisplaySize.Full
                ? FullFace(card, layout, art)
                : CompactFace(card, layout, size);
        body.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 32),
            Math.Max(1, layout.MinimumHeight - 32));
        control.AddChild(body);
        return control;
    }

    private static float EstimatedCompactHeight(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        CardDisplaySize size)
    {
        if (size == CardDisplaySize.Hand)
        {
            return layout.MinimumHeight;
        }

        int valueRows = card.PrintedStats.Count + card.Fields.Count + card.Counters.Count;
        int textRows = 2
            + (string.IsNullOrWhiteSpace(card.Classification) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(card.Status) ? 0 : 1)
            + valueRows;
        float scale = layout.Width / 250.0f;
        return Math.Max(layout.MinimumHeight, textRows * 24 * scale + 48 * scale);
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
        content.AddChild(Label("Card back", GodotThemeVariations.Eyebrow, "BackKind"));
        content.AddChild(Label(
            string.IsNullOrWhiteSpace(card.Back) ? "Concealed" : card.Back,
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

    private static VBoxContainer FullFace(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        ICardArtProvider? art)
    {
        CardFrameProfile profile = VisualSystem.CardFrame(card.Kind);
        var content = Stack();
        content.Name = "CardFace";

        var header = new HBoxContainer
        {
            Name = "Masthead",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        BoardFieldPresentation? primary = PrimaryValue(card, profile.Family);
        if (primary is not null && profile.Family != CardFrameFamily.Scheme)
        {
            header.AddChild(Badge(primary, "PrimaryValue"));
        }

        var identity = Stack();
        identity.Name = "Identity";
        identity.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        identity.AddChild(Label(card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true));
        if (!string.IsNullOrWhiteSpace(card.Subtitle))
        {
            identity.AddChild(Label(
                card.Subtitle, GodotThemeVariations.MutedText, "Subtitle", wrap: true));
        }
        identity.AddChild(Label(
            card.Kind, GodotThemeVariations.Eyebrow, "Kind", wrap: true));
        header.AddChild(identity);
        if (primary is not null && profile.Family == CardFrameFamily.Scheme)
        {
            header.AddChild(Badge(primary, "PrimaryValue"));
        }
        if (!string.IsNullOrWhiteSpace(card.Status))
        {
            header.AddChild(Badge(
                new BoardFieldPresentation("STATE", card.Status), "ReadyIndicator"));
        }
        content.AddChild(header);

        var artWell = new PanelContainer
        {
            Name = "IllustrationRegion",
            ThemeTypeVariation = GodotThemeVariations.CardArtWell,
            CustomMinimumSize = new Vector2(
                0,
                profile.Family == CardFrameFamily.Scheme
                    ? layout.MinimumHeight * 0.20f
                    : layout.Width * 0.32f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Texture2D? illustration = card.FaceId is { Length: > 0 } faceId
            ? art?.Find(faceId)
            : null;
        if (illustration is not null)
        {
            artWell.AddChild(new TextureRect
            {
                Name = "Illustration",
                Texture = illustration,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
        content.AddChild(artWell);

        if (card.Traits.Count > 0)
        {
            content.AddChild(Label(
                string.Join("  ·  ", card.Traits),
                GodotThemeVariations.Caption,
                "Traits",
                wrap: true));
        }

        BoardFieldPresentation? schemeThreat = profile.Family == CardFrameFamily.Scheme
            ? SchemeThreat(card)
            : null;
        List<BoardFieldPresentation> printed = card.PrintedStats
            .Where(value => value.Name is not "RES")
            .Where(value => primary is null || value.Name != primary.Name)
            .Where(value => profile.Family != CardFrameFamily.Identity
                || value.Name != "HP"
                || !card.Fields.Any(field => field.Name == "HEALTH"))
            .Where(value => profile.Family != CardFrameFamily.Scheme
                || value.Name is not "TargetThreat")
            .ToList();
        if (schemeThreat is not null)
        {
            printed.Add(schemeThreat);
        }
        if (printed.Count > 0)
        {
            content.AddChild(ValueStrip(
                profile.Family == CardFrameFamily.Scheme ? "" : "PRINTED",
                printed, GodotThemeVariations.CardPrintedValue, "PrintedValues",
                horizontal: true));
        }

        content.AddChild(new RichTextLabel
        {
            Name = "RulesText",
            BbcodeEnabled = true,
            Text = CardRulesMarkup.ToBbCode(card.RulesMarkup, card.RulesText),
            FitContent = true,
            ScrollActive = false,
            FocusMode = FocusModeEnum.All,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            CustomMinimumSize = Vector2.Zero,
            ThemeTypeVariation = GodotThemeVariations.CardRulesRich,
        });

        if (profile.Family != CardFrameFamily.Scheme || schemeThreat is null)
        {
            List<BoardFieldPresentation> live = LiveValues(card);
            if (profile.Family == CardFrameFamily.Identity)
            {
                live = live.Where(value => value.Name == "HEALTH").ToList();
            }
            if (live.Count > 0)
            {
                content.AddChild(ValueStrip(
                    "CURRENT", live, GodotThemeVariations.CardLiveValue, "LiveValues",
                    horizontal: true));
            }
        }

        BoardFieldPresentation? resource = card.PrintedStats
            .FirstOrDefault(value => value.Name == "RES");
        if (resource is not null)
        {
            content.AddChild(Label(
                $"RESOURCE  {CardRulesMarkup.ResourceIcons(resource.Value)}",
                GodotThemeVariations.CardPrintedValue,
                "ResourceIcons"));
        }
        return content;
    }

    private static VBoxContainer CompactFace(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
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
            Label ready = Label(card.Status, GodotThemeVariations.CardState, "ReadyIndicator");
            ready.HorizontalAlignment = HorizontalAlignment.Right;
            content.AddChild(ready);
        }
        content.AddChild(Label(card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true));

        bool hasCurrentHealth = card.Fields.Any(field => field.Name == "HEALTH");
        if (layout.ShowPrintedStats)
        {
            BoardFieldPresentation[] printed = card.PrintedStats
                .Where(stat => stat.Name is
                    "REC" or "THW" or "ATK" or "DEF" or "SCH" or "HP"
                    or "Stage" or "StartingThreat" or "TargetThreat")
                .Where(stat => stat.Name != "HP" || !hasCurrentHealth)
                .ToArray();
            if (printed.Length > 0)
            {
                content.AddChild(ValueStrip(
                    "PRINTED", printed, GodotThemeVariations.CardPrintedValue, "PrintedValues"));
            }
        }

        List<BoardFieldPresentation> live = size == CardDisplaySize.Hand ? [] : LiveValues(card);
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

    private static List<BoardFieldPresentation> LiveValues(BoardCardPresentation card)
    {
        bool hasCurrentHealth = card.Fields.Any(field => field.Name == "HEALTH");
        List<BoardFieldPresentation> live = card.Fields.ToList();
        if (card.Damage > 0 && !hasCurrentHealth)
        {
            live.Add(new BoardFieldPresentation("DAMAGE", card.Damage.ToString()));
        }
        live.AddRange(card.Counters);
        return live;
    }

    private static BoardFieldPresentation? PrimaryValue(
        BoardCardPresentation card,
        CardFrameFamily family)
    {
        if (family == CardFrameFamily.Player && card.Cost is not null)
        {
            return new BoardFieldPresentation("COST", card.Cost);
        }
        string[] names = family switch
        {
            CardFrameFamily.Enemy => ["Stage", "Boost"],
            CardFrameFamily.Scheme => ["Stage", "Boost"],
            CardFrameFamily.Environment => ["Boost"],
            _ => [],
        };
        return names.Select(name => card.PrintedStats.FirstOrDefault(value => value.Name == name))
            .FirstOrDefault(value => value is not null);
    }

    private static BoardFieldPresentation? SchemeThreat(BoardCardPresentation card)
    {
        BoardFieldPresentation? current = card.Fields.FirstOrDefault(value =>
            value.Name.Equals("THREAT", StringComparison.OrdinalIgnoreCase));
        BoardFieldPresentation? maximum = card.Fields.FirstOrDefault(value =>
                value.Name.Equals("TARGET_THREAT", StringComparison.OrdinalIgnoreCase))
            ?? card.PrintedStats.FirstOrDefault(value => value.Name == "TargetThreat");
        if (current is null)
        {
            return null;
        }
        return new BoardFieldPresentation(
            "THREAT",
            maximum is null
                ? current.Value
                : $"{current.Value}/{maximum.Value.TrimEnd('*')}");
    }

    private static VBoxContainer Badge(BoardFieldPresentation value, string name)
    {
        var badge = Stack();
        badge.Name = name;
        badge.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        badge.CustomMinimumSize = new Vector2(64, 0);
        badge.AddChild(Label(value.Name, GodotThemeVariations.Eyebrow, $"{name}Label"));
        badge.AddChild(Label(value.Value, GodotThemeVariations.CardTitle, $"{name}Value"));
        return badge;
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
        string name,
        bool horizontal = false)
    {
        var section = new VBoxContainer
        {
            Name = name,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        if (!string.IsNullOrWhiteSpace(heading))
        {
            section.AddChild(Label(heading, GodotThemeVariations.Eyebrow, $"{name}Heading"));
        }
        Container valuesList = horizontal && values.Count > 3
            ? new GridContainer { Columns = values.Count >= 6 ? 3 : 2 }
            : horizontal
                ? new HBoxContainer()
                : new VBoxContainer();
        valuesList.Name = $"{name}Values";
        valuesList.ThemeTypeVariation = horizontal
            ? GodotThemeVariations.CompactRow
            : GodotThemeVariations.TightStack;
        valuesList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (BoardFieldPresentation value in values)
        {
            string displayed = value.Name == "Boost"
                ? new string('◆', int.TryParse(value.Value, out int boost) ? boost : 0)
                : value.Value;
            Label field = Label(
                $"{DisplayFieldName(value.Name)}  {displayed}",
                variation,
                $"{name}{value.Name}",
                wrap: true);
            field.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            valuesList.AddChild(field);
        }
        section.AddChild(valuesList);
        return section;
    }

    private static string DisplayFieldName(string name) => name switch
    {
        "ALLY_LIMIT" => "Ally",
        "HAND_SIZE" => "Hand",
        "HS" => "Hand",
        "HEALTH" => "HP",
        "FIRST_PLAYER_TOKEN" => "First",
        "RECOVER" => "REC",
        "RESTRICTED_LIMIT" => "Limit",
        "StartingThreat" or "PrintedStartingThreat" or "PRINTED_STARTING_THREAT" => "Start",
        "TargetThreat" or "TARGET_THREAT" => "Target",
        "EscalationThreat" or "ESCALATION_THREAT" => "Escalation",
        "PRINTED_STAGE" => "Stage",
        _ when name.StartsWith("PRINTED", StringComparison.OrdinalIgnoreCase) => "Start",
        _ => name.Replace('_', ' '),
    };

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
