using Godot;

namespace Marvel.Godot;

/// <summary>The deck library's native controls and keyboard-accessible editing surface.</summary>
internal sealed class DeckEditorControls
{
    internal Window Window { get; } = new() { Name = "DeckLibrary", Title = "Core deck library", Size = new(1080, 760), MinSize = new(850, 650), Transient = true, Exclusive = true };
    internal LineEdit Name { get; } = new() { PlaceholderText = "Deck name", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal OptionButton Hero { get; } = new();
    internal OptionButton Library { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Load { get; } = new() { Text = "Open saved" };
    internal Button Delete { get; } = new() { Text = "Delete saved" };
    internal Button Starter { get; } = new() { Text = "New starter" };
    internal Button Empty { get; } = new() { Text = "New empty deck" };
    internal Button Copy { get; } = new() { Text = "Duplicate" };
    internal Button Save { get; } = new() { Text = "Save draft" };
    internal Button Import { get; } = new() { Text = "Import JSON…" };
    internal Button Export { get; } = new() { Text = "Export JSON…" };
    internal LineEdit Url { get; } = new() { PlaceholderText = "https://marvelcdb.com/decklist/view/…", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Fetch { get; } = new() { Text = "Import URL" };
    internal Button CancelFetch { get; } = new() { Text = "Cancel import", Disabled = true };
    internal LineEdit Search { get; } = new() { PlaceholderText = "Search title, trait or cost", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal OptionButton Aspect { get; } = new();
    internal OptionButton Kind { get; } = new();
    internal ItemList Cards { get; } = new() { Name = "CardBrowser", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(300, 160) };
    internal ItemList Deck { get; } = new() { Name = "DraftCards", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(300, 160) };
    internal Button Add { get; } = new() { Text = "Add selected card +1" };
    internal Button Remove { get; } = new() { Text = "Remove selected copy −1" };
    internal Label Count { get; } = new() { Text = "Choose a deck" };
    internal TextEdit Inspector { get; } = new() { Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, CustomMinimumSize = new(0, 100) };
    internal TextEdit Diagnostics { get; } = new() { Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, CustomMinimumSize = new(0, 90) };
    internal Label Status { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Button SeatOne { get; } = new() { Text = "Use for seat 1", Disabled = true };
    internal Button SeatTwo { get; } = new() { Text = "Use for seat 2", Disabled = true };
    internal Button Close { get; } = new() { Text = "Back to setup" };
    internal ConfirmationDialog Confirm { get; } = new() { Title = "Unsaved changes", DialogText = "Discard changes to this draft?", OkButtonText = "Discard changes" };
    internal FileDialog File { get; } = new() { Access = FileDialog.AccessEnum.Filesystem, Filters = ["*.json ; JSON deck files"], UseNativeDialog = true };

    internal DeckEditorControls(Main main)
    {
        Window.Theme = main.Theme;
        foreach (var control in new Control[] { Cards, Deck, Inspector, Diagnostics })
        {
            control.AddThemeFontOverride("font", CardTypography.Body);
            control.AddThemeFontSizeOverride("font_size", VisualSystem.Type(main.interfaceScale).Body);
            control.AddThemeColorOverride("font_color", ClientTheme.ToGodot(VisualSystem.Palette.Text));
            control.AddThemeColorOverride("font_readonly_color", ClientTheme.ToGodot(VisualSystem.Palette.Text));
        }
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 14);
        Window.AddChild(margin);
        var outer = new VBoxContainer();
        margin.AddChild(outer);
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        outer.AddChild(scroll);
        var stack = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(stack);
        Row(stack, new Label { Text = "Saved decks" }, Library, Load, Delete);
        Row(stack, new Label { Text = "Editing" }, Name, Hero);
        var actions = new HFlowContainer();
        stack.AddChild(actions);
        foreach (var button in new[] { Starter, Empty, Copy, Save, Import, Export }) actions.AddChild(button);
        Row(stack, Url, Fetch, CancelFetch);
        Row(stack, Search, Aspect, Kind);
        var columns = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, 230) };
        stack.AddChild(columns);
        var browse = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var chosen = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        columns.AddChild(browse);
        columns.AddChild(chosen);
        browse.AddChild(new Label { Text = "Supported Core cards — select to inspect" });
        browse.AddChild(Cards);
        browse.AddChild(Add);
        chosen.AddChild(Count);
        chosen.AddChild(Deck);
        chosen.AddChild(Remove);
        stack.AddChild(Inspector);
        outer.AddChild(Diagnostics);
        outer.AddChild(Status);
        Row(outer, SeatOne, SeatTwo, Close);
        Window.AddChild(Confirm);
        Window.AddChild(File);
        main.AddChild(Window);
    }

    private static void Row(VBoxContainer parent, params Control[] controls)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        foreach (var control in controls) row.AddChild(control);
    }
}
