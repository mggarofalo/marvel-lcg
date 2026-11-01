using System.Globalization;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Describes the engine's Kang progress facts for an authorized scope.</summary>
/// <remarks>
/// Every value comes from <see cref="KangTable.Of"/>; this projection drops ids the
/// scope cannot address and adds readable copy. The wording is our choice; the notes
/// word the rules each <see cref="KangNoteKind"/> cites; which apply, and to which card, is the
/// engine's answer.
/// </remarks>
internal static class KangTableProjection
{
    internal static KangTableDescriptor? From(World world, WorldDescriptor visible, IReadOnlySet<int> addressable)
    {
        if (KangTable.Of(world) is not { } facts) return null;
        // A departing Kang is in play or removed from the game, and a noted scheme is in play,
        // all public; these filters remove nothing today (equivalent mutants) and keep every id
        // scope-checked.
        int? Visible(int? id) => id is { } known && addressable.Contains(known) ? known : null;
        var departures = facts.Departures.Where(item => addressable.Contains(item.Villain))
            .Select(item => new KangDepartureDescriptor(item.Villain, item.SourceGameArea, item.Seats.ToArray(),
                Visible(item.CompletedScheme), item.Destinations.ToArray(), item.Reunion, item.Summary)).ToArray();
        return new KangTableDescriptor(facts.Stage, facts.RemainingLocalKangs, facts.StoredDominions,
            [.. facts.CentralSchemes.Where(addressable.Contains)], Visible(facts.PendingSplit), departures)
        {
            Acting = facts.Acting is { } acting ? Focus(acting, visible) : null,
            StartingPlayers = world.Players,
            EncounterDeck = world.AreaOf(DeckType.EncounterDeck).Id,
            EncounterDiscard = world.AreaOf(DeckType.EncounterDiscardPile).Id,
            Progress = Progress(facts),
            Notes = [.. facts.Notes.Where(note => note.Card is not { } card || addressable.Contains(card))
                .Select(note => new KangNoteDescriptor(note.Kind, note.Card, Text(note.Kind, world.Players)))],
        };
    }

    private static VillainPhaseFocusDescriptor Focus(VillainPhaseFocus acting, WorldDescriptor visible)
    {
        string step = acting.Step switch
        {
            1 => "threat is placed on the main scheme",
            2 => "the villain and engaged minions activate",
            _ => "encounter cards are revealed",
        };
        string seat = PendingSituationPresentation.SeatName(visible, acting.Seat);
        string? area = visible.GameAreas.FirstOrDefault(item => item.Id == acting.GameArea)?.Label;
        return new(acting.Step, acting.Seat, acting.GameArea,
            $"Villain phase step {acting.Step.ToString(CultureInfo.InvariantCulture)}: {step} for {seat}"
            + (area is null ? "." : $", in {area}."));
    }

    private static string Progress(KangTableFacts facts) => facts.StoredDominions switch
    {
        0 => Stage(facts),
        1 => $"{Stage(facts)} 1 Kang's Dominion is stored under stage 4A.",
        _ => $"{Stage(facts)} {facts.StoredDominions.ToString(CultureInfo.InvariantCulture)} Kang's Dominions are stored under stage 4A.",
    };

    private static string Stage(KangTableFacts facts) => facts.Stage switch
    {
        1 when facts.PendingSplit is not null =>
            "Kang (I) is defeated. At the end of the phase each player forms their own game area against a Kang (II).",
        1 => "Defeat Kang (I) to split the table into separate game areas.",
        2 when facts.RemainingLocalKangs == 0 =>
            "No Kang (II) remains. At the end of the phase the last group leaves its game area and the table reunites "
            + "at stage 4A against Kang (III).",
        2 => $"{facts.RemainingLocalKangs.ToString(CultureInfo.InvariantCulture)} Kang (II) "
            + $"{(facts.RemainingLocalKangs == 1 ? "remains" : "remain")}. A game area's group joins another when its "
            + "Kang (II) is defeated or completes his stage; when no game area remains, the table reunites at stage 4A "
            + "against Kang (III).",
        _ => "Defeat Kang (III) to win.",
    };

    private static string Text(KangNoteKind kind, int players) => kind switch
    {
        KangNoteKind.AreaIsolation =>
            "Cards in one game area cannot affect another game area; only stage 2B's text reaches every area.",
        KangNoteKind.StepsInTurnOrder =>
            "Each villain-phase step resolves for every game area in turn order before the next step begins.",
        KangNoteKind.EachPlayerIsArea =>
            "On an encounter card, \"each player\" means each player in the same game area; a per-player value still "
            + (players == 1 ? "counts the 1 player who started the scenario."
                : $"counts all {players.ToString(CultureInfo.InvariantCulture)} players who started the scenario."),
        KangNoteKind.SharedEncounterDeck => "All game areas share one encounter deck and one encounter discard pile.",
        KangNoteKind.CentralTokensWaitFor4A =>
            "Acceleration tokens on stage 2B add no threat during the split; they carry into stage 4A.",
        KangNoteKind.CompletionStoresDominion =>
            "Completing this stage does not lose the game: it stores a Kang's Dominion under stage 4A, and this "
            + "group joins another game area at the end of the phase. It is better to let Kang (II) complete his "
            + "main scheme than to let Kang (II) defeat your hero.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no wording for this note"),
    };
}
