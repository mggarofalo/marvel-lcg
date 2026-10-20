using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Finalizes the separate villain groups described by pack:mc03:new-rules.</summary>
internal static class EncounterVillainDefeat
{
    internal static bool Apply(World world, Card villain, string trigger, List<GameEvent> events)
    {
        var topology = world.EncounterTopology!;
        var group = topology.Groups.Single(candidate => candidate.Stages.Contains(villain));
        int stage = group.Stages.ToList().IndexOf(villain);
        Card? next = stage + 1 < group.Stages.Count ? group.Stages[stage + 1] : null;
        bool active = ReferenceEquals(topology.Active, group);
        var tokens = villain.Tokens;
        var area = villain.Area;
        Remove(world, villain, trigger, events, preserveHosted: next is not null);
        if (active) events.Add(new FieldSet(villain.ObjectId, "active_villain", 1, 0)
            { Trigger = trigger, Verb = "Remove" });
        if (next is not null)
            return Promote(world, villain, next, tokens, area, trigger, events);

        // pack:mc03:new-rules: the defeated villain's side scheme is removed;
        // encounter cards from its deck already in play remain in play.
        Remove(world, group.Scheme, trigger, events, verb: "Remove");
        if (topology.Groups.All(candidate => candidate.CurrentVillain is null))
        {
            world.Finish(Outcome.PlayersWin);
            return false;
        }
        return active && ActiveVillainSelection.Schedule(world, trigger, events);
    }

    private static bool Promote(World world, Card prior, Card next,
        IReadOnlyDictionary<string, long> tokens, Area into, string trigger, List<GameEvent> events)
    {
        // pack:mc03:adjustable-difficulty: "its version B enters play".
        // rr:villain-defeat.3.1: the next stage is the same character.
        // Engine choice: retain that character's captured area for continuation
        // identity and public placement, including worlds with separate areas.
        var from = next.Area;
        World.MoveToTop(next, into);
        next.TurnFaceUp();
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
            [new Landing(next.ObjectId, into.Cards.Count - 1)]) { Trigger = trigger, Verb = "Reveal" });
        if (ReferenceEquals(world.EncounterTopology!.Active.CurrentVillain, next))
            events.Add(new FieldSet(next.ObjectId, "active_villain", 0, 1)
                { Trigger = trigger, Verb = "Place" });
        VillainStageInheritance.Apply(world, world.Facts, prior, next, trigger, events, tokens);
        Reveal.EnterPlay(world, world.Facts, next, events);
        var revealed = world.EncounterAbilities.ExecuteReveal(world, next, world.FirstPlayer);
        ActivityBatches.Append(events, revealed.Events);
        return !revealed.IsComplete;
    }

    private static void Remove(World world, Card card, string trigger,
        List<GameEvent> events, bool preserveHosted = false, string verb = "Defeat")
    {
        var constants = world.Effects.PreflightConstantsEnding(card, includeHostedCards: !preserveHosted);
        using var departure = constants.Begin();
        if (!preserveHosted) Discard.Attachments(world, card, trigger, events);
        var from = card.Area;
        var removed = world.AreaOf(DeckType.RemovedArea);
        World.MoveToTop(card, removed);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)]) { Trigger = trigger, Verb = verb });
        constants.Complete(trigger, events);
    }
}
