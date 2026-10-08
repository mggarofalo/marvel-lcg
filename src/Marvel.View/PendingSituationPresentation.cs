using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Explains public waiting context without reading another seat's prompt.</summary>
public static class PendingSituationPresentation
{
    /// <summary>Names the answering seat and the public purpose.</summary>
    public static string Heading(WorldDescriptor world) => world.Outcome switch
    {
        Outcome.PlayersWin => "Victory",
        Outcome.Unfinished => world.Table?.PromptOwner is { } owner
            ? $"Waiting for {SeatName(world, owner)} · {Purpose(world.Table.PendingSituation?.Kind)}"
            : "Waiting for another player",
        _ => "Defeat",
    };

    /// <summary>Describes public causal objects, without option counts or hidden placeholders.</summary>
    public static string Context(WorldDescriptor world)
    {
        if (world.Outcome != Outcome.Unfinished)
            return world.Outcome == Outcome.PlayersWin ? "The players won." : "The players lost.";
        return UnfinishedContext(world, world.Table?.PendingSituation);
    }

    private static string UnfinishedContext(WorldDescriptor world, PendingSituationDescriptor? pending)
    {
        string[] sources = ReadableTitles(world, pending?.SourceCardIds ?? []);
        string relation = pending?.Kind == PublicDecisionKind.MinionActivationOrder
            ? "Choosing the next activation among" : "Resolving";
        string[] causes = ReadableTitles(world, pending?.CauseCardIds ?? []);
        string cause = causes.Length == 0 ? string.Empty : $"Cause: {string.Join(", ", causes)}. ";
        return cause + (sources.Length == 0 ? "" : $"{relation} {string.Join(" · ", sources)}. ")
            + "You can inspect the public table. Refresh to receive other players' latest actions.";
    }

    private static string[] ReadableTitles(WorldDescriptor world, IReadOnlyList<int> ids) =>
        [.. ids.Select(id => world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(card => card.Id == id && card.Face is not null)?.Face?.Title)
            .OfType<string>().Distinct()];

    /// <summary>Names a public seat.</summary>
    public static string SeatName(WorldDescriptor world, int seat) =>
        world.Players.FirstOrDefault(player => player.Seat == seat)?.Name ?? $"Player {seat + 1}";

    private static string Purpose(PublicDecisionKind? kind) => kind switch
    {
        PublicDecisionKind.PlayerAction => "player actions",
        PublicDecisionKind.OpeningHand => "opening hand",
        PublicDecisionKind.EndPhaseDiscards => "end-phase discards",
        PublicDecisionKind.Interrupt => "interrupt opportunity",
        PublicDecisionKind.Response => "response opportunity",
        PublicDecisionKind.Ability => "ability opportunity",
        PublicDecisionKind.Defense => "defense choice",
        PublicDecisionKind.Order => "ordering choice",
        PublicDecisionKind.MinionActivationOrder => "next minion activation",
        PublicDecisionKind.SpecialAbilityNext => "next Special ability",
        PublicDecisionKind.VisibleCardSelection => "card selection",
        PublicDecisionKind.CardSearch => "card search",
        PublicDecisionKind.CardLook => "card selection",
        _ => "current choice",
    };
}
