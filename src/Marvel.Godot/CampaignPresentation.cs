using Godot;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
namespace Marvel.Godot;

/// <summary>States the campaign log as chips and trackers and offers only the current phase's commitments.</summary>
internal static class CampaignPresentation
{
    internal static void Render(CampaignControls ui, CampaignDescriptor campaign, DeckCatalog catalog,
        Action<CampaignQuery> choose, Action<int> edit, Action resume)
    {
        ui.Clear();
        var state = campaign.State;
        InterfaceScale scale = ui.Scale;
        ui.Body.AddChild(CampaignBlocks.Title("CampaignName", campaign.Name, scale, CampaignWording.Product));
        HFlowContainer facts = CampaignBlocks.Row("CampaignFacts", scale);
        facts.AddChild(Tracker.Create("CampaignStage", CampaignWording.StageTracker(state.Stage), scale));
        CampaignBlocks.Chips(facts, "CampaignFact", Facts(campaign), scale);
        ui.Body.AddChild(facts);
        ui.Body.AddChild(new HSeparator());
        foreach (var (player, seat) in state.Players.Select((player, seat) => (player, seat)))
            ui.Body.AddChild(CampaignPlayerRow.Create(ui, player, seat, campaign, catalog, edit, choose));
        foreach (var (section, faces) in new (CampaignSection, IEnumerable<string>)[]
                 {
                     (CampaignSection.Weapons, state.ExperimentalWeapons),
                     (CampaignSection.Consumed, campaign.RemovedCards.Select(card => card.Face)),
                     (CampaignSection.Imprisoned, state.BannedAllyFaces),
                 })
            if (CampaignCardLinks.Section(ui, section, faces, catalog) is { } row) ui.Body.AddChild(row);
        if (!campaign.Archived) ShowActions(ui, campaign, catalog, choose, resume);
    }

    /// <summary>The campaign's facts beside its scenario tracker, in reading order.</summary>
    internal static IEnumerable<Wording> Facts(CampaignDescriptor campaign)
    {
        var state = campaign.State;
        if (campaign.Archived) yield return CampaignWording.Archived;
        yield return CampaignWording.Phase(state.Phase);
        yield return CampaignWording.Difficulty(state.Expert);
        // While preparing, the scenario difficulty is its own toggle below.
        if (campaign.Archived || state.Phase != CampaignPhase.Preparing) yield return CampaignWording.ScenarioMode(state.ExpertScenario);
        if (state.Attempt > 1) yield return CampaignWording.Attempt(state.Attempt);
        if (state.DelayCounters > 0) yield return CampaignWording.Delay(state.DelayCounters);
    }

    private static void ShowActions(CampaignControls ui, CampaignDescriptor campaign, DeckCatalog catalog,
        Action<CampaignQuery> choose, Action resume)
    {
        InterfaceScale scale = ui.Scale;
        HFlowContainer actions = CampaignBlocks.Row("CampaignActions", scale);
        switch (campaign.State.Phase)
        {
            case CampaignPhase.Preparing:
                var mode = new CheckButton { Text = "Expert scenario", ButtonPressed = campaign.State.ExpertScenario,
                    TooltipText = CampaignWording.ScenarioMode(campaign.State.ExpertScenario).Tooltip };
                mode.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
                mode.Toggled += selected => choose(new("mode") { Expert = selected });
                actions.AddChild(mode);
                if (campaign.PreparationProblems.Count > 0)
                    actions.AddChild(Chip.Create("PreparationProblems", CampaignWording.Problems(campaign.PreparationProblems), null, scale));
                CampaignBlocks.Action(actions, "Begin " + CampaignWording.Stage(campaign.State.Stage), () => choose(new("start")), scale)
                    .Disabled = campaign.PreparationProblems.Count > 0;
                break;
            case CampaignPhase.Playing: CampaignBlocks.Action(actions, "Return to current game", resume, scale); break;
            case CampaignPhase.Retry: CampaignBlocks.Action(actions, "Prepare another attempt", () => choose(new("retry")), scale); break;
            case CampaignPhase.Rewards: Rewards(ui, campaign.Reward!, catalog, choose, actions); break;
        }
        if (actions.GetChildCount() > 0) ui.Body.AddChild(actions);
    }

    private static void Rewards(CampaignControls ui, CampaignRewardChoice reward, DeckCatalog catalog,
        Action<CampaignQuery> choose, HFlowContainer actions)
    {
        InterfaceScale scale = ui.Scale;
        Control[] facts = [Chip.Create("RewardSeat", CampaignWording.Seat(reward.Seat), null, scale),
            Chip.Create("RewardKind", CampaignWording.Reward(reward.Kind), null, scale),
            .. reward.Optional ? [Chip.Create("RewardOptional", CampaignWording.OptionalReward, null, scale)] : Array.Empty<Control>()];
        ui.Body.AddChild(TablePanel.Create("RewardHead", CampaignBlocks.Heading("RewardTitle", "Reward", scale), facts, "", scale));
        foreach (var card in reward.Options)
        {
            var printed = catalog.Cards.FirstOrDefault(item => item.Face == card.Face);
            var button = CampaignBlocks.Action(actions, printed?.Title ?? card.Face,
                () => choose(new("reward") { Seat = reward.Seat, Choice = card.Key }), scale);
            if (printed is not null) CampaignCardLinks.Bind(ui, button, printed);
        }
        if (reward.Optional) CampaignBlocks.Action(actions, "Decline", () => choose(new("reward") { Seat = reward.Seat }), scale);
    }

    internal static string Name(CampaignStage stage) => CampaignWording.Stage(stage);
}
