using Godot;

namespace Marvel.Godot;

/// <summary>The deck library's native controls and keyboard-accessible editing surface.</summary>
internal sealed class DeckEditorControls
{
    internal Window Window { get; } = new() { Name = "DeckLibrary", Title = "Deck library", Size = new(1720, 940), MinSize = new(1000, 600), Transient = true, Exclusive = true, GuiEmbedSubwindows = true };
    internal LineEdit Name { get; } = new() { PlaceholderText = "Deck name", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal OptionButton Hero { get; } = new() { Name = "DeckIdentity", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button InspectIdentity { get; } = new() { Text = "Preview identity" };
    internal VBoxContainer Preview { get; } = new() { Name = "DeckCardPreview" };
    internal InterfaceScale Scale { get; }
    internal OptionButton Library { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Load { get; } = new() { Text = "Open saved" };
    internal Button Delete { get; } = new() { Text = "Delete saved" };
    internal Button Starter { get; } = new() { Text = "New starter" };
    internal Button Empty { get; } = new() { Text = "New deck" };
    internal Button Copy { get; } = new() { Text = "Duplicate" };
    internal Button Save { get; } = new() { Text = "Save draft" };
    internal Button Import { get; } = new() { Text = "Import JSON…" };
    internal Button Export { get; } = new() { Text = "Export JSON…" };
    internal LineEdit Url { get; } = new() { Name = "DeckImportUrl", PlaceholderText = "https://marvelcdb.com/decklist/view/…", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Published { get; } = new() { Text = "Find published decks…" };
    internal Button Fetch { get; } = new() { Text = "Import URL" };
    internal Button CancelFetch { get; } = new() { Text = "Cancel import", Disabled = true };
    internal LineEdit Search { get; } = new() { Name = "CardSearch", PlaceholderText = "Search cards", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal OptionButton SearchField { get; } = new();
    internal OptionButton SearchOperator { get; } = new();
    internal LineEdit SearchValue { get; } = new() { PlaceholderText = "Filter value", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button AddFilter { get; } = new() { Text = "Add filter" };
    internal Label SearchError { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal OptionButton Sort { get; } = new();
    internal OptionButton CardScope { get; } = new() { Name = "CardScope" };
    internal OptionButton Aspect { get; } = new();
    internal OptionButton Kind { get; } = new();
    internal DeckCardTable Cards { get; } = new() { Name = "CardBrowser", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(260, 150) };
    internal DeckCardTable Deck { get; } = new() { Name = "DraftCards", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(260, 150) };
    internal Button Add { get; } = new() { Name = "DeckAdd", Text = "Add selected +1", Disabled = true };
    internal Button Remove { get; } = new() { Text = "Clear selected" };
    internal Button Undo { get; } = new() { Text = "Undo", Name = "DeckUndo" };
    internal Button Redo { get; } = new() { Text = "Redo", Name = "DeckRedo" };
    internal Button Clear { get; } = new() { Text = "Clear deck", TooltipText = "Keep only this identity's required signature cards. Undo restores your cards." };
    internal Button ClearSearch { get; } = new() { Text = "×", TooltipText = "Clear search and filters" };
    internal Button SelectAll { get; } = new() { Text = "Select page" };
    internal Button RestoreRequired { get; } = new() { Text = "Restore required cards", TooltipText = "Repair imported signature quantities, preserving all optional cards. Undo restores the import." };
    internal Button PreviousPage { get; } = new() { Text = "‹", TooltipText = "Previous cards" };
    internal Button NextPage { get; } = new() { Text = "›", TooltipText = "Next cards" };
    internal Label Page { get; } = new();
    internal Button SelectDeck { get; } = new() { Text = "Select entire deck" };
    internal Button Deselect { get; } = new() { Text = "Deselect all" };
    internal Label Selection { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Label DropHint { get; } = new() { Text = "Drop cards here to add +1 each", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Label Count { get; } = new() { Text = "Choose a deck" };
    internal TextEdit Inspector { get; } = new() { Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, CustomMinimumSize = new(0, 100) };
    internal TextEdit Diagnostics { get; } = new() { Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, CustomMinimumSize = new(0, 60) };
    internal Label Status { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Button Close { get; } = new() { Text = "Back to setup" };
    internal ConfirmationDialog Confirm { get; } = new() { Title = "Delete saved deck", OkButtonText = "Delete saved deck" };
    internal FileDialog File { get; } = new() { Access = FileDialog.AccessEnum.Filesystem, Filters = ["*.json ; JSON deck files"], UseNativeDialog = true };

    internal OptionButton Seat { get; } = new() { Name = "DeckSeat" };
    internal Button UseDeck { get; } = new() { Name = "UseDeck", Text = "Use deck", Disabled = true };
    internal Button Validity { get; } = new() { Text = "Checking…" };
    internal Label ImportStatus { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Window? ImportDialog { get; set; }
    internal Label SaveState { get; } = new();

    internal DeckEditorControls(Main main)
    {
        ModalWindowFocus.Bind(Window);
        ModalWindowFocus.Bind(Confirm);
        ModalWindowFocus.Bind(File);
        Scale = main.interfaceScale;
        Window.Theme = DeckEditorTheme.Create(Scale);
        Cards.UiScale = Deck.UiScale = Scale;
        DeckEditorLayout.Build(this, main);
    }
}
