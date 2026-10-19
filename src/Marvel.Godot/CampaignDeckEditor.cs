using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Reuses the deck workspace with fixed campaign identity and engine-authenticated construction exceptions.</summary>
internal static class CampaignDeckEditor
{
    internal static void Open(Main main, CampaignControls tracker, CampaignConnection connection,
        CampaignDescriptor campaign, DeckCatalog catalog, int seat, Action<CampaignQuery> choose)
    {
        var player = campaign.State.Players[seat];
        var banned = campaign.State.BannedAllyFaces;
        var hero = catalog.Heroes.Single(item => item.Key == player.Hero);
        var fixedHero = hero with { Signature = hero.Signature.Where(face => !banned.Contains(face)).ToArray(),
            Starter = hero.Starter.Where(face => !banned.Contains(face)).ToArray() };
        var filtered = new DeckCatalog([fixedHero], catalog.Cards.Select(card => banned.Contains(card.Face)
            ? card with { RuntimeSupported = false } : card).ToArray());
        var connected = ClientComposition.Connect(DesktopDataRoot.Current(), connection.Endpoint);
        if (!connected.Succeeded) { tracker.Status.Text = connected.Error!.Message; return; }
        var client = connected.Client!;
        client.CampaignDeck = new(connection.CampaignId!, connection.Capability!);
        var workspace = new DeckWorkspace(filtered);
        workspace.Initialize(new(hero.Name + " campaign deck", hero.IdentityFaces[0], player.Deck.Cards));
        var ui = new DeckEditorControls(main);
        ui.Window.Title = hero.Name + " campaign deck";
        ui.Seat.Clear(); ui.Seat.AddItem($"Player {seat + 1}"); ui.Seat.Disabled = true;
        var store = new LocalDeckStore(System.Environment.GetEnvironmentVariable("MARVEL_DECK_LIBRARY_DIRECTORY")
            ?? ProjectSettings.GlobalizePath("user://decks"));
        var editor = new DeckEditorController(ui, client, workspace, store, (draft, _) =>
        {
            if (!hero.IdentityFaces.Contains(draft.HeroCode, StringComparer.Ordinal))
            { tracker.Status.Text = "The campaign identity cannot change."; return; }
            choose(new("deck") { Seat = seat, Deck = new(draft.Cards) });
        });
        tracker.Window.Hide();
        ui.Window.TreeExiting += () =>
        {
            if (GodotObject.IsInstanceValid(tracker.Window) && !main.IsQueuedForDeletion()) tracker.Window.PopupCentered();
        };
        editor.Show();
        ui.Hero.Disabled = true;
    }
}
