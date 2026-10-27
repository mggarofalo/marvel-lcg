using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves one authored reveal before permitting its B-side flip.</summary>
internal static class KangSplitProcedure
{
    internal static Prompts.Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        var operation = step.Require<KangSplitOperation>();
        if (operation.Unused is { } unused)
            foreach (int id in unused) KangFirstSplit.Remove(world, world.Cards[id], "11008a", events);
        if (operation.Flip)
        {
            card.TurnTo(card.Faces[^1]);
            events.Add(new CardsFlipped([card.ObjectId], true)
                { SubjectFaces = OccurrenceFaces.For(card), Trigger = "Kang split", Verb = "Advance" });
            Reveal.EnterPlay(world, world.Facts, card, events);
            ActivityBatches.Append(events, world.EncounterAbilities.ExecuteReveal(world, card, step.Seat).Events);
        }
        else
        {
            ActivityBatches.Append(events, world.EncounterAbilities.ExecuteReveal(world, card, step.Seat).Events);
        }
        return null;
    }
}
