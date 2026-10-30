using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Lists disclosed value calculations together beside the inspected card.</summary>
internal static class CardValueSourceInspection
{
    internal static void Add(VBoxContainer parent, BoardCardPresentation card, Action<BoardCardPresentation> inspect,
        CardControl face)
    {
        if (card.Concealed) return;
        var content = new VBoxContainer { Name = "ValueSourceDetail", Visible = false };
        content.AddThemeConstantOverride("separation", 8);
        foreach (string key in card.EffectiveValues.Keys.OrderBy(key => key == "HP" ? "" : key, StringComparer.Ordinal))
        {
            CardEffectiveValue value = card.EffectiveValues[key];
            if (!value.IsModified) continue;
            var section = new VBoxContainer { Name = "Value" + key, Visible = false };
            string name = key == "HP" ? "Maximum HP" : key == "HS" ? "Hand size" : key;
            CardLiveStateRendering.AddLabel(section, name,
                "InspectedCurrentValue", 18);
            CardLiveStateRendering.AddLabel(section, BaseDescription(value), "InspectedBaseValue", 14);
            foreach (CardValueCalculation row in value.Calculation.Where(row => row.Source is not null))
                AddSource(section, row, inspect);
            content.AddChild(section);
            ModifiedValueDisclosure.Bind(face, key, content, section);
        }
        if (content.GetChildCount() > 0) parent.AddChild(content);
        else content.Free();
    }

    internal static string BaseDescription(CardEffectiveValue value) =>
        $"Base {value.BaseValue.ToString(CultureInfo.InvariantCulture)}" + (value.BaseKind switch
        { "Defined" => " · defined", "Replacement" => " · replacement", _ => "" });

    private static void AddSource(VBoxContainer parent, CardValueCalculation row, Action<BoardCardPresentation> inspect)
    {
        CardValueSourceDescriptor source = row.Source!;
        var button = new Button { Name = $"InspectValueSource{parent.GetChildCount()}", Text = Operation(row) + "  " + source.Title, Flat = true,
            AutowrapMode = TextServer.AutowrapMode.Word, CustomMinimumSize = new Vector2(0, 44),
            AccessibilityName = $"Read {source.Title}" + (source.Historical ? ", earlier source" : "") };
        button.Pressed += () => inspect(DescribeSource(source));
        parent.AddChild(button);
        if (source.Historical) CardLiveStateRendering.AddLabel(parent, "Earlier source", "HistoricalSource", 14);
        string duration = Duration(row.Duration);
        if (duration.Length > 0) CardLiveStateRendering.AddLabel(parent, duration, "SourceDuration", 14);
    }

    internal static BoardCardPresentation DescribeSource(CardValueSourceDescriptor source) =>
        new(source.Historical ? null : source.CardId, 1, false, source.Title,
            source.Historical ? "Earlier source" : "", "SOURCE", "", [])
        { FaceId = source.FaceId, RulesText = source.RulesText, RulesMarkup = source.RulesMarkup };

    internal static string Operation(CardValueCalculation row) => row.Operation switch
    {
        "Maximum" => $"Maximum {row.Amount.ToString(CultureInfo.InvariantCulture)}",
        "DefineBase" => $"Base {row.Amount.ToString(CultureInfo.InvariantCulture)}",
        "Add" => row.Amount.ToString("+0;-0;0", CultureInfo.InvariantCulture),
        "Lost" => "Characteristic lost",
        "Unmodifiable" => "Cannot be modified",
        _ => row.Operation,
    };

    private static string Duration(CardValueDuration? duration)
    {
        if (duration is null) return "";
        var parts = new List<string>();
        if (duration.WhileInPlay) parts.Add("While in play");
        if (duration.Until is { } until) parts.Add($"Until {until.Replace('_', ' ')}");
        if (duration.Uses is { } uses) parts.Add($"{uses} uses");
        if (duration.OnCondition is not null) parts.Add("Conditional duration · see source");
        return string.Join(" · ", parts);
    }
}
