using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns the printed local-stage failure and its retained phase-end removal.</summary>
public static class KangLocalCompletion
{
    /// <summary>Whether the physical face has Kang's local completion policy.</summary>
    public static bool IsLocal(Card scheme) => scheme.FaceId is "11009b" or "11010b" or "11011b" or "11012b";

    internal static void Preflight(World world, Card scheme)
    {
        if (!IsLocal(scheme) || world.Agenda.Current is not
            { Operation: MainSchemeCompletionOperation completion } step
            || step.Subject != scheme.ObjectId || completion.Incarnation != scheme.Incarnation)
            throw new RulesNotImplementedException(
                $"Kang local main scheme {scheme.FaceId} completion requires Kang's Dominion and phase-end reunion");
        Require(world, scheme);
    }

    private static (Card Host, Card Dominion, Card Villain) Require(World world, Card scheme)
    {
        var hosts = world.Cards.Where(card => card.FaceId == "11013a"
            && card.Area.Type == DeckType.MainSchemesDeck && card.Owner == -1 && card.Incarnation == 0).ToArray();
        var dominions = world.Cards.Where(card => card.FaceId == "11023" && card.Owner == -1
            && card.Area.Type == DeckType.AsideDeck).OrderBy(card => card.ObjectId).ToArray();
        var villains = LocalVillains(world, scheme);
        if (hosts.Length != 1 || dominions.Length == 0 || villains.Length != 1
            || world.Agenda.KangTransitions.Pending.Any(item => item.Source == Places.GameAreaOf(world, scheme)))
            throw new RulesNotImplementedException("Kang's Dominion storage requires one unrevealed 4A, a set-aside Dominion and its live local Kang II");
        return (hosts[0], dominions[0], villains[0]);
    }

    private static Card[] LocalVillains(World world, Card scheme)
    {
        int index = scheme.FaceId switch { "11009b" => 0, "11010b" => 1, "11011b" => 2, _ => 3 };
        string normal = (11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string expert = (11035 + index).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return world.Cards.Where(card => (card.FaceId == normal || card.FaceId == expert)
            && card.Area.Type == DeckType.VillainArea
            && ReferenceEquals(Places.GameAreaOf(world, card), Places.GameAreaOf(world, scheme))).ToArray();
    }

    /// <summary>Executes this physical stage's own completion Forced Response.</summary>
    public static void Capture(World world, Card scheme, Occurrence occurrence, List<GameEvent> events)
    {
        if (!IsLocal(scheme) || !occurrence.Is(MainSchemeCompletionOperation.Completed)
            || occurrence.Subject != scheme.ObjectId || scheme.Area.Type != DeckType.MainSchemesArea
            || scheme.Tokens.GetValueOrDefault("is_completed") != 1)
            throw new RulesNotImplementedException("Kang local capture requires its exact completed stage");
        var (host, dominion, villain) = Require(world, scheme);
        var destination = world.AreaOf(DeckType.BeneathCardsPile, PlayArea.Villains, host.ObjectId);
        var from = dominion.Area;
        World.MoveToTop(dominion, destination);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(destination),
            [new Landing(dominion.ObjectId, destination.Cards.Count - 1)]) { Trigger = scheme.FaceId, Verb = "Store_Beneath" });
        events.Add(new CardAttached(dominion.ObjectId, host.ObjectId) { Trigger = scheme.FaceId, Verb = "Store_Beneath" });
        world.Agenda.KangTransitions.CaptureCompletion(world, villain, scheme);
    }
}
