using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Risky Business counter exhaustion and physical villain form changes.</summary>
public static class GreenGoblinTransition
{
    /// <summary>Remove available infamy or madness and change both faces at zero.</summary>
    public static long RemoveCounters(
        World world, Card environment, string counter, long amount,
        string trigger, List<GameEvent> events)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Card villain = ValidateCurrent(world, environment, counter);
        string key = "c_" + counter;
        long before = environment.Tokens.GetValueOrDefault(key);
        long removed = Math.Min(before, amount);
        if (removed == 0) return 0;
        environment.PlaceTokens(key, -removed);
        events.Add(new FieldSet(environment.ObjectId, key, before, before - removed)
        {
            Trigger = trigger,
            Verb = "Remove_Counter",
        });
        if (before == removed) Flip(world, environment, villain, counter, trigger, events);
        return removed;
    }

    private static Card ValidateCurrent(World world, Card environment, string counter)
    {
        string face = counter switch
        {
            "infamy" => "02006a",
            "madness" => "02006b",
            _ => throw new ArgumentException("Unknown Risky Business counter", nameof(counter)),
        };
        if (environment.FaceId != face || !DeckTypes.IsInPlay(environment.Area.Type))
            throw new RulesNotImplementedException($"{counter} requires in-play {face}");

        Card villain = world.Cards.SingleOrDefault(card =>
            DeckTypes.IsInPlay(card.Area.Type) && card.FaceId is
                ("02001a" or "02002a" or "02003a" or "02001b" or "02002b" or "02003b"))
            ?? throw new RulesNotImplementedException("Risky Business requires an in-play villain");
        string nextVillain = villain.FaceId[..5] + (counter == "infamy" ? "b" : "a");
        string nextEnvironment = counter == "infamy" ? "02006b" : "02006a";
        if (!villain.Faces.Contains(nextVillain) || !environment.Faces.Contains(nextEnvironment)
            || villain.FaceId[^1] != face[^1])
            throw new RulesNotImplementedException("Risky Business villain and environment forms disagree");
        return villain;
    }

    private static void Flip(World world, Card environment, Card villain,
        string counter, string trigger, List<GameEvent> events)
    {
        // pack:mc02:new-rules#when-the-villain-changes-form: attachments, status,
        // boost cards and damage remain on the same physical villain card.
        string nextVillain = villain.FaceId[..5] + (counter == "infamy" ? "b" : "a");
        string nextEnvironment = counter == "infamy" ? "02006b" : "02006a";
        CardFlip.To(world, world.Facts, environment, nextEnvironment, trigger, events);
        CardFlip.To(world, world.Facts, villain, nextVillain, trigger, events);
        InitializeNextPool(world, environment, counter, trigger, events);
        if (counter == "infamy")
            ActivityBatches.Append(events,
                world.EncounterAbilities.ExecuteReveal(world, villain, world.FirstPlayer).Events);
    }

    private static void InitializeNextPool(World world, Card environment,
        string counter, string trigger, List<GameEvent> events)
    {
        // Engine interpretation of 02006a/b and rr:in-play-and-out-of-play.6:
        // the newly faceup environment is the active side and starts with its
        // printed 2-per-player pool. The flip retains the physical card's tokens
        // under rr:flip.2.1, so initialize this side without re-entering play.
        string nextKey = counter == "infamy" ? "c_madness" : "c_infamy";
        long nextBefore = environment.Tokens.GetValueOrDefault(nextKey);
        long nextAmount = checked(2L * world.Players);
        environment.PlaceTokens(nextKey, nextAmount - nextBefore);
        events.Add(new FieldSet(environment.ObjectId, nextKey, nextBefore, nextAmount)
        {
            Trigger = trigger,
            Verb = "Flip",
        });
    }

}
