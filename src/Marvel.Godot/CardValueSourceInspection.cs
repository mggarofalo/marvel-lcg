using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Pages through disclosed value sources without reconstructing their arithmetic.</summary>
internal sealed class CardValueSourceInspection
{
    private readonly BoardCardPresentation card;
    private readonly Action<BoardCardPresentation> inspect;
    private readonly string[] keys;
    private readonly OptionButton select;
    private readonly VBoxContainer content;
    private int sourceIndex;

    private CardValueSourceInspection(BoardCardPresentation card, Action<BoardCardPresentation> inspect,
        OptionButton select, VBoxContainer content)
    {
        this.card = card;
        this.select = select;
        this.content = content;
        this.inspect = inspect;
        keys = card.EffectiveValues.Keys.OrderBy(key => key == "HP" ? "" : key, StringComparer.Ordinal).ToArray();
        foreach (string key in keys) select.AddItem(key == "HP" ? "Maximum HP" : key == "HS" ? "Hand size" : key);
        select.ItemSelected += _ => { sourceIndex = 0; Render(); };
    }

    internal static void Add(VBoxContainer parent, BoardCardPresentation card, Action<BoardCardPresentation> inspect)
    {
        if (card.Concealed || card.EffectiveValues.Count == 0) return;
        var select = new OptionButton { Name = "InspectValue", CustomMinimumSize = new Vector2(0, 36) };
        var content = new VBoxContainer { Name = "ValueSourceDetail" };
        parent.AddChild(select);
        parent.AddChild(content);
        var browser = new CardValueSourceInspection(card, inspect, select, content);
        browser.Render();
    }

    private void Render()
    {
        foreach (Node child in content.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
        string key = keys[select.Selected];
        CardEffectiveValue value = card.EffectiveValues[key];
        AddFacts(key, value);
        CardValueCalculation[] sources = value.Calculation.Where(row => row.Source is not null).ToArray();
        if (sources.Length == 0) return;
        sourceIndex = Math.Clamp(sourceIndex, 0, sources.Length - 1);
        AddSource(content, sources[sourceIndex], inspect);
        AddPages(content, sourceIndex, sources.Length, offset => { sourceIndex += offset; Render(); });
    }

    private void AddFacts(string key, CardEffectiveValue value)
    {
        BoardPrintedValueMark? mark = card.PrintedMarks.FirstOrDefault(mark => mark.Attribute == key);
        string? printed = mark is null ? null : mark.Value + (mark.PerPlayer ? " per player" : "");
        CardLiveStateRendering.AddLabel(content, $"Current {value.CurrentValue.ToString(CultureInfo.InvariantCulture)}"
            + (value.IsModified ? " · modified" : ""), "InspectedCurrentValue", 18);
        if (printed is not null)
            CardLiveStateRendering.AddLabel(content, $"Printed {printed}", "InspectedPrintedValue", 14);
    }

    private static void AddSource(VBoxContainer parent, CardValueCalculation row, Action<BoardCardPresentation> inspect)
    {
        CardValueSourceDescriptor source = row.Source!;
        CardLiveStateRendering.AddLabel(parent, Operation(row), "SourceOperation", 16);
        var button = new Button { Name = "InspectValueSource", Text = source.Title,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0, 36),
            AccessibilityName = $"Read {source.Title}" + (source.Historical ? ", earlier source" : "") };
        button.Pressed += () => inspect(DescribeSource(source));
        parent.AddChild(button);
        if (source.Historical) CardLiveStateRendering.AddLabel(parent, "Earlier source", "HistoricalSource", 14);
        string duration = Duration(row.Duration);
        if (duration.Length > 0) CardLiveStateRendering.AddLabel(parent, duration, "SourceDuration", 14);
    }

    private static void AddPages(VBoxContainer parent, int page, int count, Action<int> change)
    {
        var navigation = new HBoxContainer();
        var previous = new Button { Name = "PreviousValueSource", Text = "‹", Disabled = page == 0,
            AccessibilityName = "Previous source", CustomMinimumSize = new Vector2(36, 36) };
        var next = new Button { Name = "NextValueSource", Text = "›", Disabled = page + 1 == count,
            AccessibilityName = "Next source", CustomMinimumSize = new Vector2(36, 36) };
        previous.Pressed += () => change(-1);
        next.Pressed += () => change(1);
        navigation.AddChild(previous);
        navigation.AddChild(new Label { Text = $"Source {page + 1}/{count}", VerticalAlignment = VerticalAlignment.Center });
        navigation.AddChild(next);
        parent.AddChild(navigation);
    }

    internal static BoardCardPresentation DescribeSource(CardValueSourceDescriptor source) =>
        new(source.Historical ? null : source.CardId, 1, false, source.Title,
            source.Historical ? "Earlier source" : "", "SOURCE", "", [])
        { FaceId = source.FaceId, RulesText = source.RulesText, RulesMarkup = source.RulesMarkup };

    internal static string Operation(CardValueCalculation row) => row.Operation switch
    {
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
