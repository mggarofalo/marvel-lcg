using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Names the seat controlling an offered payment source when it is not the paying seat.</summary>
/// <remarks>
/// Any player may use a Resource ability on an encounter card (rr:ability.8), so a payment can offer a card
/// on another seat's table, such as the first player's Milano (16142). The controller is the engine's
/// published fact; this only places it beside the source, and offers nothing the engine did not.
/// </remarks>
internal static class PaymentSourceSeat
{
    /// <summary>The controlling seat to name, or null for the payer's own or an unlocated source.</summary>
    internal static int? Other(PaymentSourcePresentation source, int payer) =>
        source.Controller is { } controller && controller != payer ? controller : null;

    /// <summary>The seat's chip, or null when the source is the payer's own.</summary>
    internal static PanelContainer? Chip(PaymentSourcePresentation source, int payer, WorldDescriptor world,
        InterfaceScale scale)
    {
        if (Other(source, payer) is not { } seat) return null;
        Wording wording = TableWording.SourceSeat(Name(world, seat), world.Table?.FirstPlayer == seat);
        PanelContainer chip = global::Marvel.Godot.Chip.Create($"SourceSeat{source.Id}", wording, null, scale);
        chip.SetMeta("source_seat", seat);
        chip.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return chip;
    }

    /// <summary>The seat as the rail names it: its identity card's published title, else the seat's public name.</summary>
    internal static string Name(WorldDescriptor world, int seat) =>
        world.PlayerSummaries.FirstOrDefault(summary => summary.Seat == seat)?.Identity is { } identity
        && world.Areas.SelectMany(area => area.Cards).FirstOrDefault(card => card.Id == identity)?.Face?.Title is { Length: > 0 } title
            ? title : PendingSituationPresentation.SeatName(world, seat);
}
