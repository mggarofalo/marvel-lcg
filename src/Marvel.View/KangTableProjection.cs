using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Describes the engine's Kang progress facts for an authorized scope.</summary>
/// <remarks>
/// Every value comes from <see cref="KangTable.Of"/>; this projection drops ids the scope cannot
/// address. It carries facts, not sentences: a client words the stage, the acting step, each
/// departure and each <see cref="KangNoteKind"/> itself.
/// </remarks>
internal static class KangTableProjection
{
    internal static KangTableDescriptor? From(World world, IReadOnlySet<int> addressable)
    {
        if (KangTable.Of(world) is not { } facts) return null;
        // A departing Kang is in play or removed from the game, and a noted scheme is in play,
        // all public; these filters remove nothing today (equivalent mutants) and keep every id
        // scope-checked.
        int? Visible(int? id) => id is { } known && addressable.Contains(known) ? known : null;
        var departures = facts.Departures.Where(item => addressable.Contains(item.Villain))
            .Select(item => new KangDepartureDescriptor(item.Villain, item.SourceGameArea, item.Seats.ToArray(),
                Visible(item.CompletedScheme), item.Destinations.ToArray(), item.Reunion)).ToArray();
        return new KangTableDescriptor(facts.Stage, facts.RemainingLocalKangs, facts.StoredDominions,
            [.. facts.CentralSchemes.Where(addressable.Contains)], Visible(facts.PendingSplit), departures)
        {
            Acting = facts.Acting is { } acting ? new(acting.Step, acting.Seat, acting.GameArea) : null,
            LocalKangsDealt = facts.LocalKangsDealt,
            StartingPlayers = world.Players,
            EncounterDeck = world.AreaOf(DeckType.EncounterDeck).Id,
            EncounterDiscard = world.AreaOf(DeckType.EncounterDiscardPile).Id,
            Notes = [.. facts.Notes.Where(note => note.Card is not { } card || addressable.Contains(card))
                .Select(note => new KangNoteDescriptor(note.Kind, note.Card))],
        };
    }
}
