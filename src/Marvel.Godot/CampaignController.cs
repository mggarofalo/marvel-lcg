using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Owns native campaign navigation, retained commands, and local resume credentials.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The Main scene owns controls and disposes connections on TreeExiting.")]
internal sealed class CampaignController
{
    private readonly Main main;
    private readonly Button open = new() { Text = "Campaigns", Visible = false };
    private readonly CampaignControls ui;
    private CampaignConnectionStore? store;
    private IReadOnlyList<CampaignConnection> saved = [];
    private CampaignConnection? connection;
    private CampaignDescriptor? campaign;
    private LocalGameClient? client;
    private DeckCatalog? catalog;
    private bool busy;

    internal CampaignController(Main main)
    {
        this.main = main;
        ui = new(main);
        main.setupPanel.GetParent().AddChild(open);
        main.setupPanel.GetParent().MoveChild(open, main.setupPanel.GetIndex());
        open.Pressed += Show;
        ui.New.Pressed += New;
        ui.Open.Pressed += ResumeSaved;
        ui.Refresh.Pressed += () => Choose(new("sync"));
        ui.Retry.Pressed += Recover;
        var timer = new global::Godot.Timer { WaitTime = 0.5, Autostart = true };
        timer.Timeout += () => open.Visible = main.setupChoices?.Campaigns.Count > 0 || saved.Count > 0;
        main.AddChild(timer);
        main.TreeExiting += () => { client?.Dispose(); store?.Dispose(); };
    }

    private void Show()
    {
        if (busy) return;
        try
        {
            store ??= new CampaignConnectionStore(System.Environment.GetEnvironmentVariable("MARVEL_CAMPAIGN_ACCESS_DIRECTORY")
                ?? ProjectSettings.GlobalizePath("user://campaign-access"));
            saved = store.Load();
            ReloadSaved();
            ui.Window.PopupCentered();
            if (connection is null)
            {
                New();
            }
            else Choose(new("sync"));
        }
        catch (Exception) { ui.Status.Text = "Campaign access files could not be opened. Existing saves have been preserved."; }
    }

    private async Task<bool> Connect(string endpoint)
    {
        client?.Dispose();
        var connected = ClientComposition.Connect(DesktopDataRoot.Current(), endpoint);
        if (!connected.Succeeded) { ui.Status.Text = connected.Error!.Message; return false; }
        client = connected.Client!;
        var result = await client.ReadDecksAsync();
        if (!result.Succeeded) { ui.Status.Text = result.Error!.Message; return false; }
        catalog = await DeckResearchCatalog.LoadAsync(client, result.Decks!.Catalog!);
        return true;
    }

    private async void New()
    {
        if (busy || connection?.Pending is not null) return;
        connection = null; campaign = null;
        if (!await Connect(main.endpoint.Text) || catalog is null) return;
        string? product = main.setupChoices?.Campaigns is { Count: > 0 } products ? products[0] : null;
        if (product is null) { ui.Status.Text = "This content installation has no complete campaign available."; return; }
        CampaignCreationForm.Render(ui, catalog, product, Create);
    }

    private async void Create(string label, CampaignQuery query)
    {
        if (busy) return;
        connection = new(Guid.NewGuid().ToString("N"), main.endpoint.Text,
            string.IsNullOrWhiteSpace(label) ? "Rise of Red Skull" : label, null, null, null, null);
        campaign = null;
        var request = new EngineRequest(EngineProtocol.Version, Guid.NewGuid().ToString("N"), EngineProtocol.Campaign,
            connection.Label, Entry: EntryClaim.Create()) { Campaign = query };
        await Send(request);
    }

    private async void ResumeSaved()
    {
        if (busy || ui.Saved.Selected < 0 || ui.Saved.Selected >= saved.Count) return;
        connection = saved[ui.Saved.Selected];
        campaign = null;
        if (!await Connect(connection.Endpoint)) return;
        if (connection.Pending is not null) { ui.Status.Text = "A choice was awaiting confirmation. Recover that exact choice before continuing."; RefreshRecovery(); }
        else Choose(new("sync"));
    }

    private async void Choose(CampaignQuery query)
    {
        if (busy || connection is null || client is null) return;
        if (connection.Pending is not null) { RefreshRecovery(); return; }
        var request = new EngineRequest(EngineProtocol.Version, Guid.NewGuid().ToString("N"), EngineProtocol.Campaign,
            connection.CampaignId!, Capability: connection.Capability, ExpectedRevision: campaign?.Revision,
            Entry: query.Action == "start" ? EntryClaim.Create() : null) { Campaign = query };
        await Send(request);
    }

    private async void Recover()
    {
        if (!busy && connection?.Pending is { } pending) await Send(pending);
    }

    private async Task Send(EngineRequest request)
    {
        if (connection is null || client is null || store is null) return;
        busy = true;
        ui.Status.Text = "Saving campaign choice…";
        try
        {
            connection = connection with { Pending = request };
            store.Save(connection);
            var result = await client.ExchangeCampaignAsync(request);
            if (result.Succeeded)
            {
                await Accept(result, request);
            }
            else
            {
                connection = connection with { Pending = result.Pending };
                store.Save(connection);
                if (result.Response?.Campaign is { } latest) { campaign = latest; Render(); }
                ui.Status.Text = result.Error!.Message;
            }
        }
        catch (Exception) { ui.Status.Text = "The campaign result could not be saved locally. Recover this exact choice before continuing."; }
        finally { busy = false; RefreshRecovery(); }
    }

    private async Task Accept(ClientCampaignResult result, EngineRequest request)
    {
        var response = result.Response!;
        var proposed = connection! with { CampaignId = response.Campaign!.Id,
            Capability = request.Campaign!.Action == "create" ? response.Capability : connection!.Capability,
            Game = result.Entry?.Session ?? connection!.Game, Pending = null };
        store!.Save(proposed);
        connection = proposed;
        campaign = response.Campaign;
        saved = store.Load(); ReloadSaved(); Render();
        ui.Status.Text = "Saved";
        if (result.Entry is not null) await OpenTable();
    }

    private void Render()
    {
        if (campaign is null || catalog is null) return;
        CampaignPresentation.Render(ui, campaign, catalog, Choose, seat =>
            CampaignDeckEditor.Open(main, ui, connection!, campaign, catalog, seat, Choose), ResumeTable);
    }

    private async void ResumeTable()
    {
        if (!busy) await OpenTable();
    }

    private async Task OpenTable()
    {
        if (connection is null || campaign is null) return;
        string? error = await CampaignTableEntry.OpenAsync(main, connection, campaign);
        if (error is null) ui.Window.Hide(); else ui.Status.Text = error;
    }

    private void RefreshRecovery()
    {
        ui.Retry.Visible = connection?.Pending is not null;
        ui.Retry.Disabled = busy;
        ui.New.Disabled = ui.Open.Disabled = ui.Refresh.Disabled = busy || connection?.Pending is not null;
    }

    private void ReloadSaved()
    {
        ui.Saved.Clear();
        foreach (var item in saved) ui.Saved.AddItem(item.Label);
        int current = saved.ToList().FindIndex(item => item.LocalId == connection?.LocalId);
        if (current >= 0) ui.Saved.Select(current);
    }
}
