using Godot;

namespace Marvel.Godot;

/// <summary>Maintains an ordered table of one through four distinct hero selections.</summary>
internal sealed class HeroSetupControls
{
    private readonly Main main;
    private readonly List<OptionButton> optional = [];

    internal HeroSetupControls(Main main)
    {
        this.main = main;
        optional.Add(main.secondHero);
        for (int seat = 2; seat < 4; seat++)
        {
            var label = new Label { Name = "HeroSeatLabel" + seat, Text = $"Hero {seat + 1}" };
            var choice = new OptionButton { Name = "HeroSeat" + seat };
            main.setupGrid.AddChild(label); main.setupGrid.AddChild(choice);
            main.setupGrid.MoveChild(label, main.scenario.GetIndex() - 1);
            main.setupGrid.MoveChild(choice, label.GetIndex() + 1);
            choice.ItemSelected += _ => main.setupController.RefreshBriefing();
            optional.Add(choice);
        }
    }

    internal IReadOnlyList<OptionButton> Additional => optional.Skip(1).ToArray();

    internal IReadOnlyList<string> Selected()
    {
        var selected = new List<string> { main.setupChoices!.Heroes[main.hero.Selected].Key };
        foreach (var choice in optional)
        {
            if (choice.Selected <= 0) break;
            selected.Add(choice.GetItemMetadata(choice.Selected).AsString());
        }
        return selected;
    }

    internal void Refresh()
    {
        if (main.setupChoices is null || main.hero.Selected < 0) return;
        var used = new HashSet<string>(StringComparer.Ordinal) { main.setupChoices.Heroes[main.hero.Selected].Key };
        bool previousSelected = true;
        foreach (var choice in optional)
            previousSelected = RefreshChoice(choice, used, previousSelected);
    }

    private bool RefreshChoice(OptionButton choice, HashSet<string> used, bool previousSelected)
    {
        string? previous = choice.Selected > 0 ? choice.GetItemMetadata(choice.Selected).AsString() : null;
        choice.Clear();
        choice.AddItem("No additional player");
        foreach (var hero in main.setupChoices!.Heroes.Where(hero => !used.Contains(hero.Key)))
        {
            choice.AddItem(hero.Name);
            choice.SetItemMetadata(choice.ItemCount - 1, hero.Key);
        }
        int selected = previousSelected && previous is not null
            ? Enumerable.Range(1, choice.ItemCount - 1).FirstOrDefault(index => choice.GetItemMetadata(index).AsString() == previous, 0)
            : 0;
        choice.Select(selected);
        choice.Disabled = main.hero.Disabled || !previousSelected;
        if (selected > 0) used.Add(choice.GetItemMetadata(selected).AsString());
        return selected > 0;
    }

    internal void Assign(string key, int seat)
    {
        if (seat is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(seat));
        if (seat == 0)
        {
            int index = main.setupChoices!.Heroes.ToList().FindIndex(hero => hero.Key == key);
            if (index < 0) throw new InvalidOperationException("This hero is not offered by the game service.");
            main.hero.Select(index);
        }
        else
        {
            if (Selected().Count < seat) throw new InvalidOperationException($"Choose heroes for seats 1 through {seat} first.");
            var choice = optional[seat - 1];
            int index = Enumerable.Range(1, choice.ItemCount - 1)
                .FirstOrDefault(item => choice.GetItemMetadata(item).AsString() == key, -1);
            if (index < 0) throw new InvalidOperationException($"Choose a distinct hero for seat {seat + 1}.");
            choice.Select(index);
        }
        Refresh();
    }
}
