using Godot;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
namespace Marvel.Godot;

/// <summary>Explains campaign authority and displays only the current phase's useful actions.</summary>
internal static class CampaignPresentation
{
    internal static void Render(CampaignControls ui, CampaignDescriptor campaign, DeckCatalog catalog,
        Action<CampaignQuery> choose, Action<int> edit, Action resume)
    {
        ui.Clear();
        var state = campaign.State;
        ui.Body.AddChild(CampaignControls.Text($"Rise of Red Skull · {(state.Expert ? "Expert" : "Standard")} campaign"));
        ui.Body.AddChild(CampaignControls.Text(string.Join("  →  ", Enum.GetValues<CampaignStage>().Select(stage =>
            (stage == state.Stage ? "▶ " : "") + Name(stage)))));
        ui.Body.AddChild(CampaignControls.Text(Phase(state.Phase)));
        foreach (var pair in state.Players.Select((player, seat) => (player, seat)))
            Player(ui, pair.player, pair.seat, state, catalog, edit, choose);
        if (!state.BannedAllyFaces.IsEmpty) ui.Body.AddChild(CampaignControls.Text("Imprisoned: " + string.Join(", ", state.BannedAllyFaces.Select(face => Title(catalog, face)))));
        if (state.DelayCounters > 0) ui.Body.AddChild(CampaignControls.Text($"Recorded delay: {state.DelayCounters}"));
        ShowActions(ui, campaign, catalog, choose, resume);
    }

    private static void Player(CampaignControls ui, CampaignPlayerState player, int seat, CampaignState state,
        DeckCatalog catalog, Action<int> edit, Action<CampaignQuery> choose)
    {
        string name = catalog.Heroes.FirstOrDefault(hero => hero.Key == player.Hero)?.Name ?? player.Hero;
        string hp = state.Expert && player.RecordedHitPoints is { } value ? $" · recorded HP {value}/{player.BaseHitPoints}" : "";
        ui.Body.AddChild(CampaignControls.Text($"Player {seat + 1} · {name}{hp}"));
        if (player.RequiresRecovery) ui.Body.AddChild(CampaignControls.Text("Defeated last scenario: take a recovery obligation during setup to rejoin."));
        if (!player.Cards.IsEmpty) ui.Body.AddChild(CampaignControls.Text("Campaign cards: " + string.Join(", ", player.Cards.Select(card => Title(catalog, card.Face)))));
        if (state.Phase == CampaignPhase.Preparing && player.RequiresRecovery)
        {
            var participate = new CheckButton { Text = $"Player {seat + 1} joins this attempt", ButtonPressed = player.Participating };
            participate.Toggled += selected => choose(new("participation") { Seat = seat, Participating = selected });
            ui.Body.AddChild(participate);
        }
        if (state.Phase == CampaignPhase.Preparing)
            CampaignControls.Action(ui.Body, $"Edit {name}'s deck · {player.Deck.Cards.Count} cards", () => edit(seat));
    }

    private static void ShowActions(CampaignControls ui, CampaignDescriptor campaign, DeckCatalog catalog,
        Action<CampaignQuery> choose, Action resume)
    {
        switch (campaign.State.Phase)
        {
            case CampaignPhase.Preparing:
                var mode = new CheckButton { Text = "Expert scenario", ButtonPressed = campaign.State.ExpertScenario,
                    TooltipText = "Scenario difficulty is independent of campaign difficulty." };
                mode.Toggled += selected => choose(new("mode") { Expert = selected });
                ui.Body.AddChild(mode);
                foreach (string problem in campaign.PreparationProblems) ui.Body.AddChild(CampaignControls.Text(problem));
                CampaignControls.Action(ui.Body, "Begin " + Name(campaign.State.Stage), () => choose(new("start")))
                    .Disabled = campaign.PreparationProblems.Count > 0;
                break;
            case CampaignPhase.Playing: CampaignControls.Action(ui.Body, "Return to current game", resume); break;
            case CampaignPhase.Retry: CampaignControls.Action(ui.Body, "Prepare another attempt", () => choose(new("retry"))); break;
            case CampaignPhase.Rewards: Rewards(ui, campaign.Reward!, catalog, choose); break;
        }
    }

    private static void Rewards(CampaignControls ui, CampaignRewardChoice reward, DeckCatalog catalog, Action<CampaignQuery> choose)
    {
        ui.Body.AddChild(CampaignControls.Text($"Player {reward.Seat + 1}: choose {reward.Kind}"));
        foreach (var card in reward.Options)
        {
            var printed = catalog.Cards.FirstOrDefault(item => item.Face == card.Face);
            var button = CampaignControls.Action(ui.Body, printed?.Title ?? card.Face,
                () => choose(new("reward") { Seat = reward.Seat, Choice = card.Key }));
            button.TooltipText = printed?.Text ?? "";
            if (printed is not null) ui.Body.AddChild(CampaignControls.Text(printed.Text));
        }
        if (reward.Optional) CampaignControls.Action(ui.Body, "Decline this optional reward", () => choose(new("reward") { Seat = reward.Seat }));
    }

    private static string Title(DeckCatalog catalog, string face) => catalog.Cards.FirstOrDefault(card => card.Face == face)?.Title ?? face;
    private static string Name(CampaignStage stage) => stage switch
    { CampaignStage.AbsorbingMan => "Absorbing Man", CampaignStage.RedSkull => "Red Skull", _ => stage.ToString() };
    private static string Phase(CampaignPhase phase) => phase switch
    {
        CampaignPhase.Preparing => "Prepare your decks, choose scenario difficulty, then begin. Campaign cards are added automatically.",
        CampaignPhase.Playing => "Your current game is saved after every accepted choice.",
        CampaignPhase.Rewards => "Victory. Resolve the campaign rewards below before preparing the next scenario.",
        CampaignPhase.Retry => "The attempt ended in defeat. Consumed campaign cards remain consumed when you retry.",
        CampaignPhase.Complete => "Campaign complete. Red Skull has been defeated.",
        CampaignPhase.Failed => "The expert campaign ended in defeat against Red Skull.",
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };
}
