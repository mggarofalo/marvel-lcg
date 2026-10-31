using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Retains local encounter components when their player carrier leaves.</summary>
public sealed class KangElimination
{
    private readonly Dictionary<int, (Area Area, long Incarnation, GameArea Group)> retained = [];

    internal GameArea? GameAreaOf(Card card) => retained.TryGetValue(card.ObjectId, out var capture)
        && ReferenceEquals(card.Area, capture.Area) && card.Incarnation == capture.Incarnation
        ? capture.Group : null;

    internal bool Retains(Card card) => GameAreaOf(card) is not null;

    internal static void Preflight(World world, int player)
    {
        var source = world.GameAreaOf(PlayArea.Of(player));
        // Engine boundary: the suspended batch owns its captured sources and
        // possible destinations even after its local schemes and Pending entries leave.
        if (source is not null && world.Agenda.Outstanding.Any(step => step.Operation switch
        {
            KangJoinOperation join => world.Agenda.IsOutstanding(join.Owner, join.Origin),
            KangDepartureOrderOperation order => world.Agenda.IsOutstanding(order.Owner, order.Origin),
            KangPhaseEndOrderOperation order => world.Agenda.IsOutstanding(order.Owner, order.Origin),
            _ => false,
        }))
            throw new RulesNotImplementedException("Kang local elimination during a suspended departure choice is not implemented");
        KangPhaseTransitions.RequireGroups(world.Agenda.KangTransitions.Pending
            .Where(item => ReferenceEquals(item.Source, source)));
    }

    internal GameArea? Capture(World world, int player)
    {
        var source = world.GameAreaOf(PlayArea.Of(player));
        if (source is null || world.Seats.All(seat => seat.Index == player || seat.Eliminated)) return null;
        bool local = world.Cards.Any(card => KangLocalCompletion.IsLocal(card)
            && card.Area.Type == DeckType.MainSchemesArea
            && ReferenceEquals(Places.GameAreaOf(world, card), source))
            || world.Agenda.KangTransitions.Pending.Any(item => ReferenceEquals(item.Source, source));
        if (!local) return null;

        // Engine representation choice: scenario zones carried by the departing
        // seat retain their game-area association until their printed departure.
        // The player's membership and all player-owned cards still leave normally.
        var roots = world.Cards.Where(card => card.Owner == World.Scenario
            && card.Area.PlayArea == PlayArea.Of(player)
            && card.Area.Type is DeckType.MainSchemesArea or DeckType.VillainArea or DeckType.SideSchemesArea)
            .Select(card => card.ObjectId).ToHashSet();
        IncludeHosted(world, roots);
        foreach (int id in roots.Order()) retained[id] = (world.Cards[id].Area, world.Cards[id].Incarnation, source);
        return source;
    }

    private static void IncludeHosted(World world, HashSet<int> roots)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var card in world.Cards.Where(card => card.Owner == World.Scenario
                && roots.Contains(card.Area.Host)))
                changed |= roots.Add(card.ObjectId);
        } while (changed);
    }

    internal void Join(World world, GameArea source, GameArea destination, string trigger, List<GameEvent> events)
    {
        var carrier = destination.PlayAreas.Where(area => area.IsPlayers).OrderBy(area => area.Player).First();
        foreach (int id in retained.Where(pair => ReferenceEquals(pair.Value.Group, source))
            .Select(pair => pair.Key).Order().ToArray())
        {
            var card = world.Cards[id];
            if (Retains(card))
            {
                var from = card.Area;
                var into = world.AreaOf(from.Type, carrier, from.Host, from.CardOwner);
                bool faceUp = card.FaceUp;
                World.MoveToTop(card, into);
                if (faceUp) card.TurnFaceUp(); else card.TurnFaceDown();
                events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
                    [new Landing(id, into.Cards.Count - 1)]) { Trigger = trigger, Verb = "Join" });
            }
            retained.Remove(id);
        }
    }
}
