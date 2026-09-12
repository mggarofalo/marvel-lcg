using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Chooses and renders the values visible on a procedural card.</summary>
internal static class CardValueRendering
{
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

        AddFrameValues(values, card);
        foreach (BoardFieldPresentation counter in card.Counters)
        {
            AddUnique(values, counter);
        }
        return values;
    }

    private static void AddFrameValues(
        List<BoardFieldPresentation> values,
        BoardCardPresentation card)
    {
        CardFrameFamily family = VisualSystem.CardFrame(card.Kind).Family;
        switch (family)
        {
            case CardFrameFamily.Identity:
                AddIdentityValues(values, card);
                break;
            case CardFrameFamily.Enemy:
                AddEnemyValues(values, card);
                break;
            case CardFrameFamily.Scheme:
                AddSchemeValues(values, card);
                break;
            case CardFrameFamily.Player when card.Kind == "ALLY":
                AddAllyValues(values, card);
                break;
            case CardFrameFamily.Environment:
                AddQuietMetadata(values, card);
                break;
        }
    }

    private static void AddIdentityValues(
        List<BoardFieldPresentation> values,
        BoardCardPresentation card)
    {
        if (!HasLiveField(card, "HEALTH"))
        {
            AddQuietMetadata(values, card);
            return;
        }
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

    private static void AddEnemyValues(
        List<BoardFieldPresentation> values,
        BoardCardPresentation card)
    {
        if (!HasLiveField(card, "HEALTH"))
        {
            AddQuietMetadata(values, card);
            return;
        }
        if (card.Kind.Contains("VILLAIN", StringComparison.OrdinalIgnoreCase))
        {
            AddPresentedOrPrinted(values, card, "Stage", "Stage", "PRINTED_STAGE");
        }
        AddPresented(values, card, "SCH", "SCHEME");
        AddPresented(values, card, "ATK", "ATTACK");
        AddPresented(values, card, "HEALTH", "HEALTH");
    }

    private static void AddSchemeValues(
        List<BoardFieldPresentation> values,
        BoardCardPresentation card)
    {
        if (!HasLiveField(card, "THREAT"))
        {
            AddQuietMetadata(values, card);
            return;
        }
        if (SchemeThreat(card) is { } threat)
        {
            AddUnique(values, threat);
        }
        AddPresentedOrPrinted(
            values, card, "ESCALATION_THREAT", "ESCALATION_THREAT", "EscalationThreat");
        AddPersistentIcons(values, card);
    }

    private static void AddAllyValues(
        List<BoardFieldPresentation> values,
        BoardCardPresentation card)
    {
        if (!HasLiveField(card, "HEALTH"))
        {
            return;
        }
        AddPresented(values, card, "THW", "THWART");
        AddPresented(values, card, "ATK", "ATTACK");
        AddPresented(values, card, "HEALTH", "HEALTH");
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

    internal static bool HasLiveField(BoardCardPresentation card, string name) =>
        card.Fields.Any(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    internal static void AddPresented(
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

    internal static void AddPresentedOrPrinted(
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

    internal static void AddQuietMetadata(
        List<BoardFieldPresentation> destination,
        BoardCardPresentation card)
    {
        AddPresentedOrPrinted(destination, card, "Stage", "Stage", "PRINTED_STAGE");
        AddPresentedOrPrinted(destination, card, "Boost", "Boost");
        AddPersistentIcons(destination, card);
    }

    internal static void AddPersistentIcons(
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

    internal static void AddUnique(
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

    internal static List<BoardFieldPresentation> LiveValues(BoardCardPresentation card)
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

    internal static BoardFieldPresentation? PrimaryValue(
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

    internal static BoardFieldPresentation? SchemeThreat(BoardCardPresentation card)
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

    internal static VBoxContainer Badge(BoardFieldPresentation value, string name)
    {
        var badge = Stack();
        badge.Name = name;
        badge.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        badge.CustomMinimumSize = new Vector2(64, 0);
        badge.AddChild(Label(value.Name, GodotThemeVariations.Eyebrow, $"{name}Label"));
        badge.AddChild(Label(value.Value, GodotThemeVariations.CardTitle, $"{name}Value"));
        return badge;
    }

    internal static VBoxContainer Stack() => new()
    {
        ThemeTypeVariation = GodotThemeVariations.TightStack,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
    };

    internal static VBoxContainer ValueStrip(
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
        valuesList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        foreach (BoardFieldPresentation value in values)
        {
            string displayed = value.Name switch
            {
                "Boost" => new string('◆', int.TryParse(value.Value, out int boost) ? boost : 0),
                "RES" => CardRulesMarkup.ResourceIcons(value.Value),
                _ => value.Value,
            };
            Label field = Label(
                $"{DisplayFieldName(value.Name)}  {displayed}",
                variation,
                $"{name}{value.Name}",
                wrap: !horizontal);
            field.SizeFlagsHorizontal = horizontal
                ? Control.SizeFlags.ShrinkBegin
                : Control.SizeFlags.ExpandFill;
            valuesList.AddChild(field);
        }
        section.AddChild(valuesList);
        return section;
    }

    internal static string DisplayFieldName(string name) => name switch
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

    internal static Label Label(
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
