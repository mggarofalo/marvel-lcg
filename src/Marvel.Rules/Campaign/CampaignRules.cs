using System.Collections.Immutable;
using Marvel.Rules.Setup;

namespace Marvel.Rules.Campaign;

/// <summary>Authoritative Rise of Red Skull preparation and attempt lifecycle.</summary>
public static class CampaignRules
{
    /// <summary>Starts an identity-locked campaign from content-admitted participants.</summary>
    public static CampaignState Create(
        CampaignDefinition definition, IReadOnlyList<CampaignRosterEntry> roster,
        bool expert, uint seed)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(roster);
        PlayerCountRules.Validate(roster.Count);
        if (definition.Scenarios.Length != 5 || definition.ObligationFaces.Length != 4)
            throw new ArgumentException("the complete Rise of Red Skull composition is required", nameof(definition));
        if (roster.Any(player => string.IsNullOrWhiteSpace(player.Hero) || player.BaseHitPoints <= 0))
            throw new ArgumentException("every campaign seat requires an admitted identity", nameof(roster));
        // pack:mc10:campaign-mode-rules: "Each player must use their chosen identity for the entire campaign."
        var players = roster.Select((entry, seat) => new CampaignPlayerState(
            entry.Hero, new PlayerDeckRecipe(entry.Deck.Cards), entry.BaseHitPoints,
            null, false, [],
            [.. definition.ObligationFaces.Select((face, index) => new CampaignCard(
                $"obligation:{seat}:{index}", face, CampaignCardRole.Obligation))], false)).ToImmutableArray();
        return new CampaignState(definition.Product, seed, expert, CampaignStage.Crossbones,
            CampaignPhase.Preparing, 0, false, players, [], [], [], 0, [], null);
    }

    /// <summary>Sets scenario difficulty independently of campaign difficulty.</summary>
    public static CampaignState SelectScenarioMode(CampaignState state, bool expert)
    {
        RequirePhase(state, CampaignPhase.Preparing);
        // rr:modes-of-play.11: "The difficulty of the campaign mode does not require players to play any other specific game mode."
        return state with { ExpertScenario = expert };
    }

    /// <summary>Captures a recipe after the owning content boundary has assessed campaign deck legality.</summary>
    public static CampaignState ReplaceDeck(CampaignState state, int seat, PlayerDeckRecipe recipe)
    {
        RequirePhase(state, CampaignPhase.Preparing);
        RequireSeat(state, seat);
        if (recipe.Cards.Any(state.BannedAllyFaces.Contains))
            throw new ArgumentException("a removed ally cannot return to the campaign", nameof(recipe));
        return state with { Players = state.Players.SetItem(seat,
            state.Players[seat] with { Deck = new PlayerDeckRecipe(recipe.Cards) }) };
    }

    /// <summary>Builds exact carry-over instructions without mutating state or consuming randomness.</summary>
    public static CampaignGameSetup Setup(CampaignState state)
    {
        RequirePhase(state, CampaignPhase.Preparing);
        int threat = state.Stage == CampaignStage.RedSkull
            ? checked(state.DelayCounters * (state.Expert ? state.Players.Length : 1)) : 0;
        return new CampaignGameSetup(state.Product, state.Expert, state.Stage, state.Attempt,
            [.. state.Players.Select((player, seat) => new CampaignSeatSetup(seat,
                player.Cards, state.Expert ? player.RecordedHitPoints : null,
                state.Expert && player.RequiresRecovery, player.AvailableObligations,
                state.Expert && state.Stage == CampaignStage.RedSkull && player.EngagedWithEnemy))],
            state.ExperimentalWeapons, threat);
    }

    /// <summary>Marks the attempt active in the same transaction as the initial game save.</summary>
    public static CampaignState Start(CampaignState state)
    {
        RequirePhase(state, CampaignPhase.Preparing);
        return state with { Phase = CampaignPhase.Playing };
    }

    /// <summary>Resets a retryable scenario while retaining every persistent campaign change.</summary>
    public static CampaignState Retry(CampaignState state)
    {
        RequirePhase(state, CampaignPhase.Retry);
        // rr:modes-of-play.10: removed cards remain unusable "even if players retry the scenario".
        return state with
        {
            Phase = CampaignPhase.Preparing, Attempt = checked(state.Attempt + 1),
            Players = [.. state.Players.Select(player => player with
            { RequiresRecovery = state.Expert && player.RecordedHitPoints == 0 })],
        };
    }

    internal static void RequirePhase(CampaignState state, CampaignPhase phase)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != phase)
            throw new InvalidOperationException($"campaign is {state.Phase}, not {phase}");
    }

    internal static void RequireSeat(CampaignState state, int seat)
    {
        if (seat < 0 || seat >= state.Players.Length)
            throw new ArgumentOutOfRangeException(nameof(seat));
    }
}
