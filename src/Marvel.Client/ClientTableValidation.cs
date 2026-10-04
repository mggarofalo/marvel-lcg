using Marvel.View;

namespace Marvel.Client;

/// <summary>Checks passive public metadata before a response becomes recoverable client state.</summary>
internal static class ClientTableValidation
{
    internal static bool Complete(WorldDescriptor world)
    {
        if (world.Table is not { } table) return true;
        if (!ValidSeats(world, table)) return false;
        if (table.PendingSituation is not { } pending) return true;
        if (!ValidPending(pending, table)) return false;
        HashSet<int> readable = [.. world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null && card.FaceUp)
            .Select(card => card.Id!.Value)];
        return ReadableDistinct(pending.SourceCardIds, readable)
            && ReadableDistinct(pending.CauseCardIds, readable);
    }

    private static bool ValidPending(PendingSituationDescriptor pending, TableContextDescriptor table) =>
        Enum.IsDefined(pending.Kind) && pending.SourceCardIds is not null
        && pending.CauseCardIds is not null && table.PromptOwner is not null;

    private static bool ReadableDistinct(IReadOnlyList<int> ids, HashSet<int> readable) =>
        ids.Distinct().Count() == ids.Count && ids.All(readable.Contains);

    private static bool ValidSeats(WorldDescriptor world, TableContextDescriptor table)
    {
        HashSet<int> seats = [.. world.Players.Select(player => player.Seat)];
        return seats.Contains(table.ActivePlayer) && seats.Contains(table.FirstPlayer)
            && seats.Contains(table.PublicFocusSeat)
            && (table.PromptOwner is not { } owner || seats.Contains(owner))
            && (table.ViewedPrivateSeat is not { } viewer || seats.Contains(viewer));
    }
}
