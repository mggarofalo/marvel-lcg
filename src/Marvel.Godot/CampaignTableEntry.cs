using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Installs a resumed campaign attempt through the ordinary validated table lifecycle.</summary>
internal static class CampaignTableEntry
{
    internal static async Task<string?> OpenAsync(Main main, CampaignConnection connection, CampaignDescriptor campaign)
    {
        if (connection.Game is not { } game) return "The current game authority is missing. Recover its start request.";
        var connected = ClientComposition.Connect(DesktopDataRoot.Current(), connection.Endpoint);
        if (!connected.Succeeded) return connected.Error!.Message;
        var client = connected.Client!;
        var synchronized = await client.SynchronizeAsync(game);
        if (synchronized.Response is not { } response || synchronized.Error is not null)
        { client.Dispose(); return synchronized.Error?.Message ?? "The campaign table could not be loaded."; }
        long ticket = main.lifecycle.BeginEntry();
        var entry = new ClientEntryResult(game, response, [], null);
        if (!main.lifecycle.Enter(ticket, client, entry)) { client.Dispose(); return "The table changed while this game was loading."; }
        main.boardController.ResetForSession();
        main.currentProgress = null;
        main.boardController.RenderGame(response, resetEvents: true, operation: EngineProtocol.Sync);
        main.setupPanel.Visible = false;
        main.board.Visible = true;
        main.eyebrow.Text = $"RISE OF RED SKULL / {campaign.State.Stage} / ATTEMPT {campaign.State.Attempt + 1}";
        main.description.Visible = true;
        main.ApplyResponsivePlayLayout();
        main.pageScroll.ScrollVertical = 0;
        return null;
    }
}
