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
                        : CompactHeight(card, layout, size)),
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
                ? FullFace(card, layout, scale, art)
                : CompactFace(card, size, scale);
        body.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 32),
            Math.Max(1, layout.MinimumHeight - 32));
        control.AddChild(body);
        return control;
    }

    internal static float CompactHeight(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        CardDisplaySize size)
    {
        IReadOnlyList<BoardFieldPresentation> values = CompactValues(card, size);
        int progressRows = values.Count(IsCompactProgressValue);
        int resourcesRows = values.Count(value => value.Name == "RES");
        int badgeCount = values.Count - progressRows - values.Count(value => value.Name == "RES");
        int valueRows = (badgeCount + 2) / 3 + progressRows + resourcesRows;
        int titleCharactersPerLine = size == CardDisplaySize.Hand ? 18 : 24;
        int titleRows = Math.Max(
            1,
            (int)Math.Ceiling(card.Title.Length / (double)titleCharactersPerLine));
        int textRows = (size == CardDisplaySize.Hand ? 1 : 0)
            + titleRows
            + (CompactState(card, size) is null ? 0 : 1)
            + valueRows;
        float scale = layout.Width / (size == CardDisplaySize.Hand ? 172.0f : 210.0f);
        return Math.Max(layout.MinimumHeight, textRows * 22 * scale + 20 * scale);
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
        InterfaceScale scale,
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

        CardRulesMarkup.ResourceFont();
        content.AddChild(new RichTextLabel
        {
            Name = "RulesText",
            BbcodeEnabled = true,
            Text = CardRulesMarkup.ToBbCode(card.RulesMarkup, card.RulesText, scale),
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
            content.AddChild(InspectorResourceValue(resource, scale));
        }
        return content;
    }

    private static HBoxContainer InspectorResourceValue(
        BoardFieldPresentation resource,
        InterfaceScale scale)
    {
        var presentation = new HBoxContainer
        {
            Name = "ResourceIcons",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = $"Resource {CardRulesMarkup.ResourceNames(resource.Value)}",
        };
        presentation.AddChild(Label(
            "RESOURCE",
            GodotThemeVariations.CardPrintedValue,
            "ResourceLabel"));

        Font iconFont = CardRulesMarkup.ResourceFont();
        int index = 0;
        foreach ((string name, string glyph) in CardRulesMarkup.ResourceTokens(resource.Value))
        {
            ResourceIconMetrics metrics = VisualSystem.ResourceIcon(glyph, scale);
            Label slot = Label(
                glyph,
                GodotThemeVariations.CardPrintedValue,
                $"InspectorResourceIconSlot{index}");
            slot.CustomMinimumSize = new Vector2(metrics.SlotSize, metrics.SlotSize);
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.TooltipText = $"Resource {name}";
            slot.AddThemeFontOverride("font", iconFont);
            slot.AddThemeFontSizeOverride("font_size", metrics.FontSize);
            presentation.AddChild(slot);
            index++;
        }
        return presentation;
    }

    private static VBoxContainer CompactFace(
        BoardCardPresentation card,
        CardDisplaySize size,
        InterfaceScale scale)
    {
        var content = Stack();
        content.Name = "CardFace";
        if (size == CardDisplaySize.Hand)
        {
            string identity = string.IsNullOrWhiteSpace(card.Classification)
                ? card.Kind
                : $"{card.Kind}  /  {card.Classification.ToUpperInvariant()}";
            content.AddChild(Label(identity, GodotThemeVariations.Eyebrow, "Kind"));
        }
        content.AddChild(Label(card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true));

        IReadOnlyList<BoardFieldPresentation> values = CompactValues(card, size);
        BoardFieldPresentation[] summary = [.. values.Where(value =>
            !IsCompactProgressValue(value) && value.Name != "RES")];
        if (summary.Length > 0)
        {
            content.AddChild(ValueStrip(
                string.Empty,
                summary,
                GodotThemeVariations.CardLiveValue,
                "SummaryValues",
                horizontal: true));
        }
        foreach (BoardFieldPresentation resource in values.Where(value => value.Name == "RES"))
        {
            content.AddChild(ResourceValue(resource, scale));
        }
        BoardFieldPresentation[] progress = [.. values.Where(IsCompactProgressValue)];
        if (progress.Length > 0)
        {
            content.AddChild(ValueStrip(
                string.Empty,
                progress,
                GodotThemeVariations.CardLiveValue,
                "ProgressValues"));
        }

        if (CompactState(card, size) is { } state)
        {
            content.AddChild(Label(state, GodotThemeVariations.CardState, "StateStrip", wrap: true));
        }
        return content;
    }

    private static HFlowContainer ResourceValue(
        BoardFieldPresentation resource,
        InterfaceScale scale)
    {
        var presentation = new HFlowContainer
        {
            Name = "SummaryValuesRES",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Font iconFont = CardRulesMarkup.ResourceFont();
        int index = 0;
        foreach ((string name, string glyph) in CardRulesMarkup.ResourceTokens(resource.Value))
        {
            var token = new HBoxContainer
            {
                Name = $"ResourceToken{index}",
                ThemeTypeVariation = GodotThemeVariations.CompactRow,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                TooltipText = $"Resource {name}",
            };
            token.AddChild(Label(
                name,
                GodotThemeVariations.CardLiveValue,
                $"ResourceName{index}"));
            ResourceIconMetrics metrics = VisualSystem.ResourceIcon(glyph, scale);
            Label slot = Label(
                glyph,
                GodotThemeVariations.CardLiveValue,
                $"ResourceIconSlot{index}");
            slot.CustomMinimumSize = new Vector2(metrics.SlotSize, metrics.SlotSize);
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.AddThemeFontOverride("font", iconFont);
            slot.AddThemeFontSizeOverride("font_size", metrics.FontSize);
            token.AddChild(slot);
            presentation.AddChild(token);
            index++;
        }
        return presentation;
    }

    internal static IReadOnlyList<BoardFieldPresentation> CompactValues(
        BoardCardPresentation card,
        CardDisplaySize size)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card.Concealed)
        {
            return [];
        }

        var values = new List<BoardFieldPresentation>();
        if (size == CardDisplaySize.Hand)
        {
            if (card.Cost is not null)
            {
                AddUnique(values, new BoardFieldPresentation("COST", card.Cost));
            }
            BoardFieldPresentation? resource = card.PrintedStats
                .FirstOrDefault(value => value.Name == "RES");
            if (resource is not null)
            {
                AddUnique(values, resource);
            }
            return values;
        }

        CardFrameFamily family = VisualSystem.CardFrame(card.Kind).Family;
        switch (family)
        {
            case CardFrameFamily.Identity:
                if (HasLiveField(card, "HEALTH"))
                {
                    if (card.Kind == "ALTER EGO")
                    {
                        AddPresented(values, card, "REC", "RECOVER");
                    }
                    else
                    {
                        AddPresented(values, card, "THW", "THWART");
                        AddPresented(values, card, "ATK", "ATTACK");
                        AddPresented(values, card, "DEF", "DEFENSE");
                    }
                    AddPresented(values, card, "HEALTH", "HEALTH");
                }
                else
                {
                    AddQuietMetadata(values, card);
                }
                break;
            case CardFrameFamily.Enemy:
                if (HasLiveField(card, "HEALTH"))
                {
                    AddPresented(values, card, "SCH", "SCHEME");
                    AddPresented(values, card, "ATK", "ATTACK");
                    AddPresented(values, card, "HEALTH", "HEALTH");
                }
                else
                {
                    AddQuietMetadata(values, card);
                }
                break;
            case CardFrameFamily.Scheme:
                if (HasLiveField(card, "THREAT"))
                {
                    if (SchemeThreat(card) is { } threat)
                    {
                        AddUnique(values, threat);
                    }
                    AddPresentedOrPrinted(
                        values,
                        card,
                        "ESCALATION_THREAT",
                        "ESCALATION_THREAT",
                        "EscalationThreat");
                    AddPersistentIcons(values, card);
                }
                else
                {
                    AddQuietMetadata(values, card);
                }
                break;
            case CardFrameFamily.Player when card.Kind == "ALLY":
                if (HasLiveField(card, "HEALTH"))
                {
                    AddPresented(values, card, "THW", "THWART");
                    AddPresented(values, card, "ATK", "ATTACK");
                    AddPresented(values, card, "HEALTH", "HEALTH");
                }
                break;
            case CardFrameFamily.Environment:
                AddQuietMetadata(values, card);
                break;
        }

        foreach (BoardFieldPresentation counter in card.Counters)
        {
            AddUnique(values, counter);
        }
        return values;
    }

    internal static bool IsCompactProgressValue(BoardFieldPresentation value) =>
        value.Name is "HEALTH" or "THREAT";

    internal static string? CompactState(BoardCardPresentation card, CardDisplaySize size)
    {
        ArgumentNullException.ThrowIfNull(card);
        return size == CardDisplaySize.Hand
            || string.IsNullOrWhiteSpace(card.Status)
            || card.Status == "READY"
                ? null
                : card.Status;
    }

    private static bool HasLiveField(BoardCardPresentation card, string name) =>
        card.Fields.Any(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static void AddPresented(
        List<BoardFieldPresentation> destination,
        BoardCardPresentation card,
        string displayedName,
        params string[] sourceNames)
    {
        BoardFieldPresentation? value = card.Fields.FirstOrDefault(field =>
            sourceNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase));
        if (value is not null)
        {
            AddUnique(destination, new BoardFieldPresentation(displayedName, value.Value));
        }
    }

    private static void AddPresentedOrPrinted(
        List<BoardFieldPresentation> destination,
        BoardCardPresentation card,
        string displayedName,
        params string[] sourceNames)
    {
        BoardFieldPresentation? value = card.Fields.FirstOrDefault(field =>
                sourceNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
            ?? card.PrintedStats.FirstOrDefault(field =>
                sourceNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase));
        if (value is not null)
        {
            AddUnique(destination, new BoardFieldPresentation(displayedName, value.Value));
        }
    }

    private static void AddQuietMetadata(
        List<BoardFieldPresentation> destination,
        BoardCardPresentation card)
    {
        AddPresentedOrPrinted(destination, card, "Stage", "Stage", "PRINTED_STAGE");
        AddPresentedOrPrinted(destination, card, "Boost", "Boost");
        AddPersistentIcons(destination, card);
    }

    private static void AddPersistentIcons(
        List<BoardFieldPresentation> destination,
        BoardCardPresentation card)
    {
        (string Source, string Display)[] icons =
        {
            ("ACCELERATION ICON", "ACCELERATION"),
            ("AMPLIFY", "AMPLIFY"),
            ("CRISIS", "CRISIS"),
            ("HAZARD", "HAZARD"),
        };
        foreach ((string source, string display) in icons)
        {
            BoardFieldPresentation? value = card.Fields.FirstOrDefault(field =>
                field.Name.Equals(source, StringComparison.OrdinalIgnoreCase));
            if (value is not null
                && long.TryParse(value.Value, out long count)
                && count > 0)
            {
                AddUnique(destination, new BoardFieldPresentation(display, value.Value));
            }
        }
    }

    private static void AddUnique(
        List<BoardFieldPresentation> destination,
        BoardFieldPresentation value)
    {
        string displayedName = DisplayFieldName(value.Name);
        if (!destination.Any(existing => string.Equals(
                DisplayFieldName(existing.Name), displayedName, StringComparison.OrdinalIgnoreCase)))
        {
            destination.Add(value);
        }
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
        Container valuesList = horizontal
            ? new HFlowContainer()
            : new VBoxContainer();
        valuesList.Name = $"{name}Values";
        valuesList.ThemeTypeVariation = horizontal
            ? GodotThemeVariations.CompactRow
            : GodotThemeVariations.TightStack;
        valuesList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (BoardFieldPresentation value in values)
        {
            string displayed = value.Name switch
            {
                "Boost" => new string('◆', int.TryParse(value.Value, out int boost) ? boost : 0),
                "RES" => $"{CardRulesMarkup.ResourceNames(value.Value)}  "
                    + CardRulesMarkup.ResourceIcons(value.Value),
                _ => value.Value,
            };
            Label field = Label(
                $"{DisplayFieldName(value.Name)}  {displayed}",
                variation,
                $"{name}{value.Name}",
                wrap: !horizontal);
            field.SizeFlagsHorizontal = horizontal
                ? SizeFlags.ShrinkBegin
                : SizeFlags.ExpandFill;
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
        "RES" => "Resource",
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
            TextOverrunBehavior = maximumLines > 0
                ? TextServer.OverrunBehavior.TrimEllipsis
                : TextServer.OverrunBehavior.NoTrimming,
            MaxLinesVisible = maximumLines,
            ClipText = maximumLines > 0,
            ThemeTypeVariation = variation,
            CustomMinimumSize = new Vector2(0, wrap ? estimatedLines * 30 : 0),
        };
    }
}
