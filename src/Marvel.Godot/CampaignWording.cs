using System.Globalization;
using Marvel.Rules.Campaign;

namespace Marvel.Godot;

/// <summary>How the campaign screens state the campaign log's typed facts: an icon, a short label and a tooltip.</summary>
/// <remarks>
/// The campaign state decides every fact; the words, icons and the length of a label are our choices.
/// A phase's tooltip holds the sentence the screen no longer prints.
/// </remarks>
internal static class CampaignWording
{
    /// <summary>The product's display name; the client supports one campaign product.</summary>
    internal const string Product = "Rise of Red Skull";

    /// <summary>Campaign difficulty, which decides persistent hit points and recovery.</summary>
    internal static Wording Difficulty(bool expert) => expert
        ? new("", "Expert campaign", "Expert campaign: hit points carry between scenarios, defeated players take a recovery "
            + "obligation to rejoin, and a defeat against Red Skull ends the campaign.")
        : new("", "Standard campaign", "Standard campaign: every scenario starts at full hit points.");

    /// <summary>Scenario difficulty, chosen per attempt and independent of the campaign's.</summary>
    internal static Wording ScenarioMode(bool expert) => new("", expert ? "Expert scenario" : "Standard scenario",
        "Scenario difficulty for the next attempt. It is independent of campaign difficulty.");

    /// <summary>The current phase, with the sentence that explains it on hover.</summary>
    internal static Wording Phase(CampaignPhase phase) => phase switch
    {
        CampaignPhase.Preparing => new("", "Preparing",
            "Prepare your decks, choose scenario difficulty, then begin. Campaign cards are added automatically."),
        CampaignPhase.Playing => new("", "Playing", "Your current game is saved after every accepted choice."),
        CampaignPhase.Rewards => new("", "Rewards", "Victory. Resolve the campaign rewards before preparing the next scenario."),
        CampaignPhase.Retry => new("", "Retry", "The attempt ended in defeat. Consumed campaign cards stay consumed when you retry."),
        CampaignPhase.Complete => new("", "Complete", "Campaign complete. Red Skull has been defeated."),
        CampaignPhase.Failed => new("", "Failed", "The expert campaign ended in defeat against Red Skull."),
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };

    /// <summary>A scenario's display name.</summary>
    internal static string Stage(CampaignStage stage) => stage switch
    {
        CampaignStage.Crossbones => "Crossbones",
        CampaignStage.AbsorbingMan => "Absorbing Man",
        CampaignStage.Taskmaster => "Taskmaster",
        CampaignStage.Zola => "Zola",
        CampaignStage.RedSkull => "Red Skull",
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    /// <summary>The scenario tracker: the current scenario, one pip per scenario through it, the sequence on hover.</summary>
    internal static TrackerState StageTracker(CampaignStage current)
    {
        CampaignStage[] all = Enum.GetValues<CampaignStage>();
        return new(Stage(current), Array.IndexOf(all, current) + 1, all.Length, null, 0,
            "Scenario " + Number(Array.IndexOf(all, current) + 1) + " of " + Number(all.Length) + ": "
            + string.Join(" → ", all.Select(stage => stage == current ? "[" + Stage(stage) + "]" : Stage(stage))));
    }

    /// <summary>Which attempt at the current scenario this is.</summary>
    internal static Wording Attempt(int attempt) => new("", "Attempt " + Number(attempt),
        "Attempt " + Number(attempt) + " at the current scenario.");

    /// <summary>Delay counters the campaign log records for a later scenario.</summary>
    internal static Wording Delay(int counters) => new("", "Delay " + Number(counters),
        "Delay counters recorded in the campaign log; a later scenario's setup uses them.");

    /// <summary>The campaign is archived: readable, not playable.</summary>
    internal static readonly Wording Archived = new("retained", "Archived",
        "Archived: the records are preserved. Restore this campaign to continue playing.");

    /// <summary>A roster seat.</summary>
    internal static Wording Seat(int seat) => new("your_seat", "P" + Number(seat + 1), "Player " + Number(seat + 1));

    /// <summary>An expert campaign's recorded hit points against the hero's printed maximum.</summary>
    internal static Wording HitPoints(int recorded, int maximum) => new("heal", Number(recorded) + "/" + Number(maximum),
        "Recorded hit points: " + Number(recorded) + " of " + Number(maximum) + ". The next scenario starts here.");

    /// <summary>A defeated expert player owes a recovery obligation to rejoin.</summary>
    internal static readonly Wording Recovery = new("departing", "Recovery",
        "Defeated last scenario: take a recovery obligation during setup to rejoin.");

    /// <summary>A defeated expert player sits out the next attempt.</summary>
    internal static readonly Wording SittingOut = new("out_of_reach", "Sitting out", "This player does not join the next attempt.");

    /// <summary>A card list in the campaign log: its short label and what it means.</summary>
    internal static Wording Section(CampaignSection section) => section switch
    {
        CampaignSection.PlayerCards => new("", "Cards", "Campaign cards added to this player's deck."),
        CampaignSection.Weapons => new("attack", "Weapons", "Experimental weapons shuffled into later encounter decks."),
        CampaignSection.Consumed => new("exhaust", "Consumed", "Consumed upgrades: unavailable for the rest of this campaign."),
        CampaignSection.Imprisoned => new("out_of_reach", "Imprisoned", "Imprisoned allies: excluded from every deck."),
        _ => throw new ArgumentOutOfRangeException(nameof(section)),
    };

    /// <summary>The host's corrections that block beginning, counted; each correction is whole on hover.</summary>
    internal static Wording Problems(IReadOnlyList<string> problems) => new("sync_stale", "To fix " + Number(problems.Count),
        string.Join("\n", problems));

    /// <summary>A reward a seat chooses after a victory.</summary>
    internal static Wording Reward(CampaignRewardKind kind) => kind switch
    {
        CampaignRewardKind.Tech => new("", "Upgrade", "Choose a campaign upgrade."),
        CampaignRewardKind.Condition => new("", "Condition", "Choose a Basic Condition upgrade, or decline."),
        CampaignRewardKind.ImproveCondition => new("", "Improve condition", "Improve your Condition upgrade, or decline."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>A reward the seat may decline.</summary>
    internal static readonly Wording OptionalReward = new("", "Optional", "This reward may be declined.");

    /// <summary>A saved campaign whose last choice awaits recovery.</summary>
    internal static readonly Wording AwaitingRecovery = new("sync_stale", "Recover choice",
        "A choice was awaiting confirmation. Open the campaign to recover that exact choice.");

    /// <summary>A saved campaign another version wrote, which this one can only delete.</summary>
    internal static readonly Wording DeleteOnly = new("out_of_reach", "Delete only", "Saved by another version: it can only be deleted.");

    /// <summary>A saved campaign whose progress has not been read yet.</summary>
    internal static readonly Wording NotLoaded = new("", "Not loaded", "Open the campaign to load its progress.");

    /// <summary>An empty campaign list.</summary>
    internal static string Empty(bool archived) => archived ? "No archived campaigns" : "No campaigns";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
